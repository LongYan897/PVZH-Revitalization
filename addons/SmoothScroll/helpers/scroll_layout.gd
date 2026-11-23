class_name ScrollLayout
extends RefCounted
## 用于 [SmoothScrollContainer] 布局与边界计算的静态工具类。
##
## 提供计算容器尺寸、内容尺寸、到边界距离的函数，
## 并负责外边距与布局管理。


## 计算 X 轴上不含垂直滚动条宽度的容器可用尺寸。[br]
## 使用 [param container] 的尺寸与 [param content_margins] 进行计算。
static func get_spare_size_x(container: Control, content_margins: Vector4) -> float:
	var v_scroll_bar: ScrollBar = container.get_v_scroll_bar()
	var size_x: float = container.size.x
	
	if v_scroll_bar.visible:
		size_x -= v_scroll_bar.size.x
	
	size_x -= content_margins.x + content_margins.z
	return max(size_x, 0.0)


## 计算 Y 轴上不含水平滚动条高度的容器可用尺寸。[br]
## 使用 [param container] 的尺寸与 [param content_margins] 进行计算。
static func get_spare_size_y(container: Control, content_margins: Vector4) -> float:
	var h_scroll_bar: ScrollBar = container.get_h_scroll_bar()
	var size_y: float = container.size.y
	
	if h_scroll_bar.visible:
		size_y -= h_scroll_bar.size.y

	size_y -= content_margins.y + content_margins.w
	return max(size_y, 0.0)


## 计算两个轴上不含滚动条尺寸的容器可用尺寸。[br]
## 使用 [param container] 的尺寸与 [param content_margins] 进行计算。
static func get_spare_size(container: Control, content_margins: Vector4) -> Vector2:
	return Vector2(
		get_spare_size_x(container, content_margins),
		get_spare_size_y(container, content_margins)
	)


## 计算容器与 [param child] 节点在 X 轴上的尺寸差。[br]
## 当 [param clamp] 为 [code]true[/code] 时，将子节点尺寸钳制到 [param spare_size_x] 的最小值。
static func get_child_size_x_diff(child: Control, spare_size_x: float, clamp: bool) -> float:
	var child_size_x: float = max(child.size.x, child.get_combined_minimum_size().x) * child.scale.x
	
	if clamp:
		child_size_x = max(child_size_x, spare_size_x)
	
	return child_size_x - spare_size_x


## 计算容器与 [param child] 节点在 Y 轴上的尺寸差。[br]
## 当 [param clamp] 为 [code]true[/code] 时，将子节点尺寸钳制到 [param spare_size_y] 的最小值。
static func get_child_size_y_diff(child: Control, spare_size_y: float, clamp: bool) -> float:
	var child_size_y: float = max(child.size.y, child.get_combined_minimum_size().y) * child.scale.y
	
	if clamp:
		child_size_y = max(child_size_y, spare_size_y)
	
	return child_size_y - spare_size_y


## 计算容器与 [param child] 节点在两个轴上的尺寸差。[br]
## 当 [param clamp_x] 或 [param clamp_y] 为 [code]true[/code] 时，将对应轴的子节点尺寸钳制到 [param spare_size] 的最小值。
static func get_child_size_diff(
	child: Control,
	spare_size: Vector2,
	clamp_x: bool,
	clamp_y: bool
) -> Vector2:
	return Vector2(
		get_child_size_x_diff(child, spare_size.x, clamp_x),
		get_child_size_y_diff(child, spare_size.y, clamp_y)
	)


## 计算从当前 [param child_pos_x] 到左边界的距离。
static func get_left_dist(child_pos_x: float) -> float:
	return child_pos_x


## 计算从当前 [param child_pos_x] 到右边界的距离。[br]
## 使用 [param child_size_diff_x] 进行尺寸差计算。
static func get_right_dist(child_pos_x: float, child_size_diff_x: float) -> float:
	return child_pos_x + child_size_diff_x


## 计算从当前 [param child_pos_y] 到上边界的距离。
static func get_top_dist(child_pos_y: float) -> float:
	return child_pos_y


## 计算从当前 [param child_pos_y] 到下边界的距离。[br]
## 使用 [param child_size_diff_y] 进行尺寸差计算。
static func get_bottom_dist(child_pos_y: float, child_size_diff_y: float) -> float:
	return child_pos_y + child_size_diff_y


## 计算从当前 [param child_pos] 到四个边界的距离。[br]
## 返回包含各距离的 [Vector4]：[code](left, right, top, bottom)[/code]。使用 [param child_size_diff] 进行计算。
static func get_boundary_dist(child_pos: Vector2, child_size_diff: Vector2) -> Vector4:
	return Vector4(
		get_left_dist(child_pos.x),
		get_right_dist(child_pos.x, child_size_diff.x),
		get_top_dist(child_pos.y),
		get_bottom_dist(child_pos.y, child_size_diff.y)
	)


## 检查位于 [param pos_y] 的内容当前是否超出上边界。
static func is_outside_top_boundary(pos_y: float) -> bool:
	return pos_y > 0.0


## 检查位于 [param pos_y] 的内容当前是否超出下边界。[br]
## 使用 [param size_diff_y] 确定边界位置。
static func is_outside_bottom_boundary(pos_y: float, size_diff_y: float) -> bool:
	return pos_y < -size_diff_y


## 检查位于 [param pos_x] 的内容当前是否超出左边界。
static func is_outside_left_boundary(pos_x: float) -> bool:
	return pos_x > 0.0


## 检查位于 [param pos_x] 的内容当前是否超出右边界。[br]
## 使用 [param size_diff_x] 确定边界位置。
static func is_outside_right_boundary(pos_x: float, size_diff_x: float) -> bool:
	return pos_x < -size_diff_x


## 从 [param container] 的 StyleBox 中获取内容外边距。[br]
## 返回按顺序排列外边距的 [Vector4]：[code](left, top, right, bottom)[/code]。
static func get_content_margins(container: Control) -> Vector4:
	var style_box: StyleBox = container.get_theme_stylebox("panel")
	if style_box:
		return Vector4(
			max(style_box.content_margin_left, 0),
			max(style_box.content_margin_top, 0),
			max(style_box.content_margin_right, 0),
			max(style_box.content_margin_bottom, 0)
		)
	else:
		return Vector4.ZERO


## 根据 [param content_margins] 计算基础偏移。[br]
## 以 [Vector2] 返回基准偏移。
static func calculate_base_offset(content_margins: Vector4) -> Vector2:
	return Vector2(content_margins.x, content_margins.y)
