extends Node
class_name Calc
static func aease(x: float, ease_name: String, ease_dir: int) -> float:
	x = clamp(x, 0.0, 1.0)
	var name_lower = ease_name.to_lower()
	
	# 线性（忽略方向）
	if name_lower == "linear":
		return x
	
	# Quad (二次方)
	if name_lower == "quad":
		match ease_dir:
			1:  # In
				return x * x
			2:  # Out
				return x * (2 - x)
			3:  # InOut
				x *= 2
				if x < 1:
					return 0.5 * x * x
				x -= 1
				return -0.5 * (x * (x - 2) - 1)
	
	# Cubic (三次方)
	if name_lower == "cubic":
		match ease_dir:
			1:  # In
				return x * x * x
			2:  # Out
				x -= 1
				return x * x * x + 1
			3:  # InOut
				x *= 2
				if x < 1:
					return 0.5 * x * x * x
				x -= 2
				return 0.5 * (x * x * x + 2)
	
	# Quart (四次方)
	if name_lower == "quart":
		match ease_dir:
			1:  # In
				return x * x * x * x
			2:  # Out
				x -= 1
				return -(x * x * x * x - 1)
			3:  # InOut
				x *= 2
				if x < 1:
					return 0.5 * x * x * x * x
				x -= 2
				return -0.5 * (x * x * x * x - 2)
	
	# Quint (五次方)
	if name_lower == "quint":
		match ease_dir:
			1:  # In
				return x * x * x * x * x
			2:  # Out
				x -= 1
				return x * x * x * x * x + 1
			3:  # InOut
				x *= 2
				if x < 1:
					return 0.5 * x * x * x * x * x
				x -= 2
				return 0.5 * (x * x * x * x * x + 2)
	
	# Sine (正弦)
	if name_lower == "sine":
		match ease_dir:
			1:  # In
				return 1 - cos(x * PI / 2)
			2:  # Out
				return sin(x * PI / 2)
			3:  # InOut
				return -0.5 * (cos(PI * x) - 1)
	
	# Expo (指数)
	if name_lower == "expo":
		match ease_dir:
			1:  # In
				if x == 0:
					return 0
				else:
					return pow(2, 10 * (x - 1))
			2:  # Out
				if x == 1:
					return 1
				else:
					return 1 - pow(2, -10 * x)
			3:  # InOut
				if x == 0 or x == 1:
					return x
				x *= 2
				if x < 1:
					return 0.5 * pow(2, 10 * (x - 1))
				return 0.5 * (2 - pow(2, -10 * (x - 1)))
	
	# Circ (圆形)
	if name_lower == "circ":
		match ease_dir:
			1:  # In
				return 1 - sqrt(1 - x * x)
			2:  # Out
				return sqrt(1 - (x - 1) * (x - 1))
			3:  # InOut
				x *= 2
				if x < 1:
					return -0.5 * (sqrt(1 - x * x) - 1)
				x -= 2
				return 0.5 * (sqrt(1 - x * x) + 1)
	
	# Back (回退)
	if name_lower == "back":
		var s: float = 1.70158
		match ease_dir:
			1:  # In
				return x * x * ((s + 1) * x - s)
			2:  # Out
				x -= 1
				return x * x * ((s + 1) * x + s) + 1
			3:  # InOut
				s *= 1.525
				x *= 2
				if x < 1:
					return 0.5 * (x * x * ((s + 1) * x - s))
				x -= 2
				return 0.5 * (x * x * ((s + 1) * x + s) + 2)
	
	# Bounce (弹跳)
	if name_lower == "bounce":
		match ease_dir:
			1:  # In
				return 1 -  aease(1 - x, "bounce", 2)
			2:  # Out
				if x < 1 / 2.75:
					return 7.5625 * x * x
				elif x < 2 / 2.75:
					x -= 1.5 / 2.75
					return 7.5625 * x * x + 0.75
				elif x < 2.5 / 2.75:
					x -= 2.25 / 2.75
					return 7.5625 * x * x + 0.9375
				else:
					x -= 2.625 / 2.75
					return 7.5625 * x * x + 0.984375
			3:  # InOut
				if x < 0.5:
					return 0.5 * aease(x * 2, "bounce", 1)
				return 0.5 * aease(x * 2 - 1, "bounce", 2) + 0.5
	
	# Elastic (弹性)
	if name_lower == "elastic":
		match ease_dir:
			1:  # In
				if x == 0 or x == 1:
					return x
				return -pow(2, 10 * (x - 1)) * sin((x - 1.1) * 5 * PI)
			2:  # Out
				if x == 0 or x == 1:
					return x
				return pow(2, -10 * x) * sin((x - 0.1) * 5 * PI) + 1
			3:  # InOut
				if x == 0 or x == 1:
					return x
				x *= 2
				if x < 1:
					return -0.5 * pow(2, 10 * (x - 1)) * sin((x - 1.1) * 5 * PI)
				x -= 1
				return 0.5 * pow(2, -10 * x) * sin((x - 0.1) * 5 * PI) + 1
	
	return x  # 默认返回线性
