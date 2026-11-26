@tool
extends ScrollContainer

const Config := preload("res://addons/pvzh_card_creator/creator_config.gd")

const SIDES := ["全部", "植物", "僵尸"]
const CARD_TYPES := ["全部", "单位", "锦囊", "环境"]

var root: VBoxContainer
var hp_list: ItemList
var atk_list: ItemList
var star_list: ItemList
var bool_props_box: VBoxContainer

var bp_label_edit: LineEdit
var bp_key_edit: LineEdit
var bp_side_option: OptionButton
var bp_type_option: OptionButton

var tag_list_box: VBoxContainer
var tg_label_edit: LineEdit
var tg_side_option: OptionButton

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
	title.text = "扩展配置"
	title.add_theme_font_size_override("font_size", 18)
	root.add_child(title)

	var hint := Label.new()
	hint.text = "修改会立即保存并同步到创建卡牌面板"
	hint.add_theme_font_size_override("font_size", 11)
	hint.add_theme_color_override("font_color", Color(1, 1, 1, 0.5))
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(hint)

	hp_list = _make_type_section("生命值类型", "hp_types", false)
	atk_list = _make_type_section("攻击力类型", "atk_types")
	star_list = _make_type_section("等级类型", "star_types")

	_build_tags_section()
	_build_bool_props_section()

	_reload_all()

func _add_section(text: String, with_sep: bool = true) -> void:
	if with_sep:
		var sep := HSeparator.new()
		root.add_child(sep)
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", 15)
	l.add_theme_color_override("font_color", Color(0.75, 0.85, 1.0))
	root.add_child(l)

func _add_hint(text: String) -> void:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", 11)
	l.add_theme_color_override("font_color", Color(1, 1, 1, 0.5))
	l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(l)

func _make_type_section(title_text: String, list_key: String, with_sep: bool = true) -> ItemList:
	_add_section(title_text, with_sep)

	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 4)
	root.add_child(box)

	var list := ItemList.new()
	list.custom_minimum_size = Vector2(0, 90)
	list.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	list.select_mode = ItemList.SELECT_MULTI
	box.add_child(list)

	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 4)
	box.add_child(row)

	var input := LineEdit.new()
	input.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	input.placeholder_text = "新增项"
	row.add_child(input)

	var add_btn := Button.new()
	add_btn.text = "添加"
	add_btn.pressed.connect(func():
		var txt := input.text.strip_edges()
		if txt.is_empty():
			return
		var arr: Array = Config.data[list_key]
		if arr.has(txt):
			return
		arr.append(txt)
		Config.save()
		_reload_all()
		input.text = ""
	)
	row.add_child(add_btn)

	var del_btn := Button.new()
	del_btn.text = "删除选中"
	del_btn.pressed.connect(func():
		var sel := list.get_selected_items()
		var arr: Array = Config.data[list_key]
		var idxs: Array = []
		for s in sel:
			idxs.append(int(s))
		idxs.sort()
		idxs.reverse()
		for i in idxs:
			var idx: int = int(i)
			if idx >= 0 and idx < arr.size():
				arr.remove_at(idx)
		Config.save()
		_reload_all()
	)
	row.add_child(del_btn)

	return list

func _build_tags_section() -> void:
	_add_section("关键词")
	_add_hint("仅单位卡显示 可限定阵营")

	tag_list_box = VBoxContainer.new()
	tag_list_box.add_theme_constant_override("separation", 2)
	root.add_child(tag_list_box)

	var row1 := HBoxContainer.new()
	row1.add_theme_constant_override("separation", 4)
	root.add_child(row1)

	tg_label_edit = LineEdit.new()
	tg_label_edit.placeholder_text = "关键词名称 如 组队"
	tg_label_edit.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row1.add_child(tg_label_edit)

	var row2 := HBoxContainer.new()
	row2.add_theme_constant_override("separation", 4)
	root.add_child(row2)

	tg_side_option = OptionButton.new()
	for s in SIDES:
		tg_side_option.add_item(str(s))
	row2.add_child(tg_side_option)

	var add_btn := Button.new()
	add_btn.text = "添加关键词"
	add_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	add_btn.pressed.connect(_on_add_tag)
	row2.add_child(add_btn)

func _on_add_tag() -> void:
	var label := tg_label_edit.text.strip_edges()
	if label.is_empty():
		return
	var side: String = str(SIDES[tg_side_option.selected])
	Config.add_tag(label, side)
	Config.save()
	_reload_tags()
	tg_label_edit.text = ""

func _build_bool_props_section() -> void:
	_add_section("自定义 Bool 属性")
	_add_hint("可限定阵营与卡牌类型")

	bool_props_box = VBoxContainer.new()
	bool_props_box.add_theme_constant_override("separation", 2)
	root.add_child(bool_props_box)

	var row1 := HBoxContainer.new()
	row1.add_theme_constant_override("separation", 4)
	root.add_child(row1)

	bp_label_edit = LineEdit.new()
	bp_label_edit.placeholder_text = "显示名称 如 有护甲"
	bp_label_edit.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row1.add_child(bp_label_edit)

	bp_key_edit = LineEdit.new()
	bp_key_edit.placeholder_text = "JSON 键名 如 Armor"
	bp_key_edit.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row1.add_child(bp_key_edit)

	var row2 := HBoxContainer.new()
	row2.add_theme_constant_override("separation", 4)
	root.add_child(row2)

	bp_side_option = OptionButton.new()
	for s in SIDES:
		bp_side_option.add_item(str(s))
	row2.add_child(bp_side_option)

	bp_type_option = OptionButton.new()
	for ct in CARD_TYPES:
		bp_type_option.add_item(str(ct))
	row2.add_child(bp_type_option)

	var add_btn := Button.new()
	add_btn.text = "添加属性"
	add_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	add_btn.pressed.connect(_on_add_bool_prop)
	row2.add_child(add_btn)

func _on_add_bool_prop() -> void:
	var label := bp_label_edit.text.strip_edges()
	var key := bp_key_edit.text.strip_edges()
	if key.is_empty():
		return
	if label.is_empty():
		label = key
	var side: String = str(SIDES[bp_side_option.selected])
	var ct: String = str(CARD_TYPES[bp_type_option.selected])
	Config.add_bool_prop(key, label, side, ct)
	Config.save()
	_reload_bool_props()
	bp_label_edit.text = ""
	bp_key_edit.text = ""

func _reload_all() -> void:
	_fill_list(hp_list, Config.get_hp_types())
	_fill_list(atk_list, Config.get_atk_types())
	_fill_list(star_list, Config.get_star_types())
	_reload_tags()
	_reload_bool_props()

func _fill_list(list: ItemList, items: Array) -> void:
	if list == null:
		return
	list.clear()
	for it in items:
		list.add_item(str(it))

func _reload_tags() -> void:
	if tag_list_box == null:
		return
	for c in tag_list_box.get_children():
		tag_list_box.remove_child(c)
		c.queue_free()
	var arr: Array = Config.get_tags()
	if arr.is_empty():
		var empty_lbl := Label.new()
		empty_lbl.text = "暂无关键词"
		empty_lbl.add_theme_font_size_override("font_size", 11)
		empty_lbl.add_theme_color_override("font_color", Color(1, 1, 1, 0.5))
		tag_list_box.add_child(empty_lbl)
		return
	for i in arr.size():
		var tag: Dictionary = arr[i]
		var row := HBoxContainer.new()
		row.add_theme_constant_override("separation", 4)
		tag_list_box.add_child(row)

		var l := Label.new()
		l.text = "%s  [%s]" % [
			str(tag.get("label", "")),
			str(tag.get("side", "全部"))
		]
		l.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		row.add_child(l)

		var del := Button.new()
		del.text = "删除"
		var idx: int = i
		del.pressed.connect(func():
			Config.remove_tag(idx)
			Config.save()
			_reload_tags()
		)
		row.add_child(del)

func _reload_bool_props() -> void:
	if bool_props_box == null:
		return
	for c in bool_props_box.get_children():
		bool_props_box.remove_child(c)
		c.queue_free()
	var arr: Array = Config.get_bool_props()
	if arr.is_empty():
		var empty_lbl := Label.new()
		empty_lbl.text = "暂无自定义 Bool 属性"
		empty_lbl.add_theme_font_size_override("font_size", 11)
		empty_lbl.add_theme_color_override("font_color", Color(1, 1, 1, 0.5))
		bool_props_box.add_child(empty_lbl)
		return
	for i in arr.size():
		var prop: Dictionary = arr[i]
		var row := HBoxContainer.new()
		row.add_theme_constant_override("separation", 4)
		bool_props_box.add_child(row)

		var l := Label.new()
		l.text = "%s (%s)  [%s / %s]" % [
			str(prop.get("label", "")),
			str(prop.get("key", "")),
			str(prop.get("side", "全部")),
			str(prop.get("card_type", "全部"))
		]
		l.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		row.add_child(l)

		var del := Button.new()
		del.text = "删除"
		var idx: int = i
		del.pressed.connect(func():
			Config.remove_bool_prop(idx)
			Config.save()
			_reload_bool_props()
		)
		row.add_child(del)
