@tool
extends RefCounted

const CONFIG_PATH := "res://addons/pvzh_card_creator/creator_config.json"

const DEFAULT_HP_TYPES := ["普通"]
const DEFAULT_ATK_TYPES := ["普通", "追踪", "双重打击"]
const DEFAULT_STAR_TYPES := ["星星"]
const DEFAULT_TAGS := [
	{"label": "组队", "side": "植物"},
	{"label": "两栖", "side": "全部"},
	{"label": "墓碑", "side": "僵尸"}
]

static var data: Dictionary = {}
static var loaded := false

static func _default() -> Dictionary:
	return {
		"hp_types": DEFAULT_HP_TYPES.duplicate(),
		"atk_types": DEFAULT_ATK_TYPES.duplicate(),
		"star_types": DEFAULT_STAR_TYPES.duplicate(),
		"bool_props": [],
		"tags": DEFAULT_TAGS.duplicate(true)
	}

static func ensure_loaded() -> void:
	if loaded:
		return
	loaded = true
	data = _default()
	if not FileAccess.file_exists(CONFIG_PATH):
		return
	var f := FileAccess.open(CONFIG_PATH, FileAccess.READ)
	if f == null:
		return
	var parsed = JSON.parse_string(f.get_as_text())
	f.close()
	if not (parsed is Dictionary):
		return
	if parsed.has("hp_types") and parsed["hp_types"] is Array:
		data["hp_types"] = _to_str_array(parsed["hp_types"])
	if parsed.has("atk_types") and parsed["atk_types"] is Array:
		data["atk_types"] = _to_str_array(parsed["atk_types"])
	if parsed.has("star_types") and parsed["star_types"] is Array:
		data["star_types"] = _to_str_array(parsed["star_types"])
	if parsed.has("bool_props") and parsed["bool_props"] is Array:
		var arr := []
		for item in parsed["bool_props"]:
			if item is Dictionary:
				arr.append({
					"key": str(item.get("key", "")),
					"label": str(item.get("label", "")),
					"side": str(item.get("side", "全部")),
					"card_type": str(item.get("card_type", "全部"))
				})
		data["bool_props"] = arr
	if parsed.has("tags") and parsed["tags"] is Array:
		var tarr := []
		for item in parsed["tags"]:
			if item is Dictionary:
				tarr.append({
					"label": str(item.get("label", "")),
					"side": str(item.get("side", "全部"))
				})
			elif item is String:
				tarr.append({"label": str(item), "side": "全部"})
		data["tags"] = tarr

static func _to_str_array(arr: Array) -> Array:
	var out := []
	for v in arr:
		out.append(str(v))
	return out

static func save() -> void:
	ensure_loaded()
	var dir := CONFIG_PATH.get_base_dir()
	if not DirAccess.dir_exists_absolute(dir):
		DirAccess.make_dir_recursive_absolute(dir)
	var f := FileAccess.open(CONFIG_PATH, FileAccess.WRITE)
	if f == null:
		push_error("[PVZH] 无法写入配置文件：" + CONFIG_PATH)
		return
	f.store_string(JSON.stringify(data, "\t"))
	f.close()

static func get_hp_types() -> Array:
	ensure_loaded()
	return data["hp_types"]

static func get_atk_types() -> Array:
	ensure_loaded()
	return data["atk_types"]

static func get_star_types() -> Array:
	ensure_loaded()
	return data["star_types"]

static func get_bool_props() -> Array:
	ensure_loaded()
	return data["bool_props"]

static func add_bool_prop(key: String, label: String, side: String, card_type: String) -> void:
	ensure_loaded()
	for p in data["bool_props"]:
		if str(p.get("key", "")) == key:
			p["label"] = label
			p["side"] = side
			p["card_type"] = card_type
			return
	data["bool_props"].append({
		"key": key, "label": label, "side": side, "card_type": card_type
	})

static func remove_bool_prop(index: int) -> void:
	ensure_loaded()
	if index < 0 or index >= data["bool_props"].size():
		return
	data["bool_props"].remove_at(index)

static func get_tags() -> Array:
	ensure_loaded()
	return data["tags"]

static func add_tag(label: String, side: String) -> void:
	ensure_loaded()
	for t in data["tags"]:
		if str(t.get("label", "")) == label:
			t["side"] = side
			return
	data["tags"].append({"label": label, "side": side})

static func remove_tag(index: int) -> void:
	ensure_loaded()
	if index < 0 or index >= data["tags"].size():
		return
	data["tags"].remove_at(index)
