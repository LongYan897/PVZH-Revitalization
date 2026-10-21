class_name ScrollPhysics
extends RefCounted
## 用于 [SmoothScrollContainer] 物理计算的静态工具类。
##
## 负责处理越界拖拽时的作用力、边界吸附以及速度计算。


## 内容被拉出边界时施加的拖拽阻尼系数
const OVERDRAG_DAMPING: float = 0.00001


## 使用 [param scroll_damper] 在内容被拖出边界时施加反向作用力。[br]
## 根据 [param axis_pos]、[param axis_velocity]、[param size_diff]
## 以及 [param delta] 时间计算回弹力与吸引力，并返回修改后的速度。
static func apply_overdrag(
	scroll_damper: ScrollDamper,
	axis_pos: float,
	axis_velocity: float,
	size_diff: float,
	delta: float
) -> float:
	if not scroll_damper:
		return axis_velocity
	
	# 计算到各边界的距离
	var dist_to_start: float = axis_pos
	var dist_to_end: float = axis_pos + size_diff
	
	# 计算回到边界所需的目标速度
	var target_vel_start: float = scroll_damper._calculate_velocity_to_dest(dist_to_start, 0.0)
	var target_vel_end: float = scroll_damper._calculate_velocity_to_dest(dist_to_end, 0.0)
	
	# 当超出边界且速度不足以回到边界时施加吸引力
	if axis_pos > 0.0 and axis_velocity > target_vel_start:
		axis_velocity = scroll_damper.attract(dist_to_start, 0.0, axis_velocity, delta)
	elif axis_pos < -size_diff and axis_velocity < target_vel_end:
		axis_velocity = scroll_damper.attract(dist_to_end, 0.0, axis_velocity, delta)
	
	return axis_velocity


## 当速度与距离都低于 [param snap_threshold] 时将内容吸附到边界。[br]
## 通过 [param axis_velocity]、[param axis_pos] 与 [param size_diff] 判断是否吸附。[br]
## 返回 [code][velocity, position][/code] 数组，其中的值可能已被吸附。
static func apply_snap(
	axis_velocity: float,
	axis_pos: float,
	size_diff: float,
	snap_threshold: float
) -> Array:
	var dist_to_start: float = axis_pos
	var dist_to_end: float = axis_pos + size_diff
	
	# 吸附到起始边界
	if (
		abs(dist_to_start) < snap_threshold
		and abs(axis_velocity) < snap_threshold
	):
		axis_pos = 0.0
		axis_velocity = 0.0
	# 吸附到结束边界
	elif (
		abs(dist_to_end) < snap_threshold
		and abs(axis_velocity) < snap_threshold
	):
		axis_pos = -size_diff
		axis_velocity = 0.0
	
	return [axis_velocity, axis_pos]


## 计算带越界阻尼的拖拽目的地位置。[br]
## 对正在拖拽的 [param delta] 距离施加 [param damping_factor]。
static func calculate_overdrag_dest(delta: float, damping_factor: float) -> float:
	if delta >= 0.0:
		return delta / (1.0 + delta * damping_factor * OVERDRAG_DAMPING)
	else:
		return delta


## 计算拖拽内容时带边界越界阻力的位置。[br]
## 使用 [param temp_dist_start]、[param temp_dist_end]、[param temp_relative]、[param drag_start_pos]
## 以及 [param damping_factor]，在完成越界计算后得出最终位置。
static func calculate_drag_position(
	temp_dist_start: float,
	temp_dist_end: float,
	temp_relative: float,
	drag_start_pos: float,
	damping_factor: float
) -> float:
	# 拖拽超出起始边界
	if temp_relative + temp_dist_start > 0.0:
		var delta: float = min(temp_relative, temp_relative + temp_dist_start)
		var dest: float = calculate_overdrag_dest(delta, damping_factor)
		return dest - min(0.0, temp_dist_start) + drag_start_pos
	# 拖拽超出结束边界
	elif temp_relative + temp_dist_end < 0.0:
		var delta: float = max(temp_relative, temp_relative + temp_dist_end)
		var dest: float = -calculate_overdrag_dest(-delta, damping_factor)
		return dest - max(0.0, temp_dist_end) + drag_start_pos
	# 在边界范围内
	else:
		return temp_relative + drag_start_pos
