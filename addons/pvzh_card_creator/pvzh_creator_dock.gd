@tool
extends Control

var editor_plugin: EditorPlugin

var tabs: TabContainer
var creator_panel
var config_panel

const CreatorPanelScript := preload("res://addons/pvzh_card_creator/card_creator_panel.gd")
const ConfigPanelScript := preload("res://addons/pvzh_card_creator/extension_config_panel.gd")

func _ready() -> void:
	size_flags_horizontal = Control.SIZE_EXPAND_FILL
	size_flags_vertical = Control.SIZE_EXPAND_FILL

	tabs = TabContainer.new()
	tabs.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	tabs.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	tabs.size_flags_vertical = Control.SIZE_EXPAND_FILL
	add_child(tabs)

	creator_panel = CreatorPanelScript.new()
	creator_panel.name = "创建卡牌"
	creator_panel.editor_plugin = editor_plugin
	tabs.add_child(creator_panel)

	config_panel = ConfigPanelScript.new()
	config_panel.name = "扩展配置"
	tabs.add_child(config_panel)

	tabs.tab_changed.connect(_on_tab_changed)

func _on_tab_changed(_idx: int) -> void:
	if is_instance_valid(creator_panel) and creator_panel.has_method("refresh_from_config"):
		creator_panel.refresh_from_config()
