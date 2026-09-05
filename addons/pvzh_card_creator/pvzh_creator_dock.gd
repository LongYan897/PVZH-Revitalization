@tool
extends VBoxContainer

var editor_plugin: EditorPlugin
var name_edit: LineEdit
var create_type: OptionButton
var faction: OptionButton
var card_type: OptionButton
var create_unit_check: CheckBox
var status_label: Label

const ROOT := "res://"
const UNIT_DIRS := {"植物": "场景/植物卡", "僵尸": "场景/僵尸卡"}
const CARD_DIRS := {"植物": "场景/植物卡组", "僵尸": "场景/僵尸卡组"}
const UNIT_BASES := {"植物": "res://场景/植物卡/基础植物/基础植物.tscn", "僵尸": "res://场景/僵尸卡/基础僵尸/基础僵尸.tscn"}
const CARD_BASES := {"植物": "res://场景/植物卡组/基础植物/基础植物.tscn", "僵尸": "res://场景/僵尸卡组/基础僵尸/基础僵尸.tscn"}
const ANIMATION_DIRS := {"植物": "数据资源/植物动画", "僵尸": "数据资源/僵尸动画"}

func _ready() -> void:
	custom_minimum_size = Vector2(280, 0)
	add_theme_constant_override("separation", 8)
	var title := Label.new()
	title.text = "PVZH 快速创建"
	title.add_theme_font_size_override("font_size", 18)
	add_child(title)
	_add_label("名称")
	name_edit = LineEdit.new()
	name_edit.placeholder_text = "例如：寒冰射手"
	add_child(name_edit)
	_add_label("创建内容")
	create_type = OptionButton.new()
	create_type.add_item("单位", 0)
	create_type.add_item("卡牌", 1)
	create_type.item_selected.connect(_on_create_type_changed)
	add_child(create_type)
	_add_label("阵营")
	faction = OptionButton.new()
	faction.add_item("植物", 0)
	faction.add_item("僵尸", 1)
	add_child(faction)
	_add_label("卡牌类型")
	card_type = OptionButton.new()
	card_type.add_item("单位卡（TARGET）", 0)
	card_type.add_item("锦囊卡（PLAN）", 1)
	card_type.add_item("环境卡（ENVIRONMENT）", 2)
	card_type.disabled = true
	add_child(card_type)
	create_unit_check = CheckBox.new()
	create_unit_check.text = "卡牌同时创建对应单位"
	create_unit_check.button_pressed = true
	create_unit_check.visible = false
	add_child(create_unit_check)
	var button := Button.new()
	button.text = "创建"
	button.pressed.connect(_create)
	add_child(button)
	status_label = Label.new()
	status_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	add_child(status_label)

func _add_label(text: String) -> void:
	var label := Label.new()
	label.text = text
	add_child(label)

func _on_create_type_changed(index: int) -> void:
	var is_card := index == 1
	card_type.disabled = not is_card
	create_unit_check.visible = is_card

func _create() -> void:
	var raw := name_edit.text.strip_edges()
	if raw.is_empty():
		_show_error("请输入名称")
		return
	var clean := _sanitize(raw)
	if clean.is_empty():
		_show_error("名称包含无效字符")
		return
	var side: String = faction.get_item_text(faction.selected)
	var is_card := create_type.selected == 1
	var suffix := "植物" if side == "植物" else "僵尸"
	var object_name: String = clean if clean.ends_with(suffix) else clean + suffix
	var unit_created := false
	if not is_card or create_unit_check.button_pressed:
		var unit_result := _create_unit(side, object_name)
		if not unit_result.ok:
			_show_error(unit_result.message)
			return
		unit_created = true
	if is_card:
		var card_result := _create_card(side, object_name, card_type.selected, unit_created)
		if not card_result.ok:
			_show_error(card_result.message)
			return
	_show_success("创建成功：" + object_name)
	_refresh_filesystem()

func _create_unit(side: String, object_name: String) -> Dictionary:
	var folder: String = ROOT + str(UNIT_DIRS[side]) + "/" + object_name
	var scene_path: String = folder + "/" + object_name + ".tscn"
	var script_path: String = folder + "/" + object_name + ".gd"
	var data_dir: String = ROOT + ("数据资源/植物数据" if side == "植物" else "数据资源/僵尸数据")
	var data_path: String = data_dir + "/" + object_name + "数据.tres"
	var animation_dir: String = ROOT + str(ANIMATION_DIRS[side])
	var animation_path: String = animation_dir + "/" + object_name + "动画.tres"
	if _exists_any([folder, scene_path, script_path, data_path, animation_path]):
		return {"ok": false, "message": "单位已存在，已取消创建：" + object_name}
	if not _ensure_dir(folder) or not _ensure_dir(data_dir) or not _ensure_dir(animation_dir):
		return {"ok": false, "message": "无法创建单位目录"}
	var script_class := "BasePlant" if side == "植物" else "BaseZombie"
	var data_class := "PlantData" if side == "植物" else "ZombieData"
	var data_prop := "plant_data" if side == "植物" else "zombie_data"
	var script_text := "extends " + script_class + "\n"
	var data_text := "[gd_resource type=\"Resource\" script_class=\"" + data_class + "\" format=3]\n\n"
	data_text += "[ext_resource type=\"Script\" path=\"res://脚本（类）/" + data_class + ".gd\" id=\"1_data\"]\n\n"
	data_text += "[resource]\nscript = ExtResource(\"1_data\")\nname = \"" + object_name + "\"\n"
	var scene_text := "[gd_scene load_steps=5 format=3]\n\n"
	scene_text += "[ext_resource type=\"PackedScene\" path=\"" + UNIT_BASES[side] + "\" id=\"1_base\"]\n"
	scene_text += "[ext_resource type=\"Script\" path=\"" + script_path + "\" id=\"2_script\"]\n"
	scene_text += "[ext_resource type=\"Resource\" path=\"" + data_path + "\" id=\"3_data\"]\n\n"
	scene_text += "[ext_resource type=\"SpineSkeletonDataResource\" path=\"" + animation_path + "\" id=\"4_animation\"]\n\n"
	scene_text += "[node name=\"" + object_name + "\" instance=ExtResource(\"1_base\")]\n"
	scene_text += "script = ExtResource(\"2_script\")\n" + data_prop + " = ExtResource(\"3_data\")\n"
	# 新动画资源不引用任何 atlas 或 skeleton 文件，等待手动指定。
	_write(animation_path, "[gd_resource type=\"SpineSkeletonDataResource\" format=3]\n\n[resource]\n")
	scene_text += "\n[node name=\"植物动画\" parent=\"植物\" index=\"0\"]\n" if side == "植物" else "\n[node name=\"僵尸动画\" parent=\"僵尸\" index=\"0\"]\n"
	scene_text += "skeleton_data_res = ExtResource(\"4_animation\")\n"
	_write(script_path, script_text)
	_write(data_path, data_text)
	_write(scene_path, scene_text)
	return {"ok": true}

func _create_card(side: String, object_name: String, type_index: int, has_unit: bool) -> Dictionary:
	var folder: String = ROOT + str(CARD_DIRS[side]) + "/" + object_name
	var scene_path: String = folder + "/" + object_name + ".tscn"
	var script_path: String = folder + "/" + object_name + ".gd"
	if _exists_any([folder, scene_path, script_path]):
		return {"ok": false, "message": "卡牌已存在，已取消创建：" + object_name}
	if not _ensure_dir(folder):
		return {"ok": false, "message": "无法创建卡牌目录"}
	var script_class := "BasePlantCard" if side == "植物" else "BaseZombieCard"
	var data_prop := "plantData" if side == "植物" else "zombieData"
	var data_path: String = ROOT + ("数据资源/植物数据/" if side == "植物" else "数据资源/僵尸数据/") + object_name + "数据.tres"
	var script_text := "extends " + script_class + "\n"
	var scene_text := "[gd_scene load_steps=" + ("4" if has_unit else "3") + " format=3]\n\n"
	scene_text += "[ext_resource type=\"PackedScene\" path=\"" + CARD_BASES[side] + "\" id=\"1_base\"]\n"
	scene_text += "[ext_resource type=\"Script\" path=\"" + script_path + "\" id=\"2_script\"]\n"
	if has_unit:
		scene_text += "[ext_resource type=\"Resource\" path=\"" + data_path + "\" id=\"3_data\"]\n"
	scene_text += "\n[node name=\"" + object_name + "\" instance=ExtResource(\"1_base\")]\nscript = ExtResource(\"2_script\")\n"
	if has_unit:
		scene_text += data_prop + " = ExtResource(\"3_data\")\n"
	_write(script_path, script_text)
	_write(scene_path, scene_text)
	if has_unit:
		var data_text := FileAccess.get_file_as_string(data_path)
		data_text += "cardType = " + str(type_index) + "\n"
		_write(data_path, data_text)
	return {"ok": true}

func _sanitize(value: String) -> String:
	var result := ""
	for ch in value:
		if ch in "\\/:*?\"<>|\n\r\t":
			continue
		result += ch
	return result.strip_edges()

func _exists_any(paths: Array) -> bool:
	for path in paths:
		if DirAccess.dir_exists_absolute(ProjectSettings.globalize_path(path)) or FileAccess.file_exists(path):
			return true
	return false

func _ensure_dir(path: String) -> bool:
	return DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(path)) == OK

func _write(path: String, content: String) -> void:
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file:
		file.store_string(content)

func _refresh_filesystem() -> void:
	if editor_plugin:
		editor_plugin.get_editor_interface().get_resource_filesystem().scan()

func _show_error(message: String) -> void:
	status_label.text = message
	status_label.modulate = Color(1.0, 0.45, 0.35)

func _show_success(message: String) -> void:
	status_label.text = message
	status_label.modulate = Color(0.45, 1.0, 0.55)
