@tool
extends ScrollContainer

var editor_plugin: EditorPlugin
var root: VBoxContainer

var name_edit: LineEdit
var key_name_edit: LineEdit
var faction: OptionButton
var card_type: OptionButton
var hero_check: CheckBox

var des_edit: TextEdit
var animation_edit: LineEdit
var icon_edit: LineEdit
var label_edit: LineEdit
var cost_spin: SpinBox
var rarity_option: OptionButton
var category_option: OptionButton

var include_hp: CheckBox
var include_atk: CheckBox
var include_star_type: CheckBox
var include_pack: CheckBox
var include_flavor: CheckBox

var hp_box: VBoxContainer
var atk_box: VBoxContainer
var star_type_box: VBoxContainer
var pack_box: VBoxContainer
var flavor_box: VBoxContainer

var hp_spin: SpinBox
var hp_type_option: OptionButton
var atk_spin: SpinBox
var atk_type_option: OptionButton
var star_type_option: OptionButton
var pack_edit: LineEdit
var flavor_edit: TextEdit

var status_label: Label

const RARITIES := ["基础", "普通", "罕见", "稀有", "超稀有", "传说", "活动", "令牌"]
const CATEGORIES := ["猛长", "光能", "守卫", "爆花", "聪明", "有脑", "疯狂", "野兽", "狡猾", "健壮"]
const HP_TYPES := ["普通"]
const ATK_TYPES := ["普通"]
const STAR_TYPES := ["星星"]

func _ready() -> void:
	horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED

	root = VBoxContainer.new()
	root.custom_minimum_size = Vector2(280, 0)
	root.add_theme_constant_override("separation", 8)
	root.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	add_child(root)

	var title := Label.new()
	title.text = "PVZH 快速创建"
	title.add_theme_font_size_override("font_size", 18)
	root.add_child(title)

	# ---------- 基础 ----------
	_add_label("类名（用于索引）")
	key_name_edit = LineEdit.new()
	key_name_edit.placeholder_text = "例如：SnowPea"
	root.add_child(key_name_edit)

	_add_label("名称")
	name_edit = LineEdit.new()
	name_edit.placeholder_text = "例如：寒冰射手"
	root.add_child(name_edit)

	_add_label("阵营")
	faction = OptionButton.new()
	faction.add_item("植物", 0)
	faction.add_item("僵尸", 1)
	root.add_child(faction)

	_add_label("卡牌类型")
	card_type = OptionButton.new()
	card_type.add_item("单位卡（FIGHTER）", 0)
	card_type.add_item("锦囊卡（TRICK）", 1)
	card_type.add_item("环境卡（ENVIRONMENT）", 2)
	root.add_child(card_type)

	hero_check = CheckBox.new()
	hero_check.text = "超能力?"
	root.add_child(hero_check)

	# ---------- 描述 ----------
	_add_label("描述（Des）")
	des_edit = TextEdit.new()
	des_edit.custom_minimum_size = Vector2(0, 60)
	des_edit.placeholder_text = "支持 _下划线_ 、 =图标= 、 =图标={N}"
	root.add_child(des_edit)

	# ---------- 数值 ----------
	_add_label("费用（Cost）")
	cost_spin = SpinBox.new()
	cost_spin.min_value = 0
	cost_spin.max_value = 99
	root.add_child(cost_spin)

	# ---------- 可选字段 ----------
	_add_label("可选字段")

	# Hp
	include_hp = CheckBox.new()
	include_hp.text = "有血量?"
	include_hp.toggled.connect(_on_include_hp_toggled)
	root.add_child(include_hp)

	hp_box = VBoxContainer.new()
	hp_box.visible = false
	hp_box.add_theme_constant_override("separation", 4)
	root.add_child(hp_box)

	_add_label_to(hp_box, "Hp")
	hp_spin = SpinBox.new()
	hp_spin.min_value = 0
	hp_spin.max_value = 999
	hp_box.add_child(hp_spin)

	_add_label_to(hp_box, "HpType")
	hp_type_option = OptionButton.new()
	for t in HP_TYPES:
		hp_type_option.add_item(t)
	hp_box.add_child(hp_type_option)

	# Atk
	include_atk = CheckBox.new()
	include_atk.text = "有伤害?"
	include_atk.toggled.connect(_on_include_atk_toggled)
	root.add_child(include_atk)

	atk_box = VBoxContainer.new()
	atk_box.visible = false
	atk_box.add_theme_constant_override("separation", 4)
	root.add_child(atk_box)

	_add_label_to(atk_box, "Atk")
	atk_spin = SpinBox.new()
	atk_spin.min_value = 0
	atk_spin.max_value = 999
	atk_box.add_child(atk_spin)

	_add_label_to(atk_box, "AtkType")
	atk_type_option = OptionButton.new()
	for t in ATK_TYPES:
		atk_type_option.add_item(t)
	atk_box.add_child(atk_type_option)

	# StarType
	include_star_type = CheckBox.new()
	include_star_type.text = "有等级?"
	include_star_type.toggled.connect(_on_include_star_type_toggled)
	root.add_child(include_star_type)

	star_type_box = VBoxContainer.new()
	star_type_box.visible = false
	star_type_box.add_theme_constant_override("separation", 4)
	root.add_child(star_type_box)

	_add_label_to(star_type_box, "StarType")
	star_type_option = OptionButton.new()
	for t in STAR_TYPES:
		star_type_option.add_item(t)
	star_type_box.add_child(star_type_option)

	# Pack
	include_pack = CheckBox.new()
	include_pack.text = "有卡包?"
	include_pack.toggled.connect(_on_include_pack_toggled)
	root.add_child(include_pack)

	pack_box = VBoxContainer.new()
	pack_box.visible = false
	pack_box.add_theme_constant_override("separation", 4)
	root.add_child(pack_box)

	_add_label_to(pack_box, "卡包（Pack）")
	pack_edit = LineEdit.new()
	pack_edit.placeholder_text = "高级"
	pack_box.add_child(pack_edit)

	# Flavor
	include_flavor = CheckBox.new()
	include_flavor.text = "有特别描述?"
	include_flavor.toggled.connect(_on_include_flavor_toggled)
	root.add_child(include_flavor)

	flavor_box = VBoxContainer.new()
	flavor_box.visible = false
	flavor_box.add_theme_constant_override("separation", 4)
	root.add_child(flavor_box)

	_add_label_to(flavor_box, "特别描述（Flavor）")
	flavor_edit = TextEdit.new()
	flavor_edit.custom_minimum_size = Vector2(0, 40)
	flavor_box.add_child(flavor_edit)

	# ---------- 其余 ----------
	_add_label("稀有度（Rarity）")
	rarity_option = OptionButton.new()
	for r in RARITIES:
		rarity_option.add_item(r)
	root.add_child(rarity_option)

	_add_label("派系（Category）")
	category_option = OptionButton.new()
	for c in CATEGORIES:
		category_option.add_item(c)
	root.add_child(category_option)

	_add_label("标签（Label，逗号分隔）")
	label_edit = LineEdit.new()
	label_edit.placeholder_text = "豌豆,水果"
	root.add_child(label_edit)

	_add_label("动画路径（Animation）")
	animation_edit = LineEdit.new()
	animation_edit.placeholder_text = "res://..."
	root.add_child(animation_edit)

	_add_label("卡面（Icon）")
	icon_edit = LineEdit.new()
	icon_edit.placeholder_text = "res://..."
	root.add_child(icon_edit)
	# ---------- 按钮 ----------
	var button := Button.new()
	button.text = "创建"
	button.pressed.connect(_create)
	root.add_child(button)

	status_label = Label.new()
	status_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	root.add_child(status_label)

func _add_label(text: String) -> void:
	_add_label_to(root, text)

func _add_label_to(parent: Node, text: String) -> void:
	var label := Label.new()
	label.text = text
	parent.add_child(label)

func _on_include_hp_toggled(pressed: bool) -> void:
	hp_box.visible = pressed

func _on_include_atk_toggled(pressed: bool) -> void:
	atk_box.visible = pressed

func _on_include_star_type_toggled(pressed: bool) -> void:
	star_type_box.visible = pressed

func _on_include_pack_toggled(pressed: bool) -> void:
	pack_box.visible = pressed

func _on_include_flavor_toggled(pressed: bool) -> void:
	flavor_box.visible = pressed

func _build_type_text() -> String:
	var base_text: String = ["单位", "锦囊", "环境"][card_type.selected]
	if hero_check.button_pressed:
		return base_text + ";英雄"
	return base_text

func _create() -> void:
	var raw := key_name_edit.text.strip_edges()
	if raw.is_empty():
		_show_error("请输入名称")
		return
	var clean := _sanitize(raw)
	if clean.is_empty():
		_show_error("名称包含无效字符")
		return

	var side: String = faction.get_item_text(faction.selected)
	var type_text := _build_type_text()

	_print_card_entry(clean, side, type_text)

	_show_success("已输出：" + clean)

func _print_card_entry(clean: String, side: String, type_text: String) -> void:
	var entry := {}
	entry["\"%s.Title\"" % clean] = name_edit.text
	entry["\"%s.Des\"" % clean] = des_edit.text
	entry["\"%s.Cost\"" % clean] = int(cost_spin.value)
	entry["\"%s.Camp\"" % clean] = "植物" if side == "植物" else "僵尸"
	entry["\"%s.Type\"" % clean] = type_text
	entry["\"%s.Rarity\"" % clean] = rarity_option.get_item_text(rarity_option.selected)
	entry["\"%s.Category\"" % clean] = category_option.get_item_text(category_option.selected)
	entry["\"%s.Animation\"" % clean] = animation_edit.text
	entry["\"%s.Icon\"" % clean] = icon_edit.text

	var labels := []
	for s in label_edit.text.split(",", false):
		var t := s.strip_edges()
		if not t.is_empty():
			labels.append(t)
	entry["\"%s.Label\"" % clean] = labels

	if include_hp.button_pressed:
		entry["\"%s.Hp\"" % clean] = int(hp_spin.value)
		entry["\"%s.HpType\"" % clean] = hp_type_option.get_item_text(hp_type_option.selected)

	if include_atk.button_pressed:
		entry["\"%s.Atk\"" % clean] = int(atk_spin.value)
		entry["\"%s.AtkType\"" % clean] = atk_type_option.get_item_text(atk_type_option.selected)

	if include_star_type.button_pressed:
		entry["\"%s.StarType\"" % clean] = star_type_option.get_item_text(star_type_option.selected)
	if include_pack.button_pressed:
		entry["\"%s.Pack\"" % clean] = pack_edit.text
	if include_flavor.button_pressed:
		entry["\"%s.Flavor\"" % clean] = flavor_edit.text

	print("===== cards.json ：" + clean + " =====")
	for k in entry.keys():
		var v = entry[k]
		if v is Array:
			print("%s: %s" % [k, JSON.stringify(v)])
		elif v is String:
			print("%s: \"%s\"" % [k, v])
		else:
			print("%s: %s" % [k, str(v)])
	print("=====================================")

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
