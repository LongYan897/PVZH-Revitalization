class_name ScrollInputHandler
extends RefCounted
## 处理 SmoothScrollContainer 的所有输入事件。
##
## 处理鼠标滚轮、拖拽（鼠标/触摸）、手势以及滚动条交互。


#region Variables
## 对父级滚动容器的引用
var _container: SmoothScrollContainer = null
## 鼠标滚轮滚动速度倍率
var speed: float = 1000.0
## 允许使用鼠标拖拽
var drag_with_mouse: bool = true
## 允许使用触摸拖拽
var drag_with_touch: bool = true
## 是否处理输入事件
var handle_input: bool = true
## 内容当前是否正在被拖拽
var content_dragging: bool = false
## 当前这次拖拽期间内容是否已发生移动
var content_dragging_moved: bool = false
## 触摸点是否处于死区
var is_in_deadzone: bool = false
## 为 true 时正在拖拽水平滚动条
var h_scrollbar_dragging: bool = false
## 为 true 时正在拖拽垂直滚动条
var v_scrollbar_dragging: bool = false
## 鼠标是否位于任一滚动条上
var mouse_on_scrollbar: bool = false
## 拖拽状态数据：[0,1] 相对位移累加值，[2,3] 起始位置，[4-7] 各边界距离
var drag_temp_data: Array = []
## 上一次已处理的拖拽位移所在帧（用于过滤“鼠标模拟触摸”产生的重复事件）
var _last_drag_frame: int = -1
## 上一次已处理的拖拽位移
var _last_drag_relative: Vector2 = Vector2.ZERO
#endregion


## 使用对 [param container] 的引用初始化输入处理器。
func _init(container: SmoothScrollContainer) -> void:
	_container = container


## 处理用于滚动的 GUI 输入事件。[br]
## 处理来自 [param event] 的鼠标滚轮、拖拽、平移手势以及触摸事件。
func process_gui_input(event: InputEvent) -> void:
	# 鼠标移动时显示滚动条
	if _container.hide_scrollbar_over_time and event is InputEventMouseMotion:
		_container.scrollbar_animator.show_scrollbars()
	
	# 鼠标按键事件（滚轮滚动与拖拽开始/结束）
	if event is InputEventMouseButton:
		_process_mouse_button(event)
	
	# 拖拽移动事件
	if (event is InputEventScreenDrag and drag_with_touch) \
			or (event is InputEventMouseMotion and drag_with_mouse):
		_process_drag_motion(event)
	
	# 平移手势事件
	if event is InputEventPanGesture:
		_process_pan_gesture(event)
	
	# 触摸事件
	if event is InputEventScreenTouch:
		_process_screen_touch(event)
	
	# 若已配置，则将输入标记为已处理
	if handle_input:
		_container.get_tree().get_root().set_input_as_handled()


## 处理来自 [param event] 的滚动条输入事件。[br]
## 同时处理垂直（[param vertical]）与水平滚动条交互。
func process_scrollbar_input(event: InputEvent, vertical: bool) -> void:
	if event is InputEventMouseButton:
		# 将滚轮事件转发给主输入处理器
		if event.button_index in [
			MOUSE_BUTTON_WHEEL_DOWN,
			MOUSE_BUTTON_WHEEL_UP,
			MOUSE_BUTTON_WHEEL_LEFT,
			MOUSE_BUTTON_WHEEL_RIGHT
		]:
			process_gui_input(event)
		
		# 处理滚动条拖拽
		if event.button_index == MOUSE_BUTTON_LEFT:
			_handle_scrollbar_drag_button(event, vertical)
	
	if event is InputEventScreenTouch:
		_handle_scrollbar_touch(event, vertical)


## 根据 [param entered] 在鼠标进入或离开滚动条区域时调用。
func on_mouse_scrollbar(entered: bool) -> void:
	mouse_on_scrollbar = entered


## 检查是否有任一滚动条正在被拖拽。
func any_scrollbar_dragging() -> bool:
	return h_scrollbar_dragging or v_scrollbar_dragging


## 使用当前位置与各边界距离初始化拖拽临时数据。
func init_drag_temp_data() -> void:
	var spare_size: Vector2 = ScrollLayout.get_spare_size(_container, _container.content_margins)
	var content_node_size_diff: Vector2 = ScrollLayout.get_child_size_diff(
		_container.content_node,
		spare_size,
		true,
		true
	)
	var content_node_boundary_dist: Vector4 = ScrollLayout.get_boundary_dist(
		_container.pos,
		content_node_size_diff
	)
	drag_temp_data = [
		0.0,  # X 轴相对位移累加值
		0.0,  # Y 轴相对位移累加值
		_container.pos.x,  # X 轴起始位置
		_container.pos.y,  # Y 轴起始位置
		content_node_boundary_dist.x,  # 左边界距离
		content_node_boundary_dist.y,  # 右边界距离
		content_node_boundary_dist.z,  # 上边界距离
		content_node_boundary_dist.w,  # 下边界距离
	]


## 处理来自 [param event] 的鼠标按键事件，用于滚轮滚动与拖拽。
func _process_mouse_button(event: InputEventMouseButton) -> void:
	match event.button_index:
		MOUSE_BUTTON_WHEEL_DOWN:
			if event.pressed:
				_handle_wheel_scroll(event, false, true)  # 向下方向
		MOUSE_BUTTON_WHEEL_UP:
			if event.pressed:
				_handle_wheel_scroll(event, true, true)  # 向上方向
		MOUSE_BUTTON_WHEEL_LEFT:
			if event.pressed:
				_handle_wheel_scroll(event, true, false)  # 向左方向
		MOUSE_BUTTON_WHEEL_RIGHT:
			if event.pressed:
				_handle_wheel_scroll(event, false, false)  # 向右方向
		MOUSE_BUTTON_LEFT:
			if event.pressed:
				_start_content_drag()
			else:
				_end_content_drag()


## 处理来自 [param event] 的鼠标滚轮滚动。[br]
## 在指定轴（[param is_vertical]）上沿 [param positive] 方向（上/左或下/右）滚动。
func _handle_wheel_scroll(event: InputEventMouseButton, positive: bool, is_vertical: bool) -> void:
	_container.last_scroll_type = SmoothScrollContainer.SCROLL_TYPE.WHEEL
	_container.scroll_damper = _container.wheel_scroll_damper
	_container.scrollbar_animator.kill_scroll_tweens()
	
	var amount: float = speed * event.factor * (1.0 if positive else -1.0)
	
	# 根据 Shift 键与可用的滚动方向确定要滚动的轴
	if is_vertical:
		if event.shift_pressed or not _container.should_scroll_vertical():
			if _container.should_scroll_horizontal():
				_container.velocity.x += amount
		else:
			if _container.should_scroll_vertical():
				_container.velocity.y += amount
	else:  # 水平滚轮
		if event.shift_pressed:
			if _container.should_scroll_vertical():
				_container.velocity.y += amount if positive else -amount
		else:
			if _container.should_scroll_horizontal():
				_container.velocity.x += amount


## 开始拖拽内容。
func _start_content_drag() -> void:
	if not drag_with_mouse: return
	
	content_dragging = true
	is_in_deadzone = true
	_container.scroll_damper = _container.dragging_scroll_damper
	_container.last_scroll_type = SmoothScrollContainer.SCROLL_TYPE.DRAG
	init_drag_temp_data()
	_container.scrollbar_animator.kill_scroll_tweens()


## 结束拖拽内容。
func _end_content_drag() -> void:
	_notify_scroll_end()
	if content_dragging:
		if _container.debug_mode:
			print("[SmoothScroll] 松手时的速度 = ", _container.velocity,
				"（惯性系数 ", _container.drag_release_momentum, "）")
		# 松手时按比例削减拖拽速度，缩短松手后的惯性滑行距离
		_container.velocity *= _container.drag_release_momentum
	content_dragging = false
	is_in_deadzone = false


## 处理来自 [param event] 的拖拽移动事件及其相对位移。
func _process_drag_motion(event) -> void:
	if not content_dragging: return
	
	# 项目开启「从鼠标模拟触摸」（input_devices/pointing/emulate_touch_from_mouse）时，
	# 同一次拖动会同时收到 InputEventMouseMotion 和模拟出来的 InputEventScreenDrag，
	# 位移会被累加两遍，表现为「拖动不跟手、移动量是鼠标的两倍」。
	# 这里把「同一帧 + 位移完全相同」的重复事件丢掉，只保留一份。
	var frame: int = Engine.get_process_frames()
	var rel: Vector2 = event.relative
	if frame == _last_drag_frame and rel.is_equal_approx(_last_drag_relative):
		return
	_last_drag_frame = frame
	_last_drag_relative = rel
	
	if _container.should_scroll_horizontal():
		drag_temp_data[0] += event.relative.x
	if _container.should_scroll_vertical():
		drag_temp_data[1] += event.relative.y
	
	if is_in_deadzone:
		var deadzone: int = _container.scroll_deadzone
		if abs(drag_temp_data[0]) <= deadzone and abs(drag_temp_data[1]) <= deadzone:
			return
		
		is_in_deadzone = false
		# 重置累加值，使滚动从当前位置开始
		drag_temp_data[0] = 0.0
		drag_temp_data[1] = 0.0
		# 取消子控件挂起的输入，例如按钮按下
		_container.propagate_notification(Control.NOTIFICATION_SCROLL_BEGIN)
	
	# 调试：单帧鼠标位移特别大时打印出来（配合 debug_mode 使用）
	if _container.debug_mode and absf(event.relative.y) > 40.0:
		print("[SmoothScroll] 单帧位移 ", event.relative, "，本帧累计 ", drag_temp_data[1])
	
	_container.handle_content_dragging()


## 处理来自 [param event] 的平移手势事件。
func _process_pan_gesture(event: InputEventPanGesture) -> void:
	if _container.should_scroll_horizontal():
		_container.velocity.x = -event.delta.x * speed
		_container.scrollbar_animator.kill_scroll_tweens()
	if _container.should_scroll_vertical():
		_container.velocity.y = -event.delta.y * speed
		_container.scrollbar_animator.kill_scroll_tweens()


## 处理来自 [param event] 的屏幕触摸事件。
func _process_screen_touch(event: InputEventScreenTouch) -> void:
	if event.pressed:
		if not drag_with_touch:
			return
		
		content_dragging = true
		is_in_deadzone = true
		_container.scroll_damper = _container.dragging_scroll_damper
		_container.last_scroll_type = SmoothScrollContainer.SCROLL_TYPE.DRAG
		init_drag_temp_data()
		_container.scrollbar_animator.kill_scroll_tweens()
	else:
		_notify_scroll_end()
		if content_dragging:
			# 触摸松手同样按比例削减速度
			_container.velocity *= _container.drag_release_momentum
		content_dragging = false
		is_in_deadzone = false


## 使用鼠标按键处理来自 [param event] 的滚动条拖拽。[br]
## 处理垂直（[param vertical]）或水平滚动条交互。
func _handle_scrollbar_drag_button(event: InputEventMouseButton, vertical: bool) -> void:
	if event.pressed:
		if vertical:
			v_scrollbar_dragging = true
		else:
			h_scrollbar_dragging = true
		_container.last_scroll_type = SmoothScrollContainer.SCROLL_TYPE.BAR
		_container.scrollbar_animator.kill_scroll_tweens()
	else:
		if vertical:
			v_scrollbar_dragging = false
		else:
			h_scrollbar_dragging = false


## 使用触摸处理来自 [param event] 的滚动条拖拽。[br]
## 处理垂直（[param vertical]）或水平滚动条交互。
func _handle_scrollbar_touch(event: InputEventScreenTouch, vertical: bool) -> void:
	if event.pressed:
		if vertical:
			v_scrollbar_dragging = true
		else:
			h_scrollbar_dragging = true
		_container.last_scroll_type = SmoothScrollContainer.SCROLL_TYPE.BAR
		_container.scrollbar_animator.kill_scroll_tweens()
	else:
		if vertical:
			v_scrollbar_dragging = false
		else:
			h_scrollbar_dragging = false


## 通知子控件滚动已结束。[br]
## 只在离开死区之后发送，因此总是与滚动开始成对出现。
func _notify_scroll_end() -> void:
	if content_dragging and not is_in_deadzone:
		_container.propagate_notification(Control.NOTIFICATION_SCROLL_END)
