class_name ExpoScrollDamper
extends ScrollDamper
## 指数滚动阻尼。
##
## 起始速度快，随后沿指数曲线逐渐减速。


#region Variables
const FRICTION_BASE: float = 10.0
const MIN_FACTOR: float = 1.000000000001
const MIN_FRICTION: float = 0.001
const MIN_VELOCITY: float = 0.001

## 用于减速的摩擦系数。
## 数值越高，减速越猛烈。
@export_range(0.001, 10000.0, 0.001, "or_greater", "hide_slider")
var friction: float = 4.0: set = _set_friction

## 停止前的最小速度阈值。
## 当速度降到该值以下时，滚动停止。
@export_range(0.001, 100000.0, 0.001, "or_greater", "hide_slider")
var minimum_velocity: float = 0.4: set = _set_minimum_velocity

## 由摩擦系数推导出的内部指数系数。
var _factor: float = 10000.0: set = _set_factor
#endregion


## 使用指数曲线计算给定 [param time] 处的速度。
func _calculate_velocity_by_time(time: float) -> float:
	var minimum_time: float = _calculate_time_by_velocity(minimum_velocity)
	if time <= minimum_time:
		return 0.0
	return pow(_factor, time)


## 计算达到给定 [param velocity] 所需的时间。
func _calculate_time_by_velocity(velocity: float) -> float:
	return log(abs(velocity)) / log(_factor)


## 计算给定 [param time] 处已移动的位移。
func _calculate_offset_by_time(time: float) -> float:
	return pow(_factor, time) / log(_factor)


## 计算移动给定 [param offset] 距离所需的时间。
func _calculate_time_by_offset(offset: float) -> float:
	return log(offset * log(_factor)) / log(_factor)


## 计算到达目标位置所需的速度。[br]
## 重写基类实现，以考虑 [member minimum_velocity]。[br]
## 计算从 [param from] 位置移动到 [param to] 位置所需的速度。
func _calculate_velocity_to_dest(from: float, to: float) -> float:
	var dist: float = to - from
	var min_time: float = _calculate_time_by_velocity(minimum_velocity)
	var min_offset: float = _calculate_offset_by_time(min_time)
	var time: float = _calculate_time_by_offset(abs(dist) + min_offset)
	var vel: float = _calculate_velocity_by_time(time) * sign(dist)
	return vel


#region Setters
func _set_friction(value: float) -> void:
	friction = max(value, MIN_FRICTION)
	_factor = pow(FRICTION_BASE, friction)


func _set_factor(value: float) -> void:
	_factor = max(value, MIN_FACTOR)


func _set_minimum_velocity(value: float) -> void:
	minimum_velocity = max(value, MIN_VELOCITY)
#endregion
