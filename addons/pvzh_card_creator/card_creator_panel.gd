@tool
extends ScrollContainer

const Config := preload("res://addons/pvzh_card_creator/creator_config.gd")
const WindowScript := preload("res://addons/pvzh_card_creator/card_frame_editor_window.gd")

const CARDS_JSON_PATH := "res://数据(C#)/cards.json"
const FRAME_EXPORT_DIR := "res://场景(C#)/卡面/"
const CS_SCRIPT_ROOT := "res://脚本(C#)/卡牌/实例卡牌/"

var editor_plugin: EditorPlugin
var root: VBoxContainer

var name_edit: LineEdit
var key_name_edit: LineEdit
var faction: OptionButton
var card_type: OptionButton
var hero_check: CheckBox

var des_edit: TextEdit
var animation_edit: LineEdit
var animation_extra_edit: LineEdit
var icon_edit: LineEdit
var label_edit: LineEdit
var cost_spin: SpinBox
var rarity_option: OptionButton
var category_option: OptionButton

var include_atk: CheckBox
var include_star_type: CheckBox
var include_pack: CheckBox

var hp_box: VBoxContainer
var atk_box: VBoxContainer
var star_type_box: VBoxContainer
var pack_box: VBoxContainer

var hp_spin: SpinBox
var hp_type_rows: VBoxContainer
var atk_spin: SpinBox
var atk_type_rows: VBoxContainer
var star_type_rows: VBoxContainer
var pack_edit: LineEdit
var flavor_edit: TextEdit
var id_edit: LineEdit

var tags_section: VBoxContainer
var tags_box: VBoxContainer
var tag_checks: Dictionary = {}

var bool_section: VBoxContainer
var bool_props_box: VBoxContainer
var bool_prop_checks: Dictionary = {}

var targets_section: VBoxContainer
var target_checks: Dictionary = {}

var auto_clear_check: CheckBox
var status_label: Label

var scene_editor_window: Window
var current_icon_texture: Texture2D = null
var current_icon_path: String = ""
var icon_file_dialog: FileDialog
var frame_status_label: Label

const RARITIES := ["基础", "普通", "罕见", "稀有", "超稀有", "传说", "活动", "令牌"]
const CATEGORIES_PLANT := ["猛长", "光能", "守卫", "爆花", "聪明"]
const CATEGORIES_ZOMBIE := ["有脑", "疯狂", "狡猾", "野兽", "健壮"]
const CARD_TYPE_TEXTS := ["单位", "锦囊", "环境"]
const TARGET_OPTIONS := [
	{"key": "Fighters", "label": "战斗单位"},
	{"key": "Lines", "label": "行"},
	{"key": "Grids", "label": "格子"},
	{"key": "CoopGrids", "label": "组队格子"},
	{"key": "Hero", "label": "英雄"},
	{"key": "GraveStone", "label": "墓碑"}
]

func _ready() -> void:
	Config.ensure_loaded()
	horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	size_flags_horizontal = Control.SIZE_EXPAND_FILL
	size_flags_vertical = Control.SIZE_EXPAND_FILL

	root = VBoxContainer.new()
	root.custom_minimum_size = Vector2(300, 0)
	root.add_theme_constant_override("separation", 6)
	root.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	add_child(root)

	var title := Label.new()
	title.text = "PVZH 快速创建"
	title.add_theme_font_size_override("font_size", 18)
	root.add_child(title)

	_add_section("基本信息", false)

	_add_label("类名")
	key_name_edit = LineEdit.new()
	key_name_edit.placeholder_text = "例如 SnowPea"
	root.add_child(key_name_edit)

	_add_label("名称")
	name_edit = LineEdit.new()
	name_edit.placeholder_text = "例如 寒冰射手"
	root.add_child(name_edit)

	var row_basic := HBoxContainer.new()
	row_basic.add_theme_constant_override("separation", 6)
	root.add_child(row_basic)

	var col_faction := VBoxContainer.new()
	col_faction.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row_basic.add_child(col_faction)
	_add_label_to(col_faction, "阵营")
	faction = OptionButton.new()
	faction.add_item("植物", 0)
	faction.add_item("僵尸", 1)
	col_faction.add_child(faction)

	var col_type := VBoxContainer.new()
	col_type.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row_basic.add_child(col_type)
	_add_label_to(col_type, "卡牌类型")
	card_type = OptionButton.new()
	card_type.add_item("单位卡", 0)
	card_type.add_item("锦囊卡", 1)
	card_type.add_item("环境卡", 2)
	card_type.item_selected.connect(_on_card_type_selected)
	col_type.add_child(card_type)

	hero_check = CheckBox.new()
	hero_check.text = "超能力"
	root.add_child(hero_check)

	_add_section("卡牌属性")

	_add_label("描述")
	des_edit = TextEdit.new()
	des_edit.custom_minimum_size = Vector2(0, 60)
	des_edit.placeholder_text = "支持 _下划线_ =图标= =图标={N}"
	root.add_child(des_edit)

	var row_fee := HBoxContainer.new()
	row_fee.add_theme_constant_override("separation", 6)
	root.add_child(row_fee)

	var col_cost := VBoxContainer.new()
	col_cost.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row_fee.add_child(col_cost)
	_add_label_to(col_cost, "费用")
	cost_spin = SpinBox.new()
	cost_spin.min_value = 0
	cost_spin.max_value = 99
	col_cost.add_child(cost_spin)

	var col_id := VBoxContainer.new()
	col_id.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row_fee.add_child(col_id)
	_add_label_to(col_id, "卡牌Id")
	id_edit = LineEdit.new()
	id_edit.placeholder_text = "重复会自动改用靠后的空闲 Id"
	col_id.add_child(id_edit)

	var row_rarity := HBoxContainer.new()
	row_rarity.add_theme_constant_override("separation", 6)
	root.add_child(row_rarity)

	var col_rarity := VBoxContainer.new()
	col_rarity.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row_rarity.add_child(col_rarity)
	_add_label_to(col_rarity, "稀有度")
	rarity_option = OptionButton.new()
	for r in RARITIES:
		rarity_option.add_item(r)
	rarity_option.item_selected.connect(func(_i): _refresh_scene_editor())
	col_rarity.add_child(rarity_option)

	var col_cat := VBoxContainer.new()
	col_cat.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row_rarity.add_child(col_cat)
	_add_label_to(col_cat, "派系")
	category_option = OptionButton.new()
	col_cat.add_child(category_option)

	hp_box = VBoxContainer.new()
	hp_box.visible = false
	hp_box.add_theme_constant_override("separation", 6)
	root.add_child(hp_box)

	_add_label_to(hp_box, "生命值")
	hp_spin = SpinBox.new()
	hp_spin.min_value = 0
	hp_spin.max_value = 999
	hp_box.add_child(hp_spin)

	_add_label_to(hp_box, "生命值类型")
	hp_type_rows = VBoxContainer.new()
	hp_type_rows.add_theme_constant_override("separation", 2)
	hp_box.add_child(hp_type_rows)

	var hp_add_btn := Button.new()
	hp_add_btn.text = "+ 添加类型"
	hp_add_btn.pressed.connect(func(): _add_type_row(hp_type_rows, Config.get_hp_types()))
	hp_box.add_child(hp_add_btn)

	include_atk = CheckBox.new()
	include_atk.text = "有伤害"
	include_atk.toggled.connect(_on_include_atk_toggled)
	hp_box.add_child(include_atk)

	atk_box = VBoxContainer.new()
	atk_box.visible = false
	atk_box.add_theme_constant_override("separation", 6)
	hp_box.add_child(atk_box)

	_add_label_to(atk_box, "攻击力")
	atk_spin = SpinBox.new()
	atk_spin.min_value = 0
	atk_spin.max_value = 999
	atk_box.add_child(atk_spin)

	_add_label_to(atk_box, "攻击力类型")
	atk_type_rows = VBoxContainer.new()
	atk_type_rows.add_theme_constant_override("separation", 2)
	atk_box.add_child(atk_type_rows)

	var atk_add_btn := Button.new()
	atk_add_btn.text = "+ 添加类型"
	atk_add_btn.pressed.connect(func(): _add_type_row(atk_type_rows, Config.get_atk_types()))
	atk_box.add_child(atk_add_btn)

	include_star_type = CheckBox.new()
	include_star_type.text = "有等级"
	include_star_type.toggled.connect(_on_include_star_type_toggled)
	hp_box.add_child(include_star_type)

	star_type_box = VBoxContainer.new()
	star_type_box.visible = false
	star_type_box.add_theme_constant_override("separation", 6)
	hp_box.add_child(star_type_box)

	_add_label_to(star_type_box, "等级类型")
	star_type_rows = VBoxContainer.new()
	star_type_rows.add_theme_constant_override("separation", 2)
	star_type_box.add_child(star_type_rows)

	var star_add_btn := Button.new()
	star_add_btn.text = "+ 添加类型"
	star_add_btn.pressed.connect(func(): _add_type_row(star_type_rows, Config.get_star_types()))
	star_type_box.add_child(star_add_btn)

	include_pack = CheckBox.new()
	include_pack.text = "有卡包"
	include_pack.toggled.connect(_on_include_pack_toggled)
	root.add_child(include_pack)

	pack_box = VBoxContainer.new()
	pack_box.visible = false
	pack_box.add_theme_constant_override("separation", 6)
	root.add_child(pack_box)

	_add_label_to(pack_box, "卡包")
	pack_edit = LineEdit.new()
	pack_edit.placeholder_text = "高级"
	pack_box.add_child(pack_edit)

	_add_label("标签")
	label_edit = LineEdit.new()
	label_edit.placeholder_text = "逗号分隔 如 豌豆,水果"
	root.add_child(label_edit)

	tags_section = VBoxContainer.new()
	tags_section.add_theme_constant_override("separation", 6)
	root.add_child(tags_section)

	var tags_sep := HSeparator.new()
	tags_section.add_child(tags_sep)

	var tags_title := Label.new()
	tags_title.text = "关键词"
	tags_title.add_theme_font_size_override("font_size", 15)
	tags_title.add_theme_color_override("font_color", Color(0.75, 0.85, 1.0))
	tags_section.add_child(tags_title)

	var tags_hint := Label.new()
	tags_hint.text = "仅单位卡显示"
	tags_hint.add_theme_font_size_override("font_size", 11)
	tags_hint.add_theme_color_override("font_color", Color(1, 1, 1, 0.5))
	tags_section.add_child(tags_hint)

	tags_box = VBoxContainer.new()
	tags_box.add_theme_constant_override("separation", 2)
	tags_section.add_child(tags_box)

	bool_section = VBoxContainer.new()
	bool_section.add_theme_constant_override("separation", 6)
	root.add_child(bool_section)

	var bool_sep := HSeparator.new()
	bool_section.add_child(bool_sep)

	var bool_title := Label.new()
	bool_title.text = "扩展属性"
	bool_title.add_theme_font_size_override("font_size", 15)
	bool_title.add_theme_color_override("font_color", Color(0.75, 0.85, 1.0))
	bool_section.add_child(bool_title)

	bool_props_box = VBoxContainer.new()
	bool_props_box.add_theme_constant_override("separation", 2)
	bool_section.add_child(bool_props_box)

	_build_targets_section()
	_build_frame_section()

	_add_section("其它")

	_add_label("特别描述")
	flavor_edit = TextEdit.new()
	flavor_edit.custom_minimum_size = Vector2(0, 40)
	root.add_child(flavor_edit)

	_add_label("动画路径")
	animation_edit = LineEdit.new()
	animation_edit.placeholder_text = "res://..."
	root.add_child(animation_edit)

	_add_label("额外动画场景")
	animation_extra_edit = LineEdit.new()
	animation_extra_edit.placeholder_text = "res://..."
	root.add_child(animation_extra_edit)

	auto_clear_check = CheckBox.new()
	auto_clear_check.text = "创建后自动清空"
	root.add_child(auto_clear_check)

	var button_row := HBoxContainer.new()
	button_row.add_theme_constant_override("separation", 8)
	root.add_child(button_row)

	var button := Button.new()
	button.text = "创建并写入 cards.json"
	button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	button.pressed.connect(_create)
	button_row.add_child(button)

	var clear_button := Button.new()
	clear_button.text = "清空"
	clear_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	clear_button.pressed.connect(_clear_all)
	button_row.add_child(clear_button)

	status_label = Label.new()
	status_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(status_label)

	_add_default_type_rows()
	_on_faction_selected(faction.selected)
	_on_card_type_selected(card_type.selected)

func _add_section(text: String, with_sep: bool = true) -> void:
	if with_sep:
		var sep := HSeparator.new()
		root.add_child(sep)
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", 15)
	l.add_theme_color_override("font_color", Color(0.75, 0.85, 1.0))
	root.add_child(l)

func _build_targets_section() -> void:
	targets_section = VBoxContainer.new()
	targets_section.add_theme_constant_override("separation", 6)
	root.add_child(targets_section)

	var sep := HSeparator.new()
	targets_section.add_child(sep)

	var title := Label.new()
	title.text = "目标"
	title.add_theme_font_size_override("font_size", 15)
	title.add_theme_color_override("font_color", Color(0.75, 0.85, 1.0))
	targets_section.add_child(title)

	var hint := Label.new()
	hint.text = "仅非单位卡生效 单位卡目标由融合与进化规则决定"
	hint.add_theme_font_size_override("font_size", 11)
	hint.add_theme_color_override("font_color", Color(1, 1, 1, 0.5))
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	targets_section.add_child(hint)

	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 2)
	targets_section.add_child(box)

	for opt in TARGET_OPTIONS:
		var cb := CheckBox.new()
		cb.text = str(opt["label"])
		cb.button_pressed = (str(opt["key"]) == "Fighters")
		box.add_child(cb)
		target_checks[str(opt["key"])] = cb

func _build_frame_section() -> void:
	_add_section("卡面")

	_add_label("卡面路径")
	icon_edit = LineEdit.new()
	icon_edit.placeholder_text = "导入 PNG 后创建会自动导出场景"
	root.add_child(icon_edit)

	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 4)
	root.add_child(row)

	var import_btn := Button.new()
	import_btn.text = "导入 PNG"
	import_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	import_btn.pressed.connect(_on_import_icon_pressed)
	row.add_child(import_btn)

	var open_scene_btn := Button.new()
	open_scene_btn.text = "打开场景"
	open_scene_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	open_scene_btn.pressed.connect(_on_open_scene_editor)
	row.add_child(open_scene_btn)

	frame_status_label = Label.new()
	frame_status_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	frame_status_label.add_theme_font_size_override("font_size", 11)
	root.add_child(frame_status_label)

	icon_file_dialog = FileDialog.new()
	icon_file_dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	icon_file_dialog.access = FileDialog.ACCESS_RESOURCES
	icon_file_dialog.add_filter("*.png", "PNG 图片")
	icon_file_dialog.title = "选择卡面 PNG"
	icon_file_dialog.confirmed.connect(_on_icon_file_confirmed)
	add_child(icon_file_dialog)

func _add_type_row(rows_box: VBoxContainer, types: Array) -> void:
	if rows_box == null:
		return
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 4)

	var opt := OptionButton.new()
	opt.name = "TypeOption"
	opt.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	for t in types:
		opt.add_item(str(t))
	row.add_child(opt)

	var num := LineEdit.new()
	num.name = "NumEdit"
	num.placeholder_text = "数字"
	num.custom_minimum_size = Vector2(64, 0)
	row.add_child(num)

	var del := Button.new()
	del.text = "×"
	del.pressed.connect(func():
		if is_instance_valid(row):
			rows_box.remove_child(row)
			row.queue_free()
	)
	row.add_child(del)

	rows_box.add_child(row)

func _add_default_type_rows() -> void:
	_clear_type_rows(hp_type_rows)
	_clear_type_rows(atk_type_rows)
	_clear_type_rows(star_type_rows)
	_add_type_row(hp_type_rows, Config.get_hp_types())
	_add_type_row(atk_type_rows, Config.get_atk_types())

func _clear_type_rows(rows_box: VBoxContainer) -> void:
	if rows_box == null:
		return
	for c in rows_box.get_children():
		rows_box.remove_child(c)
		c.queue_free()

func _sync_type_row_options(rows_box: VBoxContainer, types: Array) -> void:
	if rows_box == null:
		return
	for row in rows_box.get_children():
		var opt = row.get_node_or_null("TypeOption")
		if opt == null:
			continue
		var prev: String = ""
		if opt.item_count > 0 and opt.selected >= 0:
			prev = str(opt.get_item_text(opt.selected))
		opt.clear()
		for t in types:
			opt.add_item(str(t))
		if types.is_empty():
			continue
		var idx: int = -1
		for i in types.size():
			if str(types[i]) == prev:
				idx = i
				break
		opt.select(idx if idx >= 0 else 0)

func _collect_type_array(rows_box: VBoxContainer, fallback: String) -> String:
	if rows_box == null:
		return fallback
	var parts: Array = []
	for row in rows_box.get_children():
		var opt = row.get_node_or_null("TypeOption")
		if opt == null:
			continue
		var t: String = str(opt.get_item_text(opt.selected)).strip_edges()
		if t.is_empty():
			continue
		var num = row.get_node_or_null("NumEdit")
		var n: String = ""
		if num != null:
			n = str(num.text).strip_edges()
		if n.is_empty():
			parts.append(t)
		else:
			parts.append("%s:%s" % [t, n])
	if parts.is_empty():
		return fallback
	return ",".join(parts)

func _on_import_icon_pressed() -> void:
	icon_file_dialog.popup_centered_ratio(0.6)

func _on_icon_file_confirmed() -> void:
	var path := icon_file_dialog.current_path
	if path.is_empty():
		return
	var tex := load(path) as Texture2D
	if tex == null:
		_frame_status("无法加载该 PNG：%s" % path, true)
		return
	current_icon_path = path
	current_icon_texture = tex
	if scene_editor_window and is_instance_valid(scene_editor_window):
		_refresh_scene_editor()
	_frame_status("已导入卡面：%s" % path, false)

func _on_open_scene_editor() -> void:
	_ensure_scene_editor()
	_refresh_scene_editor()
	scene_editor_window.popup_centered()

func _ensure_scene_editor() -> void:
	if scene_editor_window == null or not is_instance_valid(scene_editor_window):
		scene_editor_window = WindowScript.new()
		scene_editor_window.icon_changed.connect(func(): pass)
		add_child(scene_editor_window)
		_refresh_scene_editor()

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

func _export_frame_scene_internal() -> String:
	if current_icon_texture == null:
		_frame_status("请先导入卡面 PNG", true)
		return ""
	if scene_editor_window == null or not is_instance_valid(scene_editor_window):
		_frame_status("场景编辑器未就绪", true)
		return ""

	var root_node := Control.new()
	root_node.name = "卡面"
	root_node.mouse_filter = Control.MOUSE_FILTER_IGNORE

	var icon := TextureRect.new()
	icon.name = "卡牌图标"
	icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	icon.stretch_mode = TextureRect.STRETCH_KEEP_CENTERED
	icon.position = scene_editor_window.get_icon_position()
	icon.size = scene_editor_window.get_icon_size()
	icon.scale = scene_editor_window.get_icon_scale()
	icon.texture = current_icon_texture
	root_node.add_child(icon)
	icon.owner = root_node

	var packed := PackedScene.new()
	packed.pack(root_node)

	var file_base := _sanitize(key_name_edit.text.strip_edges())
	if file_base.is_empty():
		file_base = "%s_%s" % [_card_type(), _rarity()]

	var save_path := FRAME_EXPORT_DIR + file_base + ".tscn"
	DirAccess.make_dir_recursive_absolute(save_path.get_base_dir())
	var err := ResourceSaver.save(packed, save_path)
	root_node.queue_free()
	if err != OK:
		_frame_status("保存失败：%s" % err, true)
		return ""
	return save_path

func _is_fighter() -> bool:
	return _card_type() == "单位"

func _is_environment() -> bool:
	return _card_type() == "环境"

func _is_trick() -> bool:
	return _card_type() == "锦囊"

func _card_type() -> String:
	return CARD_TYPE_TEXTS[card_type.selected]

func _rarity() -> String:
	return RARITIES[rarity_option.selected]

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

func _frame_status(msg: String, is_error: bool) -> void:
	if frame_status_label:
		frame_status_label.text = msg
		frame_status_label.modulate = Color(1, 0.45, 0.35) if is_error else Color(0.45, 1, 0.55)

func refresh_from_config() -> void:
	if not is_inside_tree():
		return
	_refresh_type_options()
	_refresh_tags()
	_refresh_bool_props()

func _refresh_type_options() -> void:
	_sync_type_row_options(hp_type_rows, Config.get_hp_types())
	_sync_type_row_options(atk_type_rows, Config.get_atk_types())
	_sync_type_row_options(star_type_rows, Config.get_star_types())

func _refresh_tags() -> void:
	if tags_box == null or tags_section == null:
		return
	var is_fighter := card_type.selected == 0
	tags_section.visible = is_fighter
	if not is_fighter:
		_clear_tag_checks()
		return

	var old_state := {}
	for k in tag_checks:
		var cb = tag_checks[k]
		if is_instance_valid(cb):
			old_state[k] = cb.button_pressed
	for c in tags_box.get_children():
		tags_box.remove_child(c)
		c.queue_free()
	tag_checks.clear()

	var side_text := faction.get_item_text(faction.selected)
	for tag in Config.get_tags():
		var p_side := str(tag.get("side", "全部"))
		if p_side != "全部" and p_side != side_text:
			continue
		var lbl := str(tag.get("label", "")).strip_edges()
		if lbl.is_empty():
			continue
		var cb := CheckBox.new()
		cb.text = lbl
		cb.button_pressed = bool(old_state.get(lbl, false))
		tags_box.add_child(cb)
		tag_checks[lbl] = cb

	if tag_checks.is_empty():
		var empty_lbl := Label.new()
		empty_lbl.text = "暂无可用关键词 请在扩展配置中添加"
		empty_lbl.add_theme_font_size_override("font_size", 11)
		empty_lbl.add_theme_color_override("font_color", Color(1, 1, 1, 0.5))
		empty_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		tags_box.add_child(empty_lbl)

func _clear_tag_checks() -> void:
	for k in tag_checks:
		var cb = tag_checks[k]
		if is_instance_valid(cb):
			cb.button_pressed = false
	tag_checks.clear()

func _refresh_bool_props() -> void:
	if bool_props_box == null:
		return
	var old_state := {}
	for k in bool_prop_checks:
		var cb = bool_prop_checks[k]
		if is_instance_valid(cb):
			old_state[k] = cb.button_pressed
	for c in bool_props_box.get_children():
		bool_props_box.remove_child(c)
		c.queue_free()
	bool_prop_checks.clear()

	var side_text := faction.get_item_text(faction.selected)
	var type_text: String = CARD_TYPE_TEXTS[card_type.selected]

	for prop in Config.get_bool_props():
		if not _prop_applies(prop, side_text, type_text):
			continue
		var key := str(prop.get("key", "")).strip_edges()
		if key.is_empty():
			continue
		var cb := CheckBox.new()
		var lbl := str(prop.get("label", ""))
		if lbl.is_empty():
			lbl = key
		cb.text = lbl
		cb.button_pressed = bool(old_state.get(key, false))
		bool_props_box.add_child(cb)
		bool_prop_checks[key] = cb

	if bool_prop_checks.is_empty():
		var empty_lbl := Label.new()
		empty_lbl.text = "暂无可用扩展属性 请在扩展配置中添加"
		empty_lbl.add_theme_font_size_override("font_size", 11)
		empty_lbl.add_theme_color_override("font_color", Color(1, 1, 1, 0.5))
		empty_lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		bool_props_box.add_child(empty_lbl)

func _prop_applies(prop: Dictionary, side_text: String, type_text: String) -> bool:
	var p_side := str(prop.get("side", "全部"))
	var p_type := str(prop.get("card_type", "全部"))
	if p_side != "全部" and p_side != side_text:
		return false
	if p_type != "全部" and p_type != type_text:
		return false
	return true

func _add_label(text: String) -> void:
	_add_label_to(root, text)

func _add_label_to(parent: Node, text: String) -> void:
	var label := Label.new()
	label.text = text
	parent.add_child(label)

func _on_faction_selected(_index: int) -> void:
	var is_zombie := faction.selected == 1
	var list := CATEGORIES_ZOMBIE if is_zombie else CATEGORIES_PLANT
	var prev_text := ""
	if category_option.selected >= 0:
		prev_text = category_option.get_item_text(category_option.selected)
	category_option.clear()
	for c in list:
		category_option.add_item(c)
	var idx := list.find(prev_text)
	category_option.select(idx if idx >= 0 else 0)
	_refresh_tags()
	_refresh_bool_props()

func _on_card_type_selected(index: int) -> void:
	hp_box.visible = (index == 0)
	if not hp_box.visible:
		include_atk.button_pressed = false
		include_star_type.button_pressed = false
		atk_box.visible = false
		star_type_box.visible = false
	if targets_section:
		targets_section.visible = (index != 0)
	_refresh_tags()
	_refresh_bool_props()
	_refresh_scene_editor()

func _on_include_atk_toggled(pressed: bool) -> void:
	atk_box.visible = pressed
	if pressed and atk_type_rows.get_child_count() == 0:
		_add_type_row(atk_type_rows, Config.get_atk_types())

func _on_include_star_type_toggled(pressed: bool) -> void:
	star_type_box.visible = pressed
	if pressed and star_type_rows.get_child_count() == 0:
		_add_type_row(star_type_rows, Config.get_star_types())

func _on_include_pack_toggled(pressed: bool) -> void:
	pack_box.visible = pressed

func _build_type_text() -> String:
	var base_text: String = ["单位", "锦囊", "环境"][card_type.selected]
	if hero_check.button_pressed:
		return base_text + ";英雄"
	return base_text

func _collect_targets() -> Array:
	var out := []
	for opt in TARGET_OPTIONS:
		var key := str(opt["key"])
		if target_checks.has(key):
			var cb: CheckBox = target_checks[key]
			if is_instance_valid(cb) and cb.button_pressed:
				out.append(key)
	return out

func _create() -> void:
	var raw := key_name_edit.text.strip_edges()
	if raw.is_empty():
		_show_error("请输入类名")
		return
	var clean := _sanitize(raw)
	if clean.is_empty():
		_show_error("类名包含无效字符")
		return

	if current_icon_texture != null:
		_ensure_scene_editor()
		var auto_path := _export_frame_scene_internal()
		if auto_path.is_empty():
			_show_error("卡面场景导出失败")
			return
		icon_edit.text = auto_path

	var side: String = faction.get_item_text(faction.selected)
	var type_text := _build_type_text()

	var entry := _build_entry(clean, side, type_text)
	var final_id := _save_to_cards(clean, entry)
	_print_entry(clean, entry)

	var cs_path := _create_csharp_script(clean, type_text)

	if editor_plugin:
		editor_plugin.get_editor_interface().get_resource_filesystem().scan()

	if auto_clear_check.button_pressed:
		_clear_all()

	if cs_path.is_empty():
		_show_success("已写入 cards.json：%s Id=%d" % [clean, final_id])
	else:
		_show_success("已写入 cards.json：%s Id=%d\n已创建 C# 脚本：%s" % [clean, final_id, cs_path])

func _build_entry(clean: String, side: String, type_text: String) -> Dictionary:
	var is_zombie := side == "僵尸"
	var entry := {}
	entry["%s.Title" % clean] = name_edit.text
	entry["%s.Des" % clean] = des_edit.text
	entry["%s.Cost" % clean] = int(cost_spin.value)
	entry["%s.Camp" % clean] = "僵尸" if is_zombie else "植物"
	entry["%s.Type" % clean] = type_text
	entry["%s.Rarity" % clean] = _option_text(rarity_option, "")
	entry["%s.Category" % clean] = _option_text(category_option, "")
	entry["%s.Animation" % clean] = animation_edit.text
	if not animation_extra_edit.text.is_empty():
		entry["%s.Scene" % clean] = animation_extra_edit.text
	entry["%s.Icon" % clean] = icon_edit.text

	var labels := []
	for s in label_edit.text.split(",", false):
		var t := s.strip_edges()
		if not t.is_empty():
			labels.append(t)
	entry["%s.Label" % clean] = labels

	var tags := []
	if card_type.selected == 0:
		for lbl in tag_checks:
			var cb: CheckBox = tag_checks[lbl]
			if is_instance_valid(cb) and cb.button_pressed:
				tags.append(lbl)
	entry["%s.Tag" % clean] = tags

	if card_type.selected == 0:
		entry["%s.Hp" % clean] = int(hp_spin.value)
		entry["%s.HpType" % clean] = _collect_type_array(hp_type_rows, "普通")

		if include_atk.button_pressed:
			entry["%s.Atk" % clean] = int(atk_spin.value)
			entry["%s.AtkType" % clean] = _collect_type_array(atk_type_rows, "普通")
		else:
			entry["%s.Atk" % clean] = 0
			entry["%s.AtkType" % clean] = "普通"

		if include_star_type.button_pressed:
			entry["%s.StarType" % clean] = _collect_type_array(star_type_rows, "星星")

	if include_pack.button_pressed:
		entry["%s.Pack" % clean] = pack_edit.text

	for prop in Config.get_bool_props():
		var key := str(prop.get("key", "")).strip_edges()
		if key.is_empty() or not bool_prop_checks.has(key):
			continue
		var cb: CheckBox = bool_prop_checks[key]
		entry["%s.%s" % [clean, key]] = cb.button_pressed

	entry["%s.Flavor" % clean] = flavor_edit.text
	entry["%s.Id" % clean] = id_edit.text.to_int()
	return entry

func _load_cards_json() -> Dictionary:
	if not FileAccess.file_exists(CARDS_JSON_PATH):
		return {}
	var f := FileAccess.open(CARDS_JSON_PATH, FileAccess.READ)
	if f == null:
		return {}
	var txt := f.get_as_text()
	f.close()
	var parsed = JSON.parse_string(txt)
	if parsed is Dictionary:
		return parsed
	return {}

func _collect_existing_ids(cards: Dictionary) -> Dictionary:
	var ids := {}
	for k in cards.keys():
		if str(k).ends_with(".Id"):
			var v = cards[k]
			if v is float or v is int:
				ids[int(v)] = true
	return ids

func _resolve_unique_id(cards: Dictionary, desired: int) -> int:
	var ids := _collect_existing_ids(cards)
	if not ids.has(desired):
		return desired
	var max_id := 0
	for id in ids.keys():
		if id > max_id:
			max_id = id
	var candidate := max_id + 1
	while ids.has(candidate):
		candidate += 1
	return candidate

func _save_to_cards(clean: String, entry: Dictionary) -> int:
	var cards := _load_cards_json()
	var desired := int(entry.get("%s.Id" % clean, 0))
	var final_id := _resolve_unique_id(cards, desired)
	entry["%s.Id" % clean] = final_id
	cards.merge(entry, true)

	var dir := CARDS_JSON_PATH.get_base_dir()
	if not DirAccess.dir_exists_absolute(dir):
		DirAccess.make_dir_recursive_absolute(dir)

	var f := FileAccess.open(CARDS_JSON_PATH, FileAccess.WRITE)
	if f == null:
		_show_error("无法写入：%s" % CARDS_JSON_PATH)
		return final_id
	f.store_string(JSON.stringify(cards, "\t"))
	f.close()
	return final_id

func _print_entry(clean: String, entry: Dictionary) -> void:
	print("===== cards.json ：%s =====" % clean)
	for k in entry.keys():
		var v = entry[k]
		if v is Array:
			print("\"%s\": %s" % [k, JSON.stringify(v)])
		elif v is bool:
			print("\"%s\": %s" % [k, "true" if v else "false"])
		elif v is String:
			print("\"%s\": \"%s\"" % [k, v])
		else:
			print("\"%s\": %s" % [k, str(v)])
	print("=====================================")

func _option_text(option: OptionButton, fallback: String) -> String:
	if option != null and option.item_count > 0 and option.selected >= 0:
		return option.get_item_text(option.selected)
	return fallback

func _clear_all() -> void:
	key_name_edit.text = ""
	name_edit.text = ""
	faction.select(0)
	_on_faction_selected(0)
	card_type.select(0)
	_on_card_type_selected(0)
	hero_check.button_pressed = false
	des_edit.text = ""
	cost_spin.value = 0
	hp_spin.value = 0
	include_atk.button_pressed = false
	atk_spin.value = 0
	include_star_type.button_pressed = false
	include_pack.button_pressed = false
	pack_edit.text = ""
	_add_default_type_rows()
	for cb in tag_checks.values():
		if is_instance_valid(cb):
			cb.button_pressed = false
	for cb in bool_prop_checks.values():
		if is_instance_valid(cb):
			cb.button_pressed = false
	for k in target_checks:
		var cb: CheckBox = target_checks[k]
		if is_instance_valid(cb):
			cb.button_pressed = (k == "Fighters")
	flavor_edit.text = ""
	id_edit.text = ""
	rarity_option.select(0)
	category_option.select(0)
	label_edit.text = ""
	animation_edit.text = ""
	animation_extra_edit.text = ""
	icon_edit.text = ""
	current_icon_texture = null
	current_icon_path = ""
	if scene_editor_window and is_instance_valid(scene_editor_window):
		scene_editor_window.set_icon_texture(null)
	if frame_status_label:
		frame_status_label.text = ""
	status_label.text = ""

func _create_csharp_script(clean: String, type_text: String) -> String:
	var _class_name := _to_cs_class_name(clean)
	var folder_name := _sanitize(name_edit.text.strip_edges())
	if folder_name.is_empty():
		folder_name = _class_name
	var dir := CS_SCRIPT_ROOT + folder_name + "/"
	var path := dir + _class_name + ".cs"
	if FileAccess.file_exists(path):
		return path
	if not DirAccess.dir_exists_absolute(dir):
		DirAccess.make_dir_recursive_absolute(dir)
	var is_fighter := type_text.begins_with("单位")
	var targets: Array = [] if is_fighter else _collect_targets()
	var content := _build_cs_template(_class_name, is_fighter, targets)
	var f := FileAccess.open(path, FileAccess.WRITE)
	if f == null:
		push_error("[PVZH] 无法创建 C# 脚本：" + path)
		return ""
	f.store_string(content)
	f.close()
	return path

func _build_cs_template(_class_name: String, is_fighter: bool, targets: Array) -> String:
	if is_fighter:
		return """using System.Threading.Tasks;
using Battle;
using Battle.Entity;
using Play;

namespace Card;

public class %s : FighterCardModel
{
    public override async Task AnimationWhenPlayed(Road road)
    {
        await base.AnimationWhenPlayed(road);
    }
}
""" % _class_name

	var target_line := ""
	if not targets.is_empty():
		var parts := []
		for t in targets:
			parts.append("Target.TargetType.%s" % str(t))
		target_line = "    public override TargetType TargetType => %s;\n\n" % " | ".join(parts)

	return """using System.Threading.Tasks;
using Target;

namespace Card;

public class %s : CardModel
{
%s    public override async Task Play(ITarget target)
    {
        await base.Play(target);
    }
}
""" % [_class_name, target_line]

func _to_cs_class_name(raw: String) -> String:
	var result := ""
	for i in raw.length():
		var ch := raw[i]
		var code := ch.unicode_at(0)
		if (code >= 65 and code <= 90) or (code >= 97 and code <= 122) or (code >= 48 and code <= 57) or ch == "_":
			result += ch
		else:
			result += "_"
	if result.replace("_", "").is_empty():
		return "NewCard"
	if result[0] >= "0" and result[0] <= "9":
		result = "_" + result
	return result

func _sanitize(value: String) -> String:
	var result := ""
	for ch in value:
		if ch in "\\/:*?\"<>|\n\r\t":
			continue
		result += ch
	return result.strip_edges()

func _show_error(message: String) -> void:
	status_label.text = message
	status_label.modulate = Color(1.0, 0.45, 0.35)
	push_error("[PVZH] " + message)

func _show_success(message: String) -> void:
	status_label.text = message
	status_label.modulate = Color(0.45, 1.0, 0.55)
	print("[PVZH] " + message)
