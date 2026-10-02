@tool
extends Control

var editor_plugin: EditorPlugin

var card_type_option: OptionButton
var rarity_option: OptionButton

var status_label: Label
var tscn_name: LineEdit

var scene_editor_window: Window
var current_icon_texture: Texture2D = null
var current_icon_path: String = ""
var _user_imported_icon := false
var icon_file_dialog: FileDialog

const WindowScript := preload("res://addons/pvzh_card_frame_editor/card_frame_editor_window.gd")

const CARD_TYPES := ["单位", "锦囊", "环境"]
const RARITIES := ["基础", "普通", "罕见", "稀有", "超稀有", "传说", "活动"]

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

	_add_label(root, "场景导出名称")
	tscn_name = LineEdit.new()
	tscn_name.placeholder_text = "xxx"
	root.add_child(tscn_name)

	var import_btn := Button.new()
	import_btn.text = "导入卡面 PNG"
	import_btn.pressed.connect(_on_import_icon_pressed)
	root.add_child(import_btn)

	icon_file_dialog = FileDialog.new()
	icon_file_dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	icon_file_dialog.access = FileDialog.ACCESS_RESOURCES
	icon_file_dialog.add_filter("*.png", "PNG 图片")
	icon_file_dialog.title = "选择卡面 PNG"
	icon_file_dialog.confirmed.connect(_on_icon_file_confirmed)
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
	if scene_editor_window and is_instance_valid(scene_editor_window):
		_refresh_scene_editor()

func _on_import_icon_pressed() -> void:
	icon_file_dialog.popup_centered_ratio(0.6)

func _on_icon_file_confirmed() -> void:
	_on_icon_file_selected(icon_file_dialog.current_path)

func _on_icon_file_selected(path: String) -> void:
	if path.is_empty():
		return
	var tex := load(path) as Texture2D
	if tex == null:
		_show_status("无法加载该 PNG：%s" % path, true)
		return

	current_icon_path = path
	current_icon_texture = tex
	_user_imported_icon = true
	if scene_editor_window and is_instance_valid(scene_editor_window):
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
		null,
		_get_frame_texture(),
		_get_frame_position(),
		_get_frame_scale()
	)
	scene_editor_window.set_icon_texture(current_icon_texture)
	scene_editor_window.set_frame_on_top(not _is_fighter())

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

	var icon := TextureRect.new()
	icon.name = "卡牌图标"
	icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	icon.stretch_mode = TextureRect.STRETCH_KEEP_CENTERED
	icon.position = scene_editor_window.get_icon_position()
	icon.size = scene_editor_window.get_icon_size()
	icon.scale = scene_editor_window.get_icon_scale()
	icon.texture = current_icon_texture
	root.add_child(icon)
	icon.owner = root

	var packed := PackedScene.new()
	packed.pack(root)

	var file_base := tscn_name.text.strip_edges()
	if file_base.is_empty():
		file_base = "%s_%s" % [t, r]

	var save_path := "res://场景(C#)/卡面/%s.tscn" % file_base
	DirAccess.make_dir_recursive_absolute(save_path.get_base_dir())
	var err := ResourceSaver.save(packed, save_path)
	if err != OK:
		_show_status("保存失败：%s" % err, true)
		return

	tscn_name.text = file_base
	_show_status("已导出：%s" % save_path, false)
	editor_plugin.get_editor_interface().get_resource_filesystem().scan()

func _show_status(msg: String, is_error: bool) -> void:
	status_label.text = msg
	status_label.modulate = Color(1, 0.45, 0.35) if is_error else Color(0.45, 1, 0.55)
