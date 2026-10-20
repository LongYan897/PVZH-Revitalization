class_name QuadScrollDamper
extends ScrollDamper
## 二次方滚动阻尼。
##
## 施加二次方减速曲线（x²）。缓动曲线强度中等，
## 介于线性曲线与指数曲线之间。


#region Variables
const QUADRATIC_POWER: float = 2.0
const CUBIC_POWER: float = 3.0
const CUBIC_COEFFICIENT: float = 1.0 / 3.0
const FRICTION_BASE: float = 10.0
const FRICTION_OFFSET: float = 1.0
const MIN_FRICTION: float = 0.001
const MIN_FACTOR: float = 0.000000000001

## 用于减速的摩擦系数。
## 数值越高，减速越猛烈。
@export_range(0.001, 10000.0, 0.001, "or_greater", "hide_slider")
var friction: float = 4.0: set = _set_friction

## 由摩擦系数推导出的内部二次方系数。
var _factor: float = 10000.0: set = _set_factor
#endregion


## 使用二次方曲线计算给定 [param time] 处的速度。
func _calculate_velocity_by_time(time: float) -> float:
	if time <= 0.0:
		return 0.0
	return pow(time, QUADRATIC_POWER) * _factor


## 计算达到给定 [param velocity] 所需的时间。
func _calculate_time_by_velocity(velocity: float) -> float:
	return sqrt(abs(velocity) / _factor)


## 计算给定 [param time] 处已移动的位移。
func _calculate_offset_by_time(time: float) -> float:
	time = max(time, 0.0)
	return CUBIC_COEFFICIENT * _factor * pow(time, CUBIC_POWER)


## 计算移动给定 [param offset] 距离所需的时间。
func _calculate_time_by_offset(offset: float) -> float:
	return pow(abs(offset) * CUBIC_POWER / _factor, 1.0 / CUBIC_POWER)


#region Setters
func _set_friction(value: float) -> void:
	friction = max(value, MIN_FRICTION)
	_factor = pow(FRICTION_BASE, friction) - FRICTION_OFFSET


func _set_factor(value: float) -> void:
	_factor = max(value, MIN_FACTOR)
#endregion
