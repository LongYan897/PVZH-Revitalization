@icon("icon.svg")
class_name ScrollDamper
extends Resource
## 滚动阻尼算法的基类。
##
## 它定义了滚动如何随时间减速。不同的阻尼器使用不同的
## 数学曲线，以营造各种不同的滚动手感。
## 子类将为每种风格实现实际的数学计算。


#region Variables
## 内容被拉出边界时的回弹强度。
## 数值越高，吸引回边界的速度越快。
@export_range(0.0, 1.0, 0.001, "or_greater", "hide_slider")
var rebound_strength: float = 7.0: set = _set_rebound_strength

## 用于计算吸引力的内部系数。
var _attract_factor: float = 400.0: set = _set_attract_factor
#endregion


## 抽象方法。计算给定 [param time] 值处的速度。
func _calculate_velocity_by_time(time: float) -> float:
	return 0.0


## 抽象方法。计算达到给定 [param velocity] 所需的时间。
func _calculate_time_by_velocity(velocity: float) -> float:
	return 0.0


## 抽象方法。计算给定 [param time] 值处已移动的位移。
func _calculate_offset_by_time(time: float) -> float:
	return 0.0


## 抽象方法。计算移动给定 [param offset] 距离所需的时间。
func _calculate_time_by_offset(offset: float) -> float:
	return 0.0


## 计算到达目标位置所需的速度。[br]
## 计算从 [param from] 位置移动到 [param to] 位置所需的速度。
func _calculate_velocity_to_dest(from: float, to: float) -> float:
	var dist: float = to - from
	var time: float = _calculate_time_by_offset(abs(dist))
	var vel: float = _calculate_velocity_by_time(time) * sign(dist)
	return vel


## 计算推进时间后的下一速度。[br]
## 返回 [param present_time] 减去 [param delta_time] 时刻的速度。
func _calculate_next_velocity(present_time: float, delta_time: float) -> float:
	return _calculate_velocity_by_time(present_time - delta_time)


## 计算一个时间步长内的位置变化。[br]
## 返回 [param present_time] 与 [param present_time] 减去 [param delta_time] 之间的位移差。
func _calculate_next_offset(present_time: float, delta_time: float) -> float:
	return _calculate_offset_by_time(present_time) \
		 - _calculate_offset_by_time(present_time - delta_time)


## 在 [param delta_time] 内对 [param velocity] 施加阻尼。[br]
## 返回数组：[code][next_velocity, position_change][/code]。
func slide(velocity: float, delta_time: float) -> Array:
	var present_time: float = _calculate_time_by_velocity(velocity)
	return [
		_calculate_next_velocity(present_time, delta_time) * sign(velocity),
		_calculate_next_offset(present_time, delta_time) * sign(velocity)
	]


## 过度拖动时施加朝向目标的吸引力。[br]
## 将内容从 [param from] 位置拉向 [param to] 位置，在 [param delta_time] 内修改 [param velocity]，
## 并返回修改后的速度。
func attract(from: float, to: float, velocity: float, delta_time: float) -> float:
	var dist: float = to - from
	var target_vel: float = _calculate_velocity_to_dest(from, to)
	velocity += _attract_factor * dist * delta_time \
		 + _calculate_velocity_by_time(delta_time) * sign(dist)
	if (
		(dist > 0 and velocity >= target_vel) \
		or (dist < 0 and velocity <= target_vel) \
	):
		velocity = target_vel
	return velocity


#region Setters
func _set_rebound_strength(value: float) -> void:
	rebound_strength = max(value, 0.0)
	_attract_factor = rebound_strength * rebound_strength * rebound_strength


func _set_attract_factor(value: float) -> void:
	_attract_factor = max(value, 0.0)
#endregion
