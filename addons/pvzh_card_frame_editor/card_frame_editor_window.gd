@tool
extends Window

signal icon_changed

var canvas: Control
var root_node: Control
var frame_bg: TextureRect
var frame: TextureRect
var icon: TextureRect

var frame_texture: Texture2D
var frame_bg_texture: Texture2D

var _dragging := false
var _drag_start_mouse := Vector2.ZERO
var _drag_start_pos := Vector2.ZERO
var _resizing := false
var _resize_start_mouse := Vector2.ZERO
var _resize_start_scale := Vector2.ONE

const CORNER_SIZE := 16.0
func _ready() -> void:
	title = "卡面场景编辑"
	size = Vector2i(900, 700)
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
	tip.text = "左键拖动卡面 / 右下角控制点缩放"
	vbox.add_child(tip)

	canvas = Control.new()
	canvas.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	canvas.size_flags_vertical = Control.SIZE_EXPAND_FILL
	canvas.clip_contents = true
	canvas.mouse_filter = Control.MOUSE_FILTER_STOP
	canvas.gui_input.connect(_on_canvas_gui_input)
	vbox.add_child(canvas)

	var bg := ColorRect.new()
	bg.color = Color(0.13, 0.13, 0.13)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	canvas.add_child(bg)

	root_node = Control.new()
	root_node.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root_node.set_anchors_preset(Control.PRESET_FULL_RECT)
	canvas.add_child(root_node)

	frame_bg = TextureRect.new()
	frame_bg.name = "卡牌底板"
	frame_bg.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	frame_bg.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	frame_bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root_node.add_child(frame_bg)

	frame = TextureRect.new()
	frame.name = "卡框"
	frame.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	frame.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	frame.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root_node.add_child(frame)

	icon = TextureRect.new()
	icon.name = "卡牌图标"
	icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	icon.pivot_offset = Vector2.ZERO
	root_node.add_child(icon)

	close_requested.connect(func(): hide())

func set_frame_textures(bg_tex: Texture2D, frame_tex: Texture2D, frame_pos: Vector2, frame_scale: Vector2) -> void:
	frame_bg_texture = bg_tex
	frame_texture = frame_tex

	frame_bg.texture = bg_tex
	if bg_tex:
		frame_bg.size = bg_tex.get_size()
		frame_bg.position = frame_pos
		frame_bg.scale = frame_scale

	frame.texture = frame_tex
	if frame_tex:
		frame.size = frame_tex.get_size()
		frame.position = frame_pos
		frame.scale = frame_scale

func set_icon_texture(tex: Texture2D) -> void:
	icon.texture = tex
	if tex:
		icon.size = tex.get_size()
		if icon.position == Vector2.ZERO:
			icon.position = Vector2(128, 96)
		if icon.scale == Vector2.ZERO:
			icon.scale = Vector2.ONE
	else:
		icon.size = Vector2.ZERO
	
	if icon.position == Vector2.ZERO:
		icon.position = Vector2(128, 96)
	if icon.scale == Vector2.ZERO:
		icon.scale = Vector2.ONE * 36;

func get_icon_position() -> Vector2:
	return icon.position

func get_icon_scale() -> Vector2:
	return icon.scale

func set_icon_position_scale(pos: Vector2, scl: Vector2) -> void:
	icon.position = pos
	icon.scale = scl

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
			icon_changed.emit()

func _get_icon_rect() -> Rect2:
	if icon == null or icon.texture == null:
		return Rect2()
	var s := icon.size * icon.scale
	return Rect2(icon.position, s)
