@tool
class_name SmoothScrollContainer
extends ScrollContainer
## 为 ScrollContainer 提供平滑滚动功能。
##
## 为 ScrollContainer 应用基于速度的惯性与“越界拖拽”
## 功能。


#region Variables
## 最近一次用于滚动的输入类型
enum SCROLL_TYPE {
	## 鼠标滚轮滚动
	WHEEL,
	## 滚动条拖拽
	BAR,
	## 内容拖拽
	DRAG
}

#region Exported Variables
@export_group("鼠标滚轮")
## 单次滚动输入的拖拽强度
@export_range(0.0, 10.0, 0.01, "or_greater", "hide_slider") var speed: float = 1000.0
## 滚轮滚动使用的 ScrollDamper
@export var wheel_scroll_damper: ScrollDamper = ExpoScrollDamper.new()

@export_group("拖拽")
## 拖拽使用的 ScrollDamper
@export var dragging_scroll_damper: ScrollDamper = ExpoScrollDamper.new()
## 是否允许用鼠标拖拽
@export var drag_with_mouse: bool = true
## 是否允许用触摸拖拽
@export var drag_with_touch: bool = true
## 松手后保留的惯性比例：1.0 = 保留全部惯性，0.0 = 松手立刻停住。
## 数值越小，松手后惯性滑行的距离越短。
@export_range(0.0, 1.0, 0.01) var drag_release_momentum: float = 0.35

@export_group("容器")
## 低于该值时，将内容吸附到边界
@export var just_snap_under: float = 0.4
## 当前聚焦元素的外边距
@export_range(0, 50) var follow_focus_margin: int = 20
## 使容器可垂直滚动
@export var allow_vertical_scroll: bool = true
## 使容器可水平滚动
@export var allow_horizontal_scroll: bool = true
## 使容器仅在内容发生溢出时才可滚动
@export var auto_allow_scroll: bool = true
## 是否允许该容器的内容在两端越界，
## 然后再插值回到其边界之内
@export var allow_overdragging: bool = true

@export_group("滚动条")
## 在未被悬停或操作时隐藏滚动条
@export var hide_scrollbar_over_time: bool = false:
	set(val): hide_scrollbar_over_time = _set_hide_scrollbar_over_time(val)
## 当 'hide_scrollbar_over_time' 为 true 时，滚动条开始淡出前的等待时间
@export var scrollbar_hide_time: float = 5.0
## 当 'hide_scrollbar_over_time' 为 true 时，滚动条的淡入时间
@export var scrollbar_fade_in_time: float = 0.2
## 当 'hide_scrollbar_over_time' 为 true 时，滚动条的淡出时间
@export var scrollbar_fade_out_time: float = 0.5

@export_group("输入")
## 若为 true，则通过 set_input_as_handled() 将输入事件标记为已处理
@export var handle_input: bool = true
## 若为 true，则自动将子 Control 节点的 mouse_filter 设为 MOUSE_FILTER_PASS，
## 以确保平滑滚动正常工作。[br]
## 注意：已经设为 MOUSE_FILTER_IGNORE 的节点（通常用于让点击穿透到下层按钮）会被跳过，
## 不会被改成 PASS，否则会把下层要接收点击的节点挡住。
@export var override_mouse_filters: bool = true:
	set(val): override_mouse_filters = _set_override_mouse_filters(val)

@export_group("调试")
## 添加调试信息
@export var debug_mode: bool = false
#endregion

#region Public Variables
## `content_node` 的当前速度
var velocity := Vector2.ZERO
## 滚动时要移动的 Control 节点
var content_node: Control
## `content_node` 的当前位置
var pos := Vector2(0, 0)
## 根据最近一次输入类型确定当前使用的 ScrollDamper
var scroll_damper: ScrollDamper
## 内容是否正在被滚动
var is_scrolling: bool = false:
	set(val):
		if is_scrolling != val:
			if val:
				emit_signal("scroll_started")
			else:
				emit_signal("scroll_ended")
		is_scrolling = val
## 最近一次用于滚动的输入类型
var last_scroll_type: SCROLL_TYPE
#endregion

#region Private Variables
## StyleBox 内容外边距（左、上、右、下）
var content_margins := Vector4.ZERO
## 正在（重新）应用外边距/布局时为 true；此时禁用滚动
var _initializing_margins: bool = false
## 由 StyleBox/内容施加的基准布局偏移
var _base_offset := Vector2.ZERO
## 首次完成外边距/布局初始化后为 true；用于在启动阶段控制 follow_focus
var _startup_done: bool = false
## 若因处于隐藏状态而跳过初始外边距计算，则为 true
var _initial_margins_skipped: bool = false
## 用于淡入淡出效果和滚动补间动画的滚动条动画器
var scrollbar_animator: ScrollbarAnimator
## 处理所有用户输入事件的输入处理器
var input_handler: ScrollInputHandler
## 缓存 is_editor_hint() 的值以提升性能
var _is_editor_hint = Engine.is_editor_hint()

## 缓存最近一次已知尺寸，用于检测尺寸变化以延迟 follow_focus
var _last_ensure_size := Vector2.ZERO
## 用于统计尺寸稳定帧数的计数器
var _size_stable_frames := 0
## 尺寸稳定后再滚动到的待处理控件
var _pending_ensure_control: Control = null
## 用于检查尺寸稳定性的计时器
var _ensure_stability_timer: Timer = null
#endregion

#endregion


#region Native Functions
## 当节点首次进入场景树时调用。[br]
## 初始化滚动条、计时器和初始配置。
func _ready() -> void:
	if not ScrollDebugger.debug_gradient: ScrollDebugger.setup_debug_drawing()
	
	# Initialize variables
	scroll_damper = wheel_scroll_damper
	scrollbar_animator = ScrollbarAnimator.new(self)
	scrollbar_animator.hide_time = scrollbar_hide_time
	scrollbar_animator.fade_in_time = scrollbar_fade_in_time
	scrollbar_animator.fade_out_time = scrollbar_fade_out_time
	
	# Initialize input handler
	input_handler = ScrollInputHandler.new(self)
	input_handler.speed = speed
	input_handler.drag_with_mouse = drag_with_mouse
	input_handler.drag_with_touch = drag_with_touch
	input_handler.handle_input = handle_input
	
	get_v_scroll_bar().gui_input.connect(_scrollbar_input.bind(true))
	get_h_scroll_bar().gui_input.connect(_scrollbar_input.bind(false))
	get_v_scroll_bar().mouse_entered.connect(_mouse_on_scroll_bar.bind(true))
	get_v_scroll_bar().mouse_exited.connect(_mouse_on_scroll_bar.bind(false))
	get_h_scroll_bar().mouse_entered.connect(_mouse_on_scroll_bar.bind(true))
	get_h_scroll_bar().mouse_exited.connect(_mouse_on_scroll_bar.bind(false))
	get_viewport().gui_focus_changed.connect(_on_focus_changed)

	visibility_changed.connect(_visibility_changed)
	theme_changed.connect(_update_content_margins)
	
	# Check if we're initially hidden - if so, defer margin calculation until visible
	if visible:
		call_deferred("_update_content_margins")
	else:
		_initial_margins_skipped = true

	for child: Node in get_children():
		if child is Control and not child is ScrollBar:
			content_node = child
			break
	
	if content_node:
		_base_offset = content_node.position
	
	if hide_scrollbar_over_time:
		scrollbar_animator.start_hide_timer()
	
	if override_mouse_filters:
		get_tree().node_added.connect(_on_node_added)
		if not _is_editor_hint:
			call_deferred("_apply_mouse_filters_to_children")
	
	# Default to idle state until needed
	set_process(false)


## 每帧调用。更新滚动位置、速度和滚动条状态。
func _process(delta: float) -> void:
	if _is_editor_hint: return
	if _initializing_margins: return

	scroll(true, velocity.y, pos.y, delta)
	scroll(false, velocity.x, pos.x, delta)
	update_scrollbars()
	update_is_scrolling()

	if debug_mode: queue_redraw()
	
	if not is_scrolling:
		set_process(false)
#endregion


#region Input Handling
## 检测鼠标进入或离开滚动条区域。根据 [param entered] 触发相应的可见性行为。
func _mouse_on_scroll_bar(entered: bool) -> void:
	input_handler.on_mouse_scrollbar(entered)


## 将指定滚动条的滚动输入转发给输入处理器。[br]
## 同时处理 [param vertical] 垂直滚动条与水平滚动条的 [param event] 输入。
func _scrollbar_input(event: InputEvent, vertical: bool) -> void:
	set_process(true)
	input_handler.process_scrollbar_input(event, vertical)


## 通过将所有 GUI 输入事件委托给输入处理器来处理它们。
func _gui_input(event: InputEvent) -> void:
	set_process(true)
	input_handler.process_gui_input(event)


## 焦点变化时滚动，以确保新获得焦点的 [param control] 可见。
func _on_focus_changed(control: Control) -> void:
	if follow_focus and _startup_done:
		self.ensure_control_visible_smooth(control)


## 启用调试模式时绘制调试信息。
func _draw() -> void:
	if debug_mode: ScrollDebugger.draw_debug(self)


## 判断某个子 Control 的 mouse_filter 是否应该被改成 [constant Control.MOUSE_FILTER_PASS]。[br]
## 已经是 [constant Control.MOUSE_FILTER_IGNORE] 的节点必须原样保留：
## 这种节点通常是故意让点击穿透到下层按钮的（例如卡牌卡面），
## 改成 PASS 会让它重新变成命中目标，把下层按钮的点击整个吃掉。[br]
## 按钮类（[BaseButton]）保持 PASS 反而更合适：按下事件会继续冒泡到滚动容器，
## 所以既能点击，也能从按钮上按住拖动滚动；拖动超过 scroll_deadzone 后容器会发出
## NOTIFICATION_SCROLL_BEGIN，按钮自己会取消这次按下，不会误触。
func _should_override_mouse_filter(node: Control) -> bool:
	return node.mouse_filter != Control.MOUSE_FILTER_IGNORE


## 将 SmoothScroll 子节点的默认鼠标过滤器设为 [constant Control.MOUSE_FILTER_PASS]。[br]
## 当 [param node] 被添加到场景树时调用。
func _on_node_added(node: Node) -> void:
	if node is Control and is_ancestor_of(node):
		if override_mouse_filters:
			if not node.has_meta("_smooth_scroll_default_mouse_filter_set"):
				if _should_override_mouse_filter(node):
					node.mouse_filter = Control.MOUSE_FILTER_PASS
				node.set_meta("_smooth_scroll_default_mouse_filter_set", true)


## 当滚动条隐藏计时器超时时调用。在两条滚动条都未被拖拽时隐藏滚动条。
func _scrollbar_hide_timer_timeout() -> void:
	if scrollbar_animator.should_hide(input_handler.h_scrollbar_dragging, input_handler.v_scrollbar_dragging):
		scrollbar_animator.hide_scrollbars()


## 依据当前 StyleBox 更新内容外边距。[br]
## 记录基准偏移并清零速度，以保证滚动计算在无外边距的坐标空间中进行。
func _update_content_margins() -> void:
	set_process(true)
	_initializing_margins = true
	
	content_margins = ScrollLayout.get_content_margins(self)
	
	if content_node:
		_base_offset = ScrollLayout.calculate_base_offset(content_margins)
		
		if _initial_margins_skipped:
			pos = Vector2.ZERO
		
		if not _startup_done:
			velocity = Vector2.ZERO
	
	call_deferred("_end_margin_init")


## 通过重置初始化标志来完成外边距初始化。
func _end_margin_init() -> void:
	_initializing_margins = false
	_startup_done = true
	_initial_margins_skipped = false


## 可见性变化时调用。需要时更新外边距。
func _visibility_changed() -> void:
	if visible and content_node:
		if _initial_margins_skipped:
			call_deferred("_update_content_margins")
		
		elif _startup_done:
			call_deferred("_update_content_margins")


## [member hide_scrollbar_over_time] 的 setter。根据 [param value] 控制滚动条是否随时间自动隐藏。
func _set_hide_scrollbar_over_time(value: bool) -> bool:
	if value == false:
		if scrollbar_animator:
			scrollbar_animator.stop_hide_timer()
			scrollbar_animator.show_scrollbars_immediate()
	else:
		if scrollbar_animator:
			scrollbar_animator.start_hide_timer()
	return value


## [member override_mouse_filters] 的 setter。在运行时启用时，将鼠标过滤器应用到已有的子节点。
func _set_override_mouse_filters(value: bool) -> bool:
	if is_inside_tree():
		if value:
			if not get_tree().node_added.is_connected(_on_node_added):
				get_tree().node_added.connect(_on_node_added)
			if not _is_editor_hint:
				call_deferred("_apply_mouse_filters_to_children")
		else:
			if get_tree().node_added.is_connected(_on_node_added):
				get_tree().node_added.disconnect(_on_node_added)
	return value


## 递归地将 MOUSE_FILTER_PASS 应用到所有子 Control 节点。
func _apply_mouse_filters_to_children() -> void:
	_apply_mouse_filter_recursive(self)


## 递归地对 Control 节点应用鼠标过滤器。
func _apply_mouse_filter_recursive(node: Node) -> void:
	if node is Control and node != self and is_ancestor_of(node):
		if not node.has_meta("_smooth_scroll_default_mouse_filter_set"):
			if _should_override_mouse_filter(node):
				node.mouse_filter = Control.MOUSE_FILTER_PASS
			node.set_meta("_smooth_scroll_default_mouse_filter_set", true)
	
	for child in node.get_children():
		_apply_mouse_filter_recursive(child)


## [member scroll_horizontal] 和 [member scroll_vertical] 属性的 getter。[br]
## 返回指定 [param property] 对应的滚动值。
func _get(property: StringName) -> Variant:
	match property:
		"scroll_horizontal":
			if not content_node: return 0
			return -int(pos.x)
		
		"scroll_vertical":
			if not content_node: return 0
			return -int(pos.y)
		
		_:
			return null


## [member scroll_horizontal] 和 [member scroll_vertical] 属性的 setter。[br]
## 将指定 [param property] 设为 [param value]，并相应更新滚动位置。
func _set(property: StringName, value: Variant) -> bool:
	match property:
		"scroll_horizontal":
			if not content_node:
				scroll_horizontal = 0
				return true
			
			if _initializing_margins: return true

			scroll_horizontal = value
			scrollbar_animator.kill_scroll_x_tween()
			velocity.x = 0.0
			var spare_size_x: float = ScrollLayout.get_spare_size_x(self, content_margins)
			pos.x = clampf(
				-value as float,
				-ScrollLayout.get_child_size_x_diff(content_node, spare_size_x, true),
				0.0
			)
			# Wake up process to ensure scrollbars update visually
			set_process(true) 
			return true
		
		"scroll_vertical":
			if not content_node:
				scroll_vertical = 0
				return true
			
			if _initializing_margins: return true

			scroll_vertical = value
			scrollbar_animator.kill_scroll_y_tween()
			velocity.y = 0.0
			var spare_size_y: float = ScrollLayout.get_spare_size_y(self, content_margins)
			pos.y = clampf(
				-value as float,
				-ScrollLayout.get_child_size_y_diff(content_node, spare_size_y, true),
				0.0
			)
			# Important: Wake up process to ensure scrollbars update visually
			set_process(true)
			return true
		_:
			return false
#endregion


#region Scrolling Logic
## 沿单个轴处理滚动。施加速度阻尼、越界拖拽力和边界吸附。[br]
## 依据 [param axis_velocity]、
## [param axis_pos] 和 [param delta] 时间，处理 [param vertical] 垂直轴或水平轴的滚动。
func scroll(vertical: bool, axis_velocity: float, axis_pos: float, delta: float) -> void:
	# Special case: If delta is 0, we treat this as a forced position set (teleport)
	# This allows callers to reset scroll state manually (e.g. scroll(true, 0, 0, 0))
	if is_zero_approx(delta):
		if vertical:
			velocity.y = axis_velocity
			pos.y = axis_pos
			if content_node:
				content_node.position.y = _base_offset.y + axis_pos
			if get_v_scroll_bar(): get_v_scroll_bar().set_value_no_signal(-axis_pos)
		else:
			velocity.x = axis_velocity
			pos.x = axis_pos
			if content_node:
				content_node.position.x = _base_offset.x + axis_pos
			if get_h_scroll_bar(): get_h_scroll_bar().set_value_no_signal(-axis_pos)
		return

	# If no scroll needed, don't apply forces
	if vertical:
		if not should_scroll_vertical(): 
			# Ensures position resets to 0 if content fits and auto-scroll disabled it
			if auto_allow_scroll and pos.y != 0 and content_node:
				var spare = ScrollLayout.get_spare_size_y(self, content_margins)
				var diff = ScrollLayout.get_child_size_y_diff(content_node, spare, false)
				if diff <= 0:
					velocity.y = 0.0
					pos.y = 0.0
					content_node.position.y = _base_offset.y + pos.y
			return
	
	else:
		if not should_scroll_horizontal(): 
			# Ensures position resets to 0 if content fits and auto-scroll disabled it
			if auto_allow_scroll and pos.x != 0 and content_node:
				var spare = ScrollLayout.get_spare_size_x(self, content_margins)
				var diff = ScrollLayout.get_child_size_x_diff(content_node, spare, false)
				if diff <= 0:
					velocity.x = 0.0
					pos.x = 0.0
					content_node.position.x = _base_offset.x + pos.x
			return
	
	if not scroll_damper: return
	# Applies counterforces when overdragging
	if not input_handler.content_dragging:
		axis_velocity = handle_overdrag(vertical, axis_velocity, axis_pos, delta)
		# Move content node by applying velocity
		var slide_result: Array = scroll_damper.slide(axis_velocity, delta)
		axis_velocity = slide_result[0]
		axis_pos += slide_result[1]
		# Snap to boundary if close enough
		var snap_result: Array = snap(vertical, axis_velocity, axis_pos)
		axis_velocity = snap_result[0]
		axis_pos = snap_result[1]
	else:
		# Preserve dragging velocity for 1 frame
		# in case no movement event while releasing dragging with touch
		if input_handler.content_dragging_moved:
			input_handler.content_dragging_moved = false
		else:
			axis_velocity = 0.0
	# If using scroll bar dragging, set the content_node's
	# position by using the scrollbar position
	if handle_scrollbar_drag():	return
	
	if vertical:
		if not allow_overdragging:
			# Clamp if calculated position is beyond boundary
			var spare_size_y: float = ScrollLayout.get_spare_size_y(self, content_margins)
			var size_diff_y: float = ScrollLayout.get_child_size_y_diff(content_node, spare_size_y, true)

			if ScrollLayout.is_outside_top_boundary(axis_pos):
				axis_pos = 0.0
				axis_velocity = 0.0
			
			elif ScrollLayout.is_outside_bottom_boundary(axis_pos, size_diff_y):
				axis_pos = -size_diff_y
				axis_velocity = 0.0
		content_node.position.y = _base_offset.y + axis_pos
		pos.y = axis_pos
		velocity.y = axis_velocity
	
	else:
		if not allow_overdragging:
			# Clamp if calculated position is beyond boundary
			var spare_size_x: float = ScrollLayout.get_spare_size_x(self, content_margins)
			var size_diff_x: float = ScrollLayout.get_child_size_x_diff(content_node, spare_size_x, true)

			if ScrollLayout.is_outside_left_boundary(axis_pos):
				axis_pos = 0.0
				axis_velocity = 0.0
			
			elif ScrollLayout.is_outside_right_boundary(axis_pos, size_diff_x):
				axis_pos = -size_diff_x
				axis_velocity = 0.0
			
		content_node.position.x = _base_offset.x + axis_pos
		pos.x = axis_pos
		velocity.x = axis_velocity


## 当内容被拖拽到边界之外时施加反向力。[br]
## 依据 [param axis_velocity]、[param axis_pos] 和 [param delta] 时间，
## 计算并施加针对 [param vertical] 垂直轴或水平轴的吸引力。
func handle_overdrag(vertical: bool, axis_velocity: float, axis_pos: float, delta: float) -> float:
	if not scroll_damper: return 0.0
	
	var spare_size: Vector2 = ScrollLayout.get_spare_size(self, content_margins)
	var size_diff: float = ScrollLayout.get_child_size_y_diff(content_node, spare_size.y, true) if vertical \
		else ScrollLayout.get_child_size_x_diff(content_node, spare_size.x, true)
	
	return ScrollPhysics.apply_overdrag(scroll_damper, axis_pos, axis_velocity, size_diff, delta)


## 当速度和距离都低于阈值时，将内容吸附到边界。[br]
## 使用 [param axis_velocity] 和 [param axis_pos] 检查 [param vertical] 垂直轴或水平轴。[br]
## 返回 [code][velocity, position][/code] 数组，其中的值可能已被吸附。
func snap(vertical: bool, axis_velocity: float, axis_pos: float) -> Array:
	var spare_size: Vector2 = ScrollLayout.get_spare_size(self, content_margins)
	var size_diff: float = ScrollLayout.get_child_size_y_diff(content_node, spare_size.y, true) if vertical \
		else ScrollLayout.get_child_size_x_diff(content_node, spare_size.x, true)
	
	return ScrollPhysics.apply_snap(axis_velocity, axis_pos, size_diff, just_snap_under)


## 处理滚动条拖拽输入，并相应更新内容位置。[br]
## 当某条滚动条正被拖拽时返回 [code]true[/code]。
func handle_scrollbar_drag() -> bool:
	if input_handler.h_scrollbar_dragging:
		velocity.x = 0.0
		pos.x = -get_h_scroll_bar().value
		content_node.position.x = _base_offset.x + pos.x
		return true
	
	if input_handler.v_scrollbar_dragging:
		velocity.y = 0.0
		pos.y = -get_v_scroll_bar().value
		content_node.position.y = _base_offset.y + pos.y
		return true
	
	return false


## 在内容被拖拽到边界之外时，处理带越界阻力的内容拖拽。
func handle_content_dragging() -> void:
	if not dragging_scroll_damper: return
	
	input_handler.content_dragging_moved = true
	
	if should_scroll_vertical():
		var y_pos: float = ScrollPhysics.calculate_drag_position(
			input_handler.drag_temp_data[6],  # Temp top_distance
			input_handler.drag_temp_data[7],  # Temp bottom_distance
			input_handler.drag_temp_data[1],  # Temp y relative accumulation
			input_handler.drag_temp_data[3],  # Y position where dragging started
			dragging_scroll_damper._attract_factor
		)
		velocity.y = (y_pos - pos.y) / get_process_delta_time()
		pos.y = y_pos
		content_node.position.y = _base_offset.y + y_pos
	
	if should_scroll_horizontal():
		var x_pos: float = ScrollPhysics.calculate_drag_position(
			input_handler.drag_temp_data[4],  # Temp left_distance
			input_handler.drag_temp_data[5],  # Temp right_distance
			input_handler.drag_temp_data[0],  # Temp x relative accumulation
			input_handler.drag_temp_data[2],  # X position where dragging started
			dragging_scroll_damper._attract_factor
		)
		velocity.x = (x_pos - pos.x) / get_process_delta_time()
		pos.x = x_pos
		content_node.position.x = _base_offset.x + x_pos


## 根据当前的拖拽状态、速度和活跃的补间动画更新 [member is_scrolling] 状态。
func update_is_scrolling() -> void:
	if(
		(input_handler.content_dragging and not input_handler.is_in_deadzone)
		or input_handler.any_scrollbar_dragging()
		or velocity != Vector2.ZERO
		or scrollbar_animator.has_active_scroll_tween()
	):
		is_scrolling = true
	
	else:
		is_scrolling = false


## 更新滚动条位置以匹配当前滚动位置，并在需要时显示滚动条。
func update_scrollbars() -> void:
	# Update vertical scroll bar
	if get_v_scroll_bar().value != -pos.y:
		get_v_scroll_bar().set_value_no_signal(-pos.y)
		get_v_scroll_bar().queue_redraw()
	
	# Update horizontal scroll bar
	if get_h_scroll_bar().value != -pos.x:
		get_h_scroll_bar().set_value_no_signal(-pos.x)
		get_h_scroll_bar().queue_redraw()
	
	# Always show sroll bars when scrolling or mouse is on any scroll bar
	if hide_scrollbar_over_time and (is_scrolling or input_handler.mouse_on_scrollbar):
		scrollbar_animator.show_scrollbars()
#endregion


#region Public API Functions
## 使用补间动画滚动到指定的水平位置。[br]
## 在 [param duration] 秒内将内容动画移动到 [param x_pos]。
func scroll_x_to(x_pos: float, duration := 0.5) -> void:
	if not should_scroll_horizontal(): return
	if input_handler.content_dragging: return
	_scroll_x_to_internal(x_pos, duration)


## 内部方法，用于水平滚动并跳过 should_scroll 检查。[br]
## 供 [method ensure_control_visible_smooth] 使用，以确保聚焦滚动始终生效。
func _scroll_x_to_internal(x_pos: float, duration := 0.5) -> void:
	if not content_node: return
	if not allow_horizontal_scroll: return
	
	set_process(true)
	velocity.x = 0.0
	var spare_size_x: float = ScrollLayout.get_spare_size_x(self, content_margins)
	var size_x_diff: float = ScrollLayout.get_child_size_x_diff(content_node, spare_size_x, true)
	x_pos = clampf(x_pos, -size_x_diff, 0.0)
	scrollbar_animator.kill_scroll_x_tween()
	scrollbar_animator.scroll_x_to(x_pos, duration)


## 使用补间动画滚动到指定的垂直位置。[br]
## 在 [param duration] 秒内将内容动画移动到 [param y_pos]。
func scroll_y_to(y_pos: float, duration := 0.5) -> void:
	if not should_scroll_vertical(): return
	if input_handler.content_dragging: return
	_scroll_y_to_internal(y_pos, duration)


## 内部方法，用于垂直滚动并跳过 should_scroll 检查。[br]
## 供 [method ensure_control_visible_smooth] 使用，以确保聚焦滚动始终生效。
func _scroll_y_to_internal(y_pos: float, duration := 0.5) -> void:
	if not content_node: return
	if not allow_vertical_scroll: return

	set_process(true)
	velocity.y = 0.0
	var spare_size_y: float = ScrollLayout.get_spare_size_y(self, content_margins)
	var size_y_diff: float = ScrollLayout.get_child_size_y_diff(content_node, spare_size_y, true)
	y_pos = clampf(y_pos, -size_y_diff, 0.0)
	scrollbar_animator.kill_scroll_y_tween()
	scrollbar_animator.scroll_y_to(y_pos, duration)


## 使用补间动画向上滚动一页。时长由 [param duration] 指定，单位为秒。
func scroll_page_up(duration := 0.5) -> void:
	var destination: float = pos.y + ScrollLayout.get_spare_size_y(self, content_margins)
	scroll_y_to(destination, duration)


## 使用补间动画向下滚动一页。时长由 [param duration] 指定，单位为秒。
func scroll_page_down(duration := 0.5) -> void:
	var destination: float = pos.y - ScrollLayout.get_spare_size_y(self, content_margins)
	scroll_y_to(destination, duration)


## 使用补间动画向左滚动一页。时长由 [param duration] 指定，单位为秒。
func scroll_page_left(duration := 0.5) -> void:
	var destination: float = pos.x + ScrollLayout.get_spare_size_x(self, content_margins)
	scroll_x_to(destination, duration)


## 使用补间动画向右滚动一页。时长由 [param duration] 指定，单位为秒。
func scroll_page_right(duration := 0.5) -> void:
	var destination: float = pos.x - ScrollLayout.get_spare_size_x(self, content_margins)
	scroll_x_to(destination, duration)


## 为垂直滚动增加速度，以实现基于惯性的滚动。[br]
## [param amount] 为正值时向上滚动，为负值时向下滚动。
func scroll_vertically(amount: float) -> void:
	velocity.y -= amount
	set_process(true)


## 为水平滚动增加速度，以实现基于惯性的滚动。[br]
## [param amount] 为正值时向左滚动，为负值时向右滚动。
func scroll_horizontally(amount: float) -> void:
	velocity.x -= amount
	set_process(true)


## 使用补间动画滚动到顶部。时长由 [param duration] 指定，单位为秒。
func scroll_to_top(duration := 0.5) -> void:
	scroll_y_to(0.0, duration)


## 使用补间动画滚动到底部。时长由 [param duration] 指定，单位为秒。
func scroll_to_bottom(duration := 0.5) -> void:
	var spare_size_y: float = ScrollLayout.get_spare_size_y(self, content_margins)
	scroll_y_to(spare_size_y - content_node.size.y, duration)


## 使用补间动画滚动到最左侧位置。时长由 [param duration] 指定，单位为秒。
func scroll_to_left(duration := 0.5) -> void:
	scroll_x_to(0.0, duration)


## 使用补间动画滚动到最右侧位置。时长由 [param duration] 指定，单位为秒。
func scroll_to_right(duration := 0.5) -> void:
	var spare_size_x: float = ScrollLayout.get_spare_size_x(self, content_margins)
	scroll_x_to(spare_size_x - content_node.size.x, duration)


## 立即将滚动位置和速度重置为零。
func reset_scroll() -> void:
	velocity = Vector2.ZERO
	pos = Vector2.ZERO
	scrollbar_animator.kill_scroll_tweens()
	if content_node:
		# Reset position based on current base offset or 0 if margin layout
		# Usually we just want to apply 0 scroll:
		content_node.position = _base_offset
	
	# Sync native scrollbars
	if get_v_scroll_bar(): get_v_scroll_bar().value = 0
	if get_h_scroll_bar(): get_h_scroll_bar().value = 0


## 当内容高度足够进行垂直滚动时返回 [code]true[/code]。
func should_scroll_vertical() -> bool:
	var spare_size_y: float = ScrollLayout.get_spare_size_y(self, content_margins)
	var child_size_diff: float = ScrollLayout.get_child_size_y_diff(content_node, spare_size_y, false)
	var disable_scroll: bool = (not allow_vertical_scroll) \
		or (auto_allow_scroll and child_size_diff <= 0) \
		or not scroll_damper
	
	if disable_scroll:
		velocity.y = 0.0
		return pos.y != 0
	
	else:
		return true


## 当内容宽度足够进行水平滚动时返回 [code]true[/code]。
func should_scroll_horizontal() -> bool:
	var spare_size_x: float = ScrollLayout.get_spare_size_x(self, content_margins)
	var child_size_diff: float = ScrollLayout.get_child_size_x_diff(content_node, spare_size_x, false)
	var disable_scroll: bool = (not allow_horizontal_scroll) \
		or (auto_allow_scroll and child_size_diff <= 0) \
		or not scroll_damper
	
	if disable_scroll:
		velocity.x = 0.0
		return pos.x != 0
	
	else:
		return true


## 重写内置方法，以使用平滑滚动。
func ensure_control_visible(control: Control) -> void:
	ensure_control_visible_smooth(control)


## 平滑滚动，以确保给定的 [param control] 节点带着动画变为可见。[br]
## 用于替代内置的 [method ScrollContainer.ensure_control_visible] 函数。
func ensure_control_visible_smooth(control: Control) -> void:
	if not content_node: return
	if not content_node.is_ancestor_of(control): return
	if not scroll_damper: return
	
	# If container size is 0, wait for first valid size then track resizes
	var current_size := size
	if current_size.x <= 0 or current_size.y <= 0:
		_pending_ensure_control = control
		if not resized.is_connected(_on_resized_for_ensure_continuous):
			resized.connect(_on_resized_for_ensure_continuous)
		return
	
	# Execute the scroll
	_execute_ensure_control_visible(control, false)
	
	# Track this control for continuous updates during resize
	_pending_ensure_control = control
	_last_ensure_size = current_size
	if not resized.is_connected(_on_resized_for_ensure_continuous):
		resized.connect(_on_resized_for_ensure_continuous)
	
	# Start a timer to stop tracking after things stabilize
	_start_stability_timer()


## 启动或重新启动稳定性计时器
func _start_stability_timer() -> void:
	if not _ensure_stability_timer:
		_ensure_stability_timer = Timer.new()
		_ensure_stability_timer.one_shot = true
		_ensure_stability_timer.timeout.connect(_on_stability_timeout)
		add_child(_ensure_stability_timer)
	_ensure_stability_timer.start(0.6)  # Stop tracking after 600ms of no resize


## 稳定性计时器超时时调用——停止跟踪尺寸变化
func _on_stability_timeout() -> void:
	if resized.is_connected(_on_resized_for_ensure_continuous):
		resized.disconnect(_on_resized_for_ensure_continuous)
	_pending_ensure_control = null
	_last_ensure_size = Vector2.ZERO


## 在跟踪某个控件期间，每次尺寸变化时调用
func _on_resized_for_ensure_continuous() -> void:
	var current_size: Vector2 = size
	if current_size.x <= 0 or current_size.y <= 0: return
	
	if _pending_ensure_control and is_instance_valid(_pending_ensure_control):
		_execute_ensure_control_visible(_pending_ensure_control, true)
		_last_ensure_size = current_size
		_start_stability_timer()


## 实际执行 ensure_control_visible 的逻辑。[br]
## 若 [param instant] 为 true，则直接设置位置而不播放动画
func _execute_ensure_control_visible(control: Control, instant: bool) -> void:
	var control_rect: Rect2 = control.get_global_rect()
	var content_rect: Rect2 = content_node.get_global_rect()
	
	var control_in_content: Vector2 = control_rect.position - content_rect.position
	var visible_size := Vector2(
		ScrollLayout.get_spare_size_x(self, content_margins),
		ScrollLayout.get_spare_size_y(self, content_margins)
	)
	
	set_process(true)
	
	var target_x: float = pos.x
	var target_y: float = pos.y
	
	# Calculate where control currently appears relative to visible area
	var control_top_in_view: float = control_in_content.y + pos.y
	var control_bottom_in_view: float = control_top_in_view + control_rect.size.y
	var control_left_in_view: float = control_in_content.x + pos.x
	var control_right_in_view: float = control_left_in_view + control_rect.size.x
	
	# Horizontal scrolling
	if control_left_in_view < follow_focus_margin:
		target_x = -(control_in_content.x - follow_focus_margin)
	elif control_right_in_view > visible_size.x - follow_focus_margin:
		target_x = -(control_in_content.x + control_rect.size.x - visible_size.x + follow_focus_margin)
	
	# Vertical scrolling
	if control_top_in_view < follow_focus_margin:
		target_y = -(control_in_content.y - follow_focus_margin)
	elif control_bottom_in_view > visible_size.y - follow_focus_margin:
		target_y = -(control_in_content.y + control_rect.size.y - visible_size.y + follow_focus_margin)
	
	# Clamp targets
	var spare_size_x: float = ScrollLayout.get_spare_size_x(self, content_margins)
	var spare_size_y: float = ScrollLayout.get_spare_size_y(self, content_margins)
	var size_x_diff: float = ScrollLayout.get_child_size_x_diff(content_node, spare_size_x, true)
	var size_y_diff: float = ScrollLayout.get_child_size_y_diff(content_node, spare_size_y, true)
	
	target_x = clampf(target_x, -size_x_diff, 0.0)
	target_y = clampf(target_y, -size_y_diff, 0.0)
	
	# Set position directly, no animation
	if instant:
		scrollbar_animator.kill_scroll_tweens()
		pos.x = target_x
		pos.y = target_y
		content_node.position.x = _base_offset.x + target_x
		content_node.position.y = _base_offset.y + target_y
	
	# Use animated scroll
	else:
		if target_x != pos.x:
			_scroll_x_to_internal(target_x)
		if target_y != pos.y:
			_scroll_y_to_internal(target_y)
#endregion
