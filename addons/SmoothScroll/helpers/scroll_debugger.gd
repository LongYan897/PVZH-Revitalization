class_name ScrollDebugger
extends RefCounted
## 用于 [SmoothScrollContainer] 调试可视化的静态工具类。
##
## 该类为开发调试提供绘制调试图形的函数。


## 用于视觉调试的调试渐变（绿色 = 安全，红色 = 过度拖拽）
static var debug_gradient: Gradient = null


## 为调试可视化初始化渐变。
static func setup_debug_drawing() -> void:
	if debug_gradient == null:
		debug_gradient = Gradient.new()
		debug_gradient.set_color(0.0, Color.GREEN)
		debug_gradient.set_color(1.0, Color.RED)


## 为指定的 [param container] 绘制调试可视化。[br]
## 使用彩色线条显示过度拖拽距离与速度指示。
static func draw_debug(container: SmoothScrollContainer) -> void:
	if not container.content_node: return
	
	# 计算容器与 content_node 之间的大小差值
	var spare_size: Vector2 = ScrollLayout.get_spare_size(container, container.content_margins)
	var size_diff: Vector2 = ScrollLayout.get_child_size_diff(
		container.content_node,
		spare_size,
		false,
		false
	)
	
	# 计算到左、右、上、下的距离
	var boundary_dist: Vector4 = ScrollLayout.get_boundary_dist(
		container.pos,
		size_diff
	)
	var bottom_distance: float = boundary_dist.w
	var top_distance: float = boundary_dist.z
	var right_distance: float = boundary_dist.y
	var left_distance: float = boundary_dist.x
	
	# 过度拖拽线条
	# 上 + 下
	container.draw_line(
		Vector2(0.0, 0.0),
		Vector2(0.0, top_distance),
		debug_gradient.sample(clamp(top_distance / container.size.y, 0.0, 1.0)),
		5.0
	)
	container.draw_line(
		Vector2(0.0, container.size.y),
		Vector2(0.0, container.size.y + bottom_distance),
		debug_gradient.sample(clamp(-bottom_distance / container.size.y, 0.0, 1.0)),
		5.0
	)
	
	# 左 + 右
	container.draw_line(
		Vector2(0.0, container.size.y),
		Vector2(left_distance, container.size.y),
		debug_gradient.sample(clamp(left_distance / container.size.y, 0.0, 1.0)),
		5.0
	)
	container.draw_line(
		Vector2(container.size.x, container.size.y),
		Vector2(container.size.x + right_distance, container.size.y),
		debug_gradient.sample(clamp(-right_distance / container.size.y, 0.0, 1.0)),
		5.0
	)
	
	# 速度线条
	var origin := Vector2(5.0, container.size.y / 2)
	container.draw_line(
		origin,
		origin + Vector2(0.0, container.velocity.y * 0.01),
		debug_gradient.sample(clamp(container.velocity.y * 2 / container.size.y, 0.0, 1.0)),
		5.0
	)
	container.draw_line(
		origin,
		origin + Vector2(0.0, container.velocity.x * 0.01),
		debug_gradient.sample(clamp(container.velocity.x * 2 / container.size.x, 0.0, 1.0)),
		5.0
	)
