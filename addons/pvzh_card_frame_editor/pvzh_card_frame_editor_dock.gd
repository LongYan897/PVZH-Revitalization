@tool
extends Control

var editor_plugin: EditorPlugin

var card_type_option: OptionButton
var rarity_option: OptionButton
var camp_option: OptionButton

var status_label: Label

var scene_editor_window: Window
var current_icon_texture: Texture2D = null
var current_icon_path: String = ""
var icon_file_dialog: FileDialog

const WindowScript := preload("res://addons/pvzh_card_frame_editor/card_frame_editor_window.gd")

const CARD_TYPES := ["单位", "锦囊", "环境"]
const RARITIES := ["基础", "普通", "罕见", "稀有", "超稀有", "传说", "活动"]
const CAMPS := ["植物", "僵尸"]

func _ready() -> void:
	var root := VBoxContainer.new()
	root.add_theme_constant_override("separation", 6)
	add_child(root)

	var title := Label.new()
	title.text = "PVZH 卡面编辑器"
	title.add_theme_font_size_override("font_size", 16)
	root.add_child(title)

	_add_label(root, "卡牌类型")
	card_type_option = OptionButton.new()
	for t in CARD_TYPES:
		card_type_option.add_item(t)
	card_type_option.item_selected.connect(_on_any_changed)
	root.add_child(card_type_option)

	_add_label(root, "稀有度")
	rarity_option = OptionButton.new()
	for r in RARITIES:
		rarity_option.add_item(r)
	rarity_option.item_selected.connect(_on_any_changed)
	root.add_child(rarity_option)

	_add_label(root, "阵营")
	camp_option = OptionButton.new()
	for c in CAMPS:
		camp_option.add_item(c)
	camp_option.item_selected.connect(_on_any_changed)
	root.add_child(camp_option)

	var import_btn := Button.new()
	import_btn.text = "导入卡面 PNG"
	import_btn.pressed.connect(_on_import_icon_pressed)
	root.add_child(import_btn)

	icon_file_dialog = FileDialog.new()
	icon_file_dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	icon_file_dialog.access = FileDialog.ACCESS_RESOURCES
	icon_file_dialog.add_filter("*.png", "PNG 图片")
	icon_file_dialog.title = "选择卡面 PNG"
	icon_file_dialog.confirmed.connect(_on_icon_file_selected)
	add_child(icon_file_dialog)

	var open_scene_btn := Button.new()
	open_scene_btn.text = "打开场景"
	open_scene_btn.pressed.connect(_on_open_scene_editor)
	root.add_child(open_scene_btn)

	var export_btn := Button.new()
	export_btn.text = "导出卡面场景"
	export_btn.pressed.connect(_on_export)
	root.add_child(export_btn)

	status_label = Label.new()
	status_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(status_label)

func _add_label(parent: Node, text: String) -> void:
	var l := Label.new()
	l.text = text
	parent.add_child(l)

func _on_any_changed(_idx: int) -> void:
	_load_from_existing()
	_refresh_scene_editor()

func _on_import_icon_pressed() -> void:
	icon_file_dialog.popup_centered_ratio(0.6)

func _on_icon_file_selected(path: String) -> void:
	var tex := load(path) as Texture2D
	if tex == null:
		_show_status("无法加载该 PNG：%s" % path, true)
		return

	current_icon_path = path
	current_icon_texture = tex
	_refresh_scene_editor()
	_show_status("已导入卡面：%s" % path, false)

func _on_open_scene_editor() -> void:
	if scene_editor_window == null:
		scene_editor_window = WindowScript.new()
		scene_editor_window.icon_changed.connect(_on_icon_changed)
		add_child(scene_editor_window)
	_refresh_scene_editor()
	scene_editor_window.popup_centered()

func _on_icon_changed() -> void:
	pass

func _refresh_scene_editor() -> void:
	if scene_editor_window == null or not is_instance_valid(scene_editor_window):
		return

	scene_editor_window.set_frame_textures(
		_get_frame_bg_texture(),
		_get_frame_texture(),
		_get_frame_position(),
		_get_frame_scale()
	)
	scene_editor_window.set_icon_texture(current_icon_texture)

func _is_fighter() -> bool:
	return _card_type() == "单位"

func _is_trick() -> bool:
	return _card_type() == "锦囊"

func _is_environment() -> bool:
	return _card_type() == "环境"

func _rarity_key() -> String:
	match _rarity():
		"基础", "普通":
			return "Basic"
		"罕见":
			return "Uncommon"
		"稀有":
			return "Rare"
		"超稀有":
			return "SuperRare"
		"传说":
			return "Legend"
		"活动":
			return "Activity"
	return "Basic"

func _get_frame_texture() -> Texture2D:
	var base := "res://素材(C#)/卡牌属性/"
	var r := _rarity_key()

	if _is_fighter():
		match r:
			"Basic", "Common":
				return load(base + "SEEDPACKET_FRONT.png")
			"Uncommon":
				return load(base + "SEEDPACKET_R2.png")
			"Rare":
				return load(base + "SEEDPACKET_R3.png")
			"SuperRare":
				return load(base + "SEEDPACKET_R4.png")
			"Legend":
				return load(base + "SEEDPACKET_R5.png")
			"Activity":
				return load(base + "SEEDPACKET_Event.png")

	if _is_environment():
		match r:
			"Basic", "Common":
				return load(base + "Environment_R1.png")
			"Uncommon":
				return load(base + "Environment_R2.png")
			"Rare":
				return load(base + "Environment_R3.png")
			"SuperRare":
				return load(base + "Environment_R4.png")
			"Legend":
				return load(base + "Environment_R5.png")
			"Activity":
				return load(base + "Environment_Event.png")

	if _is_trick():
		match r:
			"Basic", "Common":
				return load(base + "SEEDPACKET_onetime_R1.png")
			"Uncommon":
				return load(base + "SEEDPACKET_onetime_R2.png")
			"Rare":
				return load(base + "SEEDPACKET_onetime_R3.png")
			"SuperRare":
				return load(base + "SEEDPACKET_onetime_R4.png")
			"Legend":
				return load(base + "SEEDPACKET_onetime_R5.png")
			"Activity":
				return load(base + "SEEDPACKET_onetime_Event.png")

	return null

func _get_frame_bg_texture() -> Texture2D:
	var base := "res://素材(C#)/卡牌属性/"
	if _is_fighter():
		return load(base + "SEEDPACKET_BACK.png")
	if _is_environment():
		return load(base + "Environment_Back.png")
	if _is_trick():
		return load(base + "SEEDPACKET_Back_onetime.png")
	return null

func _get_frame_position() -> Vector2:
	if _is_environment():
		return Vector2(128.0, 92.0)
	return Vector2(128.0, 96.0)

func _get_frame_scale() -> Vector2:
	if _is_environment():
		return Vector2(0.55, 0.55)
	return Vector2(1.0, 1.0)

func _card_type() -> String:
	return CARD_TYPES[card_type_option.selected]

func _rarity() -> String:
	return RARITIES[rarity_option.selected]

func _camp() -> String:
	return CAMPS[camp_option.selected]

func _load_from_existing() -> void:
	var path := "res://场景(C#)/卡面/%s_%s.tscn" % [_card_type(), _rarity()]
	if not ResourceLoader.exists(path):
		return
	var packed := load(path) as PackedScene
	var inst := packed.instantiate()
	var icon := inst.get_node_or_null("卡牌图标") as TextureRect
	if icon != null and scene_editor_window and is_instance_valid(scene_editor_window):
		scene_editor_window.set_icon_position_scale(icon.position, icon.scale)
		if icon.texture:
			current_icon_texture = icon.texture
	inst.queue_free()
	_refresh_scene_editor()

func _on_export() -> void:
	if current_icon_texture == null:
		_show_status("请先导入卡面 PNG", true)
		return

	if scene_editor_window == null or not is_instance_valid(scene_editor_window):
		_show_status("请先点击“打开场景”调整卡面位置", true)
		return

	var t := _card_type()
	var r := _rarity()

	var root := Control.new()
	root.name = "卡面"
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE

	var bg := TextureRect.new()
	bg.name = "卡牌底板"
	bg.texture = _get_frame_bg_texture()
	if bg.texture:
		bg.size = bg.texture.get_size()
		bg.position = _get_frame_position()
		bg.scale = _get_frame_scale()
	bg.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	bg.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	root.add_child(bg)
	bg.owner = root

	var frame := TextureRect.new()
	frame.name = "卡框"
	frame.texture = _get_frame_texture()
	if frame.texture:
		frame.size = frame.texture.get_size()
		frame.position = _get_frame_position()
		frame.scale = _get_frame_scale()
	frame.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	frame.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	root.add_child(frame)
	frame.owner = root

	var icon := TextureRect.new()
	icon.name = "卡牌图标"
	icon.position = scene_editor_window.get_icon_position()
	icon.scale = scene_editor_window.get_icon_scale()
	icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon.texture = current_icon_texture
	root.add_child(icon)
	icon.owner = root

	var packed := PackedScene.new()
	packed.pack(root)

	var save_path := "res://场景(C#)/卡面/%s_%s.tscn" % [t, r]
	DirAccess.make_dir_recursive_absolute(save_path.get_base_dir())
	var err := ResourceSaver.save(packed, save_path)
	if err != OK:
		_show_status("保存失败：%s" % err, true)
		return

	_show_status("已导出：%s" % save_path, false)
	editor_plugin.get_editor_interface().get_resource_filesystem().scan()

func _show_status(msg: String, is_error: bool) -> void:
	status_label.text = msg
	status_label.modulate = Color(1, 0.45, 0.35) if is_error else Color(0.45, 1, 0.55)
