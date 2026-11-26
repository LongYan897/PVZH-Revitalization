@tool
extends VBoxContainer

const CARDS_JSON_PATH := "res://数据(C#)/cards.json"
const CS_SCRIPT_ROOT := "res://脚本(C#)/卡牌/实例卡牌/"
const WindowScript := preload("res://addons/pvzh_card_creator/card_frame_editor_window.gd")

const PREVIEW_SIZE := Vector2(180, 180)
const PREVIEW_MARGIN := 4.0

var tree: Tree
var search_edit: LineEdit
var preview_inner: Control
var detail_edit: TextEdit
var status_label: Label

var open_godot_btn: Button
var open_ext_btn: Button
var copy_path_btn: Button
var edit_frame_btn: Button
var save_frame_btn: Button
var cancel_frame_btn: Button

var _entries: Array = []
var _selected_class: String = ""
var _selected_icon_path: String = ""
var _cs_path_cache: Dictionary = {}
var _frame_cache: Dictionary = {}

var scene_editor_window: Window

func _ready() -> void:
	size_flags_horizontal = Control.SIZE_EXPAND_FILL
	size_flags_vertical = Control.SIZE_EXPAND_FILL
	add_theme_constant_override("separation", 6)

	var title := Label.new()
	title.text = "卡牌列表"
	title.add_theme_font_size_override("font_size", 18)
	add_child(title)

	var toolbar := HBoxContainer.new()
	toolbar.add_theme_constant_override("separation", 4)
	add_child(toolbar)

	var refresh_btn := Button.new()
	refresh_btn.text = "刷新"
	refresh_btn.pressed.connect(_reload)
	toolbar.add_child(refresh_btn)

	search_edit = LineEdit.new()
	search_edit.placeholder_text = "按类名或标题过滤"
	search_edit.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	search_edit.text_changed.connect(func(_t): _apply_filter())
	toolbar.add_child(search_edit)

	var split := HSplitContainer.new()
	split.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	split.size_flags_vertical = Control.SIZE_EXPAND_FILL
	add_child(split)

	var left := VBoxContainer.new()
	left.custom_minimum_size = Vector2(200, 0)
	split.add_child(left)

	tree = Tree.new()
	tree.columns = 4
	tree.set_column_title(0, "类名")
	tree.set_column_title(1, "标题")
	tree.set_column_title(2, "阵营")
	tree.set_column_title(3, "稀有度")
	tree.set_column_expand(0, true)
	tree.set_column_expand(1, true)
	tree.set_column_expand(2, false)
	tree.set_column_expand(3, false)
	tree.set_column_custom_minimum_width(2, 44)
	tree.set_column_custom_minimum_width(3, 56)
	tree.hide_root = true
	tree.size_flags_vertical = Control.SIZE_EXPAND_FILL
	tree.item_selected.connect(_on_tree_selected)
	left.add_child(tree)

	var right := VBoxContainer.new()
	right.custom_minimum_size = Vector2(220, 0)
	right.add_theme_constant_override("separation", 4)
	split.add_child(right)

	var preview_wrap := CenterContainer.new()
	preview_wrap.custom_minimum_size = PREVIEW_SIZE
	right.add_child(preview_wrap)

	var preview_clip := Control.new()
	preview_clip.custom_minimum_size = PREVIEW_SIZE
	preview_clip.clip_contents = true
	preview_wrap.add_child(preview_clip)

	preview_inner = Control.new()
	preview_inner.mouse_filter = Control.MOUSE_FILTER_IGNORE
	preview_inner.position = Vector2.ZERO
	preview_inner.scale = Vector2.ONE
	preview_clip.add_child(preview_inner)

	var frame_btn_row := HBoxContainer.new()
	frame_btn_row.add_theme_constant_override("separation", 4)
	right.add_child(frame_btn_row)

	edit_frame_btn = Button.new()
	edit_frame_btn.text = "调整"
	edit_frame_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	edit_frame_btn.disabled = true
	edit_frame_btn.pressed.connect(_on_edit_frame)
	frame_btn_row.add_child(edit_frame_btn)

	save_frame_btn = Button.new()
	save_frame_btn.text = "保存"
	save_frame_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	save_frame_btn.visible = false
	save_frame_btn.pressed.connect(_on_save_frame)
	frame_btn_row.add_child(save_frame_btn)

	cancel_frame_btn = Button.new()
	cancel_frame_btn.text = "取消"
	cancel_frame_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	cancel_frame_btn.visible = false
	cancel_frame_btn.pressed.connect(_on_cancel_frame)
	frame_btn_row.add_child(cancel_frame_btn)

	var action_row := HBoxContainer.new()
	action_row.add_theme_constant_override("separation", 4)
	right.add_child(action_row)

	open_godot_btn = Button.new()
	open_godot_btn.text = "Godot"
	open_godot_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	open_godot_btn.disabled = true
	open_godot_btn.pressed.connect(_on_open_godot)
	action_row.add_child(open_godot_btn)

	open_ext_btn = Button.new()
	open_ext_btn.text = "外部"
	open_ext_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	open_ext_btn.disabled = true
	open_ext_btn.pressed.connect(_on_open_external)
	action_row.add_child(open_ext_btn)

	copy_path_btn = Button.new()
	copy_path_btn.text = "复制"
	copy_path_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	copy_path_btn.disabled = true
	copy_path_btn.pressed.connect(_on_copy_path)
	action_row.add_child(copy_path_btn)

	var detail_scroll := ScrollContainer.new()
	detail_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	detail_scroll.custom_minimum_size = Vector2(0, 100)
	right.add_child(detail_scroll)

	detail_edit = TextEdit.new()
	detail_edit.editable = false
	detail_edit.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	detail_edit.size_flags_vertical = Control.SIZE_EXPAND_FILL
	detail_scroll.add_child(detail_edit)

	status_label = Label.new()
	status_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	status_label.add_theme_font_size_override("font_size", 11)
	add_child(status_label)

	_reload()

func _reload() -> void:
	_entries.clear()
	_cs_path_cache.clear()
	_frame_cache.clear()
	_scan_cs_files(CS_SCRIPT_ROOT, _cs_path_cache)

	var data := _load_json()
	var groups := {}
	for k in data.keys():
		var key := str(k)
		var dot := key.find(".")
		if dot <= 0:
			continue
		var cls := key.substr(0, dot)
		var field := key.substr(dot + 1)
		if not groups.has(cls):
			groups[cls] = {}
		groups[cls][field] = data[k]

	for cls in groups:
		_entries.append({
			"class_name": cls,
			"data": groups[cls],
			"cs_path": str(_cs_path_cache.get(cls, ""))
		})

	_entries.sort_custom(func(a, b): return str(a["class_name"]) < str(b["class_name"]))
	_apply_filter()
	_set_status("已加载 %d 张卡牌" % _entries.size(), false)

func _scan_cs_files(dir_path: String, out_cache: Dictionary) -> void:
	var dir := DirAccess.open(dir_path)
	if dir == null:
		return
	dir.list_dir_begin()
	var fname := dir.get_next()
	while fname != "":
		if dir.current_is_dir():
			if fname != "." and fname != "..":
				_scan_cs_files(dir_path.path_join(fname), out_cache)
		elif fname.ends_with(".cs"):
			var cls := fname.get_basename()
			out_cache[cls] = dir_path.path_join(fname)
		fname = dir.get_next()
	dir.list_dir_end()

func _load_json() -> Dictionary:
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

func _apply_filter() -> void:
	tree.clear()
	var root_item := tree.create_item()
	var q := search_edit.text.strip_edges().to_lower()
	for entry in _entries:
		var cls := str(entry["class_name"])
		var data: Dictionary = entry["data"]
		var title_text := str(data.get("Title", ""))
		if not q.is_empty():
			if not cls.to_lower().contains(q) and not title_text.to_lower().contains(q):
				continue
		var item := tree.create_item(root_item)
		item.set_text(0, cls)
		item.set_text(1, title_text)
		item.set_text(2, str(data.get("Camp", "")))
		item.set_text(3, str(data.get("Rarity", "")))
		item.set_metadata(0, cls)
	_selected_class = ""
	_selected_icon_path = ""
	_clear_preview()
	_clear_detail()
	_set_actions_enabled(false)
	_hide_editor_window()
	_show_save_buttons(false)

func _on_tree_selected() -> void:
	var item := tree.get_selected()
	if item == null:
		return
	var cls := str(item.get_metadata(0))
	if cls == _selected_class:
		return
	_hide_editor_window()
	_show_save_buttons(false)

	_selected_class = cls
	for entry in _entries:
		if str(entry["class_name"]) == cls:
			_show_detail(entry)
			_refresh_preview(entry)
			_set_actions_enabled(true)
			var icon_path := str(entry["data"].get("Icon", ""))
			edit_frame_btn.disabled = not _is_editable_icon(icon_path)
			return

func _is_editable_icon(icon_path: String) -> bool:
	if icon_path.is_empty():
		return false
	if icon_path.get_extension().to_lower() != "tscn":
		return false
	return _scene_file_ok(icon_path)

func _scene_file_ok(scene_path: String) -> bool:
	if scene_path.is_empty():
		return false
	if FileAccess.file_exists(scene_path):
		return true
	return ResourceLoader.exists(scene_path)

func _show_detail(entry: Dictionary) -> void:
	var data: Dictionary = entry["data"]
	var cs_path := str(entry["cs_path"])

	var lines: Array = []
	lines.append("类名: %s" % entry["class_name"])
	lines.append("C#: %s" % (cs_path if not cs_path.is_empty() else "未找到"))
	lines.append("---")
	var keys := data.keys()
	keys.sort()
	for k in keys:
		var v = data[k]
		if v is Array:
			lines.append("%s: %s" % [k, JSON.stringify(v)])
		elif v is bool:
			lines.append("%s: %s" % [k, "true" if v else "false"])
		else:
			lines.append("%s: %s" % [k, str(v)])
	detail_edit.text = "\n".join(lines)

func _clear_detail() -> void:
	detail_edit.text = ""

func _clear_preview() -> void:
	for c in preview_inner.get_children():
		preview_inner.remove_child(c)
		c.queue_free()
	preview_inner.scale = Vector2.ONE
	preview_inner.position = Vector2.ZERO

func _refresh_preview(entry: Dictionary) -> void:
	_clear_preview()
	var data: Dictionary = entry["data"]
	_selected_icon_path = str(data.get("Icon", ""))

	var icon_ok := _scene_file_ok(_selected_icon_path)
	var icon_info := _load_icon_info(_selected_icon_path) if icon_ok else {}
	var type_text := str(data.get("Type", "单位"))
	var rarity := str(data.get("Rarity", "普通"))
	var is_fighter := type_text.begins_with("单位")

	var frame_tex := _frame_texture_for(type_text, rarity) if icon_ok else null
	var icon_tex: Texture2D = null
	var icon_pos := Vector2.ZERO
	var icon_size := Vector2(128, 128)
	var icon_scale := Vector2.ONE
	if not icon_info.is_empty():
		icon_tex = icon_info.get("texture", null)
		icon_pos = icon_info.get("position", Vector2.ZERO)
		icon_size = icon_info.get("size", Vector2(128, 128))
		icon_scale = icon_info.get("scale", Vector2.ONE)

	var frame_pos := Vector2(128, 96.5)
	var frame_scale := Vector2.ONE
	var frame_tex_size := frame_tex.get_size() if frame_tex != null else Vector2.ZERO

	var bounds := Rect2()
	var has_bounds := false

	if frame_tex != null and frame_tex_size.x > 0 and frame_tex_size.y > 0:
		var draw_pos: Vector2 = frame_pos - frame_tex_size / 2.0
		bounds = Rect2(draw_pos, frame_tex_size)
		has_bounds = true

	if icon_tex != null:
		var tex_size := icon_tex.get_size()
		if tex_size.x > 0 and tex_size.y > 0:
			var ratio := 1.0
			if icon_size.x > 0 and icon_size.y > 0:
				ratio = min(icon_size.x / tex_size.x, icon_size.y / tex_size.y)
			var new_size: Vector2 = tex_size * ratio
			var draw_pos: Vector2 = icon_pos + (icon_size - new_size) / 2.0
			var r := Rect2(draw_pos, new_size * icon_scale)
			bounds = r if not has_bounds else bounds.merge(r)
			has_bounds = true

	if not has_bounds:
		if _selected_icon_path.is_empty():
			_set_status("该卡无 Icon 字段，无法预览卡面", true)
		elif not icon_ok:
			_set_status("Icon 文件不存在：%s" % _selected_icon_path, true)
		else:
			_set_status("卡面内容为空", true)
		return

	var avail: Vector2 = PREVIEW_SIZE - Vector2(PREVIEW_MARGIN, PREVIEW_MARGIN) * 2.0
	var sx: float = avail.x / max(bounds.size.x, 1.0)
	var sy: float = avail.y / max(bounds.size.y, 1.0)
	var scale_factor: float = min(sx, sy)
	if scale_factor <= 0:
		scale_factor = 1.0

	preview_inner.scale = Vector2(scale_factor, scale_factor)
	preview_inner.position = Vector2(PREVIEW_MARGIN, PREVIEW_MARGIN) - bounds.position * scale_factor

	if frame_tex != null:
		var frame_rect := TextureRect.new()
		frame_rect.texture = frame_tex
		frame_rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		frame_rect.stretch_mode = TextureRect.STRETCH_KEEP_CENTERED
		frame_rect.position = frame_pos
		frame_rect.size = Vector2.ZERO
		frame_rect.scale = frame_scale
		frame_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
		frame_rect.z_index = 0 if is_fighter else 1
		preview_inner.add_child(frame_rect)

	if icon_tex != null:
		var icon_rect := TextureRect.new()
		icon_rect.texture = icon_tex
		icon_rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		icon_rect.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		icon_rect.position = icon_pos
		icon_rect.size = icon_size
		icon_rect.scale = icon_scale
		icon_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
		icon_rect.z_index = 1 if is_fighter else 0
		preview_inner.add_child(icon_rect)

	if _selected_icon_path.is_empty():
		_set_status("该卡无 Icon 字段，无法预览卡面", true)
	elif not icon_ok:
		_set_status("Icon 文件不存在：%s" % _selected_icon_path, true)

func _load_icon_info(icon_path: String) -> Dictionary:
	if not _scene_file_ok(icon_path):
		return {}

	var ext := icon_path.get_extension().to_lower()

	if ext != "tscn":
		var tex := load(icon_path) as Texture2D
		if tex == null:
			return {}
		return {
			"texture": tex,
			"position": Vector2.ZERO,
			"size": tex.get_size(),
			"scale": Vector2.ONE
		}

	var packed := ResourceLoader.load(icon_path, "PackedScene", ResourceLoader.CACHE_MODE_REPLACE) as PackedScene
	if packed == null:
		var fs := EditorInterface.get_resource_filesystem()
		if fs != null:
			fs.update_file(icon_path)
		packed = ResourceLoader.load(icon_path, "PackedScene", ResourceLoader.CACHE_MODE_REPLACE) as PackedScene
	if packed == null:
		return {}

	var inst := packed.instantiate()
	if inst == null:
		return {}

	var result := {}
	var icon_node: TextureRect = null
	for child in inst.get_children():
		if child is TextureRect:
			icon_node = child
			break

	if icon_node != null:
		result["texture"] = icon_node.texture
		result["position"] = icon_node.position
		var sz: Vector2 = icon_node.size
		if sz.x <= 0 or sz.y <= 0:
			if icon_node.texture != null:
				sz = icon_node.texture.get_size()
			else:
				sz = Vector2(128, 128)
		result["size"] = sz
		var sc: Vector2 = icon_node.scale
		if sc.x == 0 or sc.y == 0:
			sc = Vector2.ONE
		result["scale"] = sc

	inst.queue_free()
	return result

func _frame_texture_for(type_text: String, rarity: String) -> Texture2D:
	var key := "%s|%s" % [type_text, rarity]
	if _frame_cache.has(key):
		return _frame_cache[key]
	var base := "res://素材(C#)/卡牌属性/"
	var r := _rarity_key(rarity)
	var is_unit := type_text.begins_with("单位")
	var is_env := type_text.begins_with("环境")
	var is_trick := type_text.begins_with("锦囊")

	var path := ""
	if is_unit:
		match r:
			"Basic": path = base + "SEEDPACKET_FRONT.png"
			"Uncommon": path = base + "SEEDPACKET_R2.png"
			"Rare": path = base + "SEEDPACKET_R3.png"
			"SuperRare": path = base + "SEEDPACKET_R4.png"
			"Legend": path = base + "SEEDPACKET_R5.png"
			"Activity": path = base + "SEEDPACKET_Event.png"
	elif is_env:
		match r:
			"Basic": path = base + "Environment_R1.png"
			"Uncommon": path = base + "Environment_R2.png"
			"Rare": path = base + "Environment_R3.png"
			"SuperRare": path = base + "Environment_R4.png"
			"Legend": path = base + "Environment_R5.png"
			"Activity": path = base + "Environment_Event.png"
	elif is_trick:
		match r:
			"Basic": path = base + "SEEDPACKET_onetime_R1.png"
			"Uncommon": path = base + "SEEDPACKET_onetime_R2.png"
			"Rare": path = base + "SEEDPACKET_onetime_R3.png"
			"SuperRare": path = base + "SEEDPACKET_onetime_R4.png"
			"Legend": path = base + "SEEDPACKET_onetime_R5.png"
			"Activity": path = base + "SEEDPACKET_onetime_Event.png"

	var tex: Texture2D = null
	if not path.is_empty() and ResourceLoader.exists(path):
		tex = load(path)
	_frame_cache[key] = tex
	return tex

func _rarity_key(rarity: String) -> String:
	match rarity:
		"基础", "普通": return "Basic"
		"罕见": return "Uncommon"
		"稀有": return "Rare"
		"超稀有": return "SuperRare"
		"传说": return "Legend"
		"活动": return "Activity"
	return "Basic"

func _frame_position_for(type_text: String) -> Vector2:
	if type_text.begins_with("环境"):
		return Vector2(128.0, 92.0)
	return Vector2(128.0, 96.0)

func _frame_scale_for(type_text: String) -> Vector2:
	if type_text.begins_with("环境"):
		return Vector2(0.55, 0.55)
	return Vector2.ONE

func _set_actions_enabled(on: bool) -> void:
	open_godot_btn.disabled = not on
	open_ext_btn.disabled = not on
	copy_path_btn.disabled = not on
	if not on:
		edit_frame_btn.disabled = true

func _show_save_buttons(on: bool) -> void:
	edit_frame_btn.visible = not on
	save_frame_btn.visible = on
	cancel_frame_btn.visible = on

func _find_cs_path() -> String:
	if _selected_class.is_empty():
		return ""
	return str(_cs_path_cache.get(_selected_class, ""))

func _ensure_window() -> void:
	if scene_editor_window == null or not is_instance_valid(scene_editor_window):
		scene_editor_window = WindowScript.new()
		scene_editor_window.icon_changed.connect(func(): pass)
		scene_editor_window.close_requested.connect(func():
			scene_editor_window.hide()
			_show_save_buttons(false)
		)
		add_child(scene_editor_window)

func _hide_editor_window() -> void:
	if scene_editor_window != null and is_instance_valid(scene_editor_window):
		if scene_editor_window.visible:
			scene_editor_window.hide()

func _on_edit_frame() -> void:
	if _selected_class.is_empty():
		return
	var entry := _find_entry(_selected_class)
	if entry.is_empty():
		return
	var data: Dictionary = entry["data"]
	var icon_path := str(data.get("Icon", ""))
	if not _is_editable_icon(icon_path):
		_set_status("该卡的 Icon 不是可调整的场景", true)
		return

	var icon_info := _load_icon_info(icon_path)
	if icon_info.is_empty() or icon_info.get("texture", null) == null:
		_set_status("Icon 场景内未找到有效图标节点", true)
		return

	_ensure_window()
	scene_editor_window.set_frame_textures(
		null,
		_frame_texture_for(str(data.get("Type", "单位")), str(data.get("Rarity", "普通"))),
		_frame_position_for(str(data.get("Type", "单位"))),
		_frame_scale_for(str(data.get("Type", "单位")))
	)
	scene_editor_window.set_icon_texture(icon_info["texture"])
	if scene_editor_window.icon != null:
		scene_editor_window.icon.position = icon_info["position"]
		scene_editor_window.icon.size = icon_info["size"]
		scene_editor_window.icon.scale = icon_info["scale"]
		scene_editor_window._sync_spins_from_icon()
	scene_editor_window.popup_centered()
	_show_save_buttons(true)

func _on_save_frame() -> void:
	if scene_editor_window == null or not is_instance_valid(scene_editor_window):
		return
	if _selected_icon_path.is_empty():
		return

	var pos: Vector2 = scene_editor_window.get_icon_position()
	var size: Vector2 = scene_editor_window.get_icon_size()
	var scale: Vector2 = scene_editor_window.get_icon_scale()

	var ok := _rebuild_frame_scene(_selected_icon_path, pos, size, scale)
	scene_editor_window.hide()
	_show_save_buttons(false)

	if ok:
		var ep := editor_plugin()
		if ep != null:
			ep.get_editor_interface().get_resource_filesystem().scan()
		var entry := _find_entry(_selected_class)
		if not entry.is_empty():
			_refresh_preview(entry)
		_set_status("已保存卡面：%s" % _selected_icon_path, false)
	else:
		_set_status("保存失败：%s" % _selected_icon_path, true)

func _on_cancel_frame() -> void:
	_hide_editor_window()
	_show_save_buttons(false)

func _rebuild_frame_scene(scene_path: String, pos: Vector2, size: Vector2, scale: Vector2) -> bool:
	var icon_info := _load_icon_info(scene_path)
	if icon_info.is_empty():
		return false
	var tex: Texture2D = icon_info.get("texture", null)
	if tex == null:
		return false

	var root := Control.new()
	root.name = "卡面"
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE

	var icon := TextureRect.new()
	icon.name = "卡牌图标"
	icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	icon.stretch_mode = TextureRect.STRETCH_KEEP_CENTERED
	icon.position = pos
	icon.size = size
	icon.scale = scale
	icon.texture = tex
	root.add_child(icon)
	icon.owner = root

	var packed := PackedScene.new()
	packed.pack(root)
	var err := ResourceSaver.save(packed, scene_path)
	root.queue_free()
	return err == OK

func _find_entry(cls: String) -> Dictionary:
	for e in _entries:
		if str(e["class_name"]) == cls:
			return e
	return {}

func editor_plugin() -> EditorPlugin:
	var p := get_parent()
	while p != null:
		if p is EditorPlugin:
			return p
		if p.has_meta("editor_plugin"):
			return p.get_meta("editor_plugin")
		p = p.get_parent()
	return null

func _on_open_godot() -> void:
	var path := _find_cs_path()
	if path.is_empty():
		_set_status("未找到 C# 脚本", true)
		return
	var ep := editor_plugin()
	if ep != null:
		ep.get_editor_interface().get_resource_filesystem().update_file(path)
	var res = load(path)
	if res is Script:
		EditorInterface.edit_script(res)
		_set_status("已在 Godot 编辑器打开：%s" % path, false)
	else:
		_set_status("无法加载脚本：%s" % path, true)

func _on_open_external() -> void:
	var path := _find_cs_path()
	if path.is_empty():
		_set_status("未找到 C# 脚本", true)
		return
	var abs_path := ProjectSettings.globalize_path(path)

	var settings := EditorInterface.get_editor_settings()
	var exec_path := str(settings.get_setting("text_editor/external/exec_path"))
	var exec_flags := str(settings.get_setting("text_editor/external/exec_flags"))

	if exec_path.is_empty():
		OS.shell_open(abs_path)
		_set_status("已用系统默认程序打开：%s" % abs_path, false)
		return

	var project_dir := ProjectSettings.globalize_path("res://")
	var flags := exec_flags
	flags = flags.replace("{project}", project_dir)
	flags = flags.replace("{file}", abs_path)
	flags = flags.replace("{line}", "1")
	flags = flags.replace("{col}", "1")

	var args: PackedStringArray = []
	for part in flags.split(" ", false):
		var s := part.strip_edges()
		if not s.is_empty():
			args.append(s)

	var pid := OS.create_process(exec_path, args)
	if pid <= 0:
		_set_status("外部编辑器启动失败，改用系统默认程序", true)
		OS.shell_open(abs_path)
	else:
		_set_status("已在外部编辑器打开：%s" % abs_path, false)

func _on_copy_path() -> void:
	var path := _find_cs_path()
	if path.is_empty():
		return
	DisplayServer.clipboard_set(path)
	_set_status("已复制：%s" % path, false)

func _set_status(msg: String, is_error: bool) -> void:
	status_label.text = msg
	status_label.modulate = Color(1, 0.45, 0.35) if is_error else Color(0.45, 1, 0.55)
