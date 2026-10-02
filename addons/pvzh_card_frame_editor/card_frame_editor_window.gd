@tool
extends Window

signal icon_changed

const CORNER_SIZE := 16.0
const DEFAULT_ICON_SIZE := Vector2(128, 128)
const GRID_SIZE := 32.0
const AXIS_COLOR_X := Color(0.8, 0.25, 0.25, 0.7)
const AXIS_COLOR_Y := Color(0.25, 0.8, 0.25, 0.7)
const GRID_COLOR := Color(0.25, 0.25, 0.25, 0.6)

var canvas: Control
var canvas_bg: ColorRect
var root_node: Control
var frame: TextureRect
var icon: TextureRect
var icon_placeholder: ColorRect

var spin_pos_x: SpinBox
var spin_pos_y: SpinBox
var spin_size_w: SpinBox
var spin_size_h: SpinBox
var spin_scale_x: SpinBox
var spin_scale_y: SpinBox

var frame_texture: Texture2D

var _dragging := false
var _drag_start_mouse := Vector2.ZERO
var _drag_start_pos := Vector2.ZERO
var _resizing := false
var _resize_start_mouse := Vector2.ZERO
var _resize_start_scale := Vector2.ONE
var _updating_spins := false

func _ready() -> void:
	title = "卡面场景编辑"
	size = Vector2i(1100, 720)
	transient = true
	exclusive = false
	unresizable = false

	var ui_root := Control.new()
	ui_root.set_anchors_preset(Control.PRESET_FULL_RECT)
	ui_root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(ui_root)

	var vbox := VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.add_theme_constant_override("separation", 6)
	ui_root.add_child(vbox)

	var tip := Label.new()
	tip.text = "左键拖动卡面 / 右下角控制点缩放 / 右侧可直接输入数值"
	vbox.add_child(tip)

	var hbox := HBoxContainer.new()
	hbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.size_flags_vertical = Control.SIZE_EXPAND_FILL
	hbox.add_theme_constant_override("separation", 6)
	vbox.add_child(hbox)

	canvas = Control.new()
	canvas.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	canvas.size_flags_vertical = Control.SIZE_EXPAND_FILL
	canvas.clip_contents = true
	canvas.mouse_filter = Control.MOUSE_FILTER_STOP
	canvas.gui_input.connect(_on_canvas_gui_input)
	canvas.draw.connect(_on_canvas_draw)
	canvas.resized.connect(_on_canvas_resized)
	hbox.add_child(canvas)

	canvas_bg = ColorRect.new()
	canvas_bg.color = Color(0.13, 0.13, 0.13)
	canvas_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	canvas_bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	canvas.add_child(canvas_bg)

	root_node = Control.new()
	root_node.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root_node.position = Vector2.ZERO
	root_node.size = Vector2.ZERO
	canvas.add_child(root_node)

	frame = TextureRect.new()
	frame.name = "卡框"
	frame.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	frame.stretch_mode = TextureRect.STRETCH_KEEP_CENTERED
	frame.set_anchors_preset(Control.PRESET_TOP_LEFT, true)
	frame.mouse_filter = Control.MOUSE_FILTER_IGNORE
	frame.position = Vector2.ZERO
	root_node.add_child(frame)

	icon = TextureRect.new()
	icon.name = "卡牌图标"
	icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	icon.pivot_offset = Vector2.ZERO
	icon.size = DEFAULT_ICON_SIZE
	icon.position = Vector2.ZERO
	icon.scale = Vector2.ONE
	root_node.add_child(icon)

	icon_placeholder = ColorRect.new()
	icon_placeholder.name = "占位"
	icon_placeholder.color = Color(0.3, 0.7, 1.0, 0.25)
	icon_placeholder.mouse_filter = Control.MOUSE_FILTER_IGNORE
	icon_placeholder.set_anchors_preset(Control.PRESET_FULL_RECT)
	icon.add_child(icon_placeholder)

	var panel := VBoxContainer.new()
	panel.custom_minimum_size = Vector2(220, 0)
	panel.add_theme_constant_override("separation", 4)
	hbox.add_child(panel)

	var panel_title := Label.new()
	panel_title.text = "卡面属性"
	panel_title.add_theme_font_size_override("font_size", 14)
	panel.add_child(panel_title)

	spin_pos_x = _make_spin(-4000, 4000, 1)
	spin_pos_y = _make_spin(-4000, 4000, 1)
	spin_size_w = _make_spin(1, 4000, 1)
	spin_size_h = _make_spin(1, 4000, 1)
	spin_scale_x = _make_spin(0.05, 20, 0.01)
	spin_scale_y = _make_spin(0.05, 20, 0.01)

	_add_spin_row(panel, "X", spin_pos_x)
	_add_spin_row(panel, "Y", spin_pos_y)
	_add_spin_row(panel, "W", spin_size_w)
	_add_spin_row(panel, "H", spin_size_h)
	_add_spin_row(panel, "Scale X", spin_scale_x)
	_add_spin_row(panel, "Scale Y", spin_scale_y)

	spin_pos_x.value_changed.connect(_on_spin_changed)
	spin_pos_y.value_changed.connect(_on_spin_changed)
	spin_size_w.value_changed.connect(_on_spin_changed)
	spin_size_h.value_changed.connect(_on_spin_changed)
	spin_scale_x.value_changed.connect(_on_spin_changed)
	spin_scale_y.value_changed.connect(_on_spin_changed)

	close_requested.connect(func(): hide())

	_sync_spins_from_icon()
	await get_tree().process_frame
	_on_canvas_resized()

func _on_canvas_resized() -> void:
	if root_node:
		root_node.position = Vector2.ZERO
		root_node.size = canvas.size
	canvas.queue_redraw()

func _add_spin_row(parent: Node, label_text: String, spin: SpinBox) -> void:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 4)
	parent.add_child(row)

	var l := Label.new()
	l.text = label_text
	l.custom_minimum_size = Vector2(60, 0)
	row.add_child(l)

	spin.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(spin)

func _make_spin(min_v: float, max_v: float, step: float) -> SpinBox:
	var s := SpinBox.new()
	s.min_value = min_v
	s.max_value = max_v
	s.step = step
	s.allow_greater = true
	s.allow_lesser = true
	return s

func _sync_spins_from_icon() -> void:
	if icon == null:
		return
	_updating_spins = true
	spin_pos_x.set_value_no_signal(icon.position.x)
	spin_pos_y.set_value_no_signal(icon.position.y)
	spin_size_w.set_value_no_signal(icon.size.x)
	spin_size_h.set_value_no_signal(icon.size.y)
	spin_scale_x.set_value_no_signal(icon.scale.x)
	spin_scale_y.set_value_no_signal(icon.scale.y)
	_updating_spins = false

func _on_spin_changed(_v: float) -> void:
	if _updating_spins:
		return
	if icon == null:
		return
	icon.position = Vector2(spin_pos_x.value, spin_pos_y.value)
	icon.size = Vector2(spin_size_w.value, spin_size_h.value)
	icon.scale = Vector2(spin_scale_x.value, spin_scale_y.value)
	icon_changed.emit()

func set_frame_textures(bg_tex: Texture2D, frame_tex: Texture2D, frame_pos: Vector2, frame_scale: Vector2) -> void:
	frame.texture = frame_tex
	frame.size = Vector2(0,0)
	frame.position = Vector2(128,96.5)
	frame.scale = Vector2.ONE

func set_icon_texture(tex: Texture2D) -> void:
	icon.texture = tex
	if tex:
		icon.size = tex.get_size()
	else:
		icon.size = DEFAULT_ICON_SIZE

	if icon.scale == Vector2.ZERO:
		icon.scale = Vector2.ONE

	if icon_placeholder:
		icon_placeholder.visible = (tex == null)

	_sync_spins_from_icon()

func set_frame_on_top(on_top: bool) -> void:
	if frame == null or icon == null:
		return
	if on_top:
		frame.z_index = 1
		icon.z_index = 0
	else:
		frame.z_index = 0
		icon.z_index = 1

func get_icon_position() -> Vector2:
	return icon.position

func get_icon_scale() -> Vector2:
	return icon.scale

func set_icon_position_scale(pos: Vector2, scl: Vector2) -> void:
	icon.position = pos
	icon.scale = scl
	_sync_spins_from_icon()

func _get_icon_rect() -> Rect2:
	if icon == null:
		return Rect2()
	var s := icon.size * icon.scale
	return Rect2(icon.position, s)

func _on_canvas_draw() -> void:
	if canvas == null:
		return

	var size := canvas.size

	var x := 0.0
	while x < size.x:
		canvas.draw_line(Vector2(x, 0), Vector2(x, size.y), GRID_COLOR, 1.0)
		x += GRID_SIZE

	var y := 0.0
	while y < size.y:
		canvas.draw_line(Vector2(0, y), Vector2(size.x, y), GRID_COLOR, 1.0)
		y += GRID_SIZE

	canvas.draw_line(Vector2(0, 0), Vector2(size.x, 0), AXIS_COLOR_X, 2.0)
	canvas.draw_line(Vector2(0, 0), Vector2(0, size.y), AXIS_COLOR_Y, 2.0)

	if icon and icon.visible:
		var r := _get_icon_rect()
		canvas.draw_rect(r, Color(0.3, 0.7, 1.0), false, 1.0)
		var corner := r.position + r.size
		canvas.draw_rect(Rect2(corner - Vector2(CORNER_SIZE, CORNER_SIZE), Vector2(CORNER_SIZE * 2, CORNER_SIZE * 2)), Color(0.3, 0.7, 1.0), false, 1.0)

func _on_canvas_gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		var mouse := mb.position

		if mb.button_index == MOUSE_BUTTON_LEFT:
			if mb.pressed:
				var r := _get_icon_rect()
				var corner := r.position + r.size
				var corner_rect := Rect2(corner - Vector2(CORNER_SIZE, CORNER_SIZE), Vector2(CORNER_SIZE * 2, CORNER_SIZE * 2))

				if corner_rect.has_point(mouse):
					_resizing = true
					_resize_start_mouse = mouse
					_resize_start_scale = icon.scale
				elif r.has_point(mouse):
					_dragging = true
					_drag_start_mouse = mouse
					_drag_start_pos = icon.position
				else:
					_dragging = false
					_resizing = false
			else:
				_dragging = false
				_resizing = false

	elif event is InputEventMouseMotion:
		var mm := event as InputEventMouseMotion
		var mouse := mm.position

		if _dragging:
			var delta := mouse - _drag_start_mouse
			icon.position = _drag_start_pos + delta
			_sync_spins_from_icon()
			canvas.queue_redraw()
			icon_changed.emit()
		elif _resizing:
			var delta := mouse - _resize_start_mouse
			var base_size := icon.size
			if base_size.x <= 0 or base_size.y <= 0:
				return
			var new_scale := _resize_start_scale + delta / base_size
			new_scale.x = max(new_scale.x, 0.05)
			new_scale.y = max(new_scale.y, 0.05)
			icon.scale = new_scale
			_sync_spins_from_icon()
			canvas.queue_redraw()
			icon_changed.emit()

func get_icon_size() -> Vector2:
	return icon.size
