class_name ScrollbarAnimator
extends RefCounted
## 处理滚动条可见性动画，带淡入/淡出效果。
##
## 管理用于显示与隐藏滚动条的补间动画，
## 包括自动隐藏功能的定时器管理。


#region Variables
## ScrollContainer 引用
var _container: ScrollContainer = null
## 用于自动隐藏滚动条的定时器
var _hide_timer: Timer = null
## 用于淡入动画的补间
var _show_tween: Tween = null
## 用于淡出动画的补间
var _hide_tween: Tween = null
## 用于水平滚动动画的补间
var _scroll_x_tween: Tween = null
## 用于垂直滚动动画的补间
var _scroll_y_tween: Tween = null
## 滚动条开始淡出前的持续时间
var hide_time: float = 5.0
## 淡入动画的持续时间
var fade_in_time: float = 0.2
## 淡出动画的持续时间
var fade_out_time: float = 0.5
#endregion


## 使用对 [param container] 的引用初始化动画器。
func _init(container: ScrollContainer) -> void:
	_container = container
	_hide_timer = Timer.new()
	_hide_timer.one_shot = true
	_hide_timer.timeout.connect(_on_hide_timer_timeout)
	container.add_child(_hide_timer)


## 启动自动隐藏定时器。
func start_hide_timer() -> void:
	if _hide_timer and _hide_timer.is_inside_tree():
		_hide_timer.start(hide_time)


## 停止自动隐藏定时器。
func stop_hide_timer() -> void:
	if _hide_timer:
		_hide_timer.stop()


## 根据拖拽状态检查滚动条是否应当隐藏。[br]
## 当 [param h_scrollbar_dragging] 与 [param v_scrollbar_dragging] 均未激活时返回 [code]true[/code]。
func should_hide(h_scrollbar_dragging: bool, v_scrollbar_dragging: bool) -> bool:
	return not h_scrollbar_dragging and not v_scrollbar_dragging


## 在指定的 [param time] 时长内淡入滚动条。[br]
## 当 [param time] 为负数时，改用 [member fade_in_time]。
func show_scrollbars(time: float = -1.0) -> void:
	if time < 0.0:
		time = fade_in_time
	
	# 重启隐藏定时器
	start_hide_timer()
	
	# 终止冲突的动画
	if _hide_tween and _hide_tween.is_valid():
		_hide_tween.kill()
	
	var v_scroll_bar: ScrollBar = _container.get_v_scroll_bar()
	var h_scroll_bar: ScrollBar = _container.get_h_scroll_bar()
	
	# 仅在滚动条尚未完全可见时创建补间
	if v_scroll_bar.modulate != Color.WHITE or h_scroll_bar.modulate != Color.WHITE:
		if _show_tween and _show_tween.is_running():
			_show_tween.kill()
		
		_show_tween = _container.create_tween()
		_show_tween.set_parallel(true)
		_show_tween.tween_property(v_scroll_bar, "modulate", Color.WHITE, time)
		_show_tween.tween_property(h_scroll_bar, "modulate", Color.WHITE, time)


## 在指定的 [param time] 时长内淡出滚动条。[br]
## 当 [param time] 为负数时，改用 [member fade_out_time]。
func hide_scrollbars(time: float = -1.0) -> void:
	if time < 0.0:
		time = fade_out_time
	
	# 终止冲突的动画
	if _show_tween and _show_tween.is_valid():
		_show_tween.kill()
	
	var v_scroll_bar: ScrollBar = _container.get_v_scroll_bar()
	var h_scroll_bar: ScrollBar = _container.get_h_scroll_bar()
	
	# 仅在滚动条尚未完全隐藏时创建补间
	if v_scroll_bar.modulate != Color.TRANSPARENT or h_scroll_bar.modulate != Color.TRANSPARENT:
		if _hide_tween and _hide_tween.is_running():
			_hide_tween.kill()
		
		_hide_tween = _container.create_tween()
		_hide_tween.set_parallel(true)
		_hide_tween.tween_property(v_scroll_bar, "modulate", Color.TRANSPARENT, time)
		_hide_tween.tween_property(h_scroll_bar, "modulate", Color.TRANSPARENT, time)


## 立即将滚动条设为完全可见，不使用动画。
func show_scrollbars_immediate() -> void:
	if _show_tween: _show_tween.kill()
	if _hide_tween:	_hide_tween.kill()
	
	_container.get_v_scroll_bar().modulate = Color.WHITE
	_container.get_h_scroll_bar().modulate = Color.WHITE


## 清理补间与定时器。
func cleanup() -> void:
	if _show_tween: _show_tween.kill()
	if _hide_tween: _hide_tween.kill()
	
	if _scroll_x_tween:	_scroll_x_tween.kill()
	if _scroll_y_tween:	_scroll_y_tween.kill()
	if _hide_timer:	_hide_timer.queue_free()


## 在指定的 [param duration] 内将水平滚动动画到 [param target_pos]。
func scroll_x_to(target_pos: float, duration: float = 0.5) -> void:
	if _scroll_x_tween and _scroll_x_tween.is_valid():
		_scroll_x_tween.kill()
	
	_scroll_x_tween = _container.create_tween()
	_scroll_x_tween.set_parallel(true)
	var pos_tweener: PropertyTweener = _scroll_x_tween.tween_property(
		_container,
		"pos:x",
		target_pos,
		duration
	)
	pos_tweener.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUINT)
	
	# 同时直接对 content_node 的位置做补间，确保即使 scroll() 提前返回也能移动
	if _container.content_node:
		var content_tweener: PropertyTweener = _scroll_x_tween.tween_property(
			_container.content_node,
			"position:x",
			_container._base_offset.x + target_pos,
			duration
		)
		content_tweener.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUINT)


## 在指定的 [param duration] 内将垂直滚动动画到 [param target_pos]。
func scroll_y_to(target_pos: float, duration: float = 0.5) -> void:
	if _scroll_y_tween and _scroll_y_tween.is_valid():
		_scroll_y_tween.kill()
	
	_scroll_y_tween = _container.create_tween()
	_scroll_y_tween.set_parallel(true)
	var pos_tweener: PropertyTweener = _scroll_y_tween.tween_property(
		_container,
		"pos:y",
		target_pos,
		duration
	)
	pos_tweener.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUINT)
	
	# 同时直接对 content_node 的位置做补间，确保即使 scroll() 提前返回也能移动
	if _container.content_node:
		var target_content_pos = _container._base_offset.y + target_pos
		var content_tweener: PropertyTweener = _scroll_y_tween.tween_property(
			_container.content_node,
			"position:y",
			target_content_pos,
			duration
		)
		content_tweener.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUINT)


## 如果水平滚动补间存在，则终止它。
func kill_scroll_x_tween() -> void:
	if _scroll_x_tween and _scroll_x_tween.is_valid():
		_scroll_x_tween.kill()


## 如果垂直滚动补间存在，则终止它。
func kill_scroll_y_tween() -> void:
	if _scroll_y_tween and _scroll_y_tween.is_valid():
		_scroll_y_tween.kill()


## 终止水平与垂直滚动补间。
func kill_scroll_tweens() -> void:
	kill_scroll_x_tween()
	kill_scroll_y_tween()


## 如果当前有任何滚动补间处于活动状态，返回 [code]true[/code]。
func has_active_scroll_tween() -> bool:
	return (_scroll_x_tween and _scroll_x_tween.is_valid() and _scroll_x_tween.is_running()) \
		or (_scroll_y_tween and _scroll_y_tween.is_valid() and _scroll_y_tween.is_running())


## 当隐藏定时器超时时调用。
func _on_hide_timer_timeout() -> void:
	hide_scrollbars()
