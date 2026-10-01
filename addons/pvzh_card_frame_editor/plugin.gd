@tool
extends EditorPlugin

const DockScript := preload("res://addons/pvzh_card_frame_editor/pvzh_card_frame_editor_dock.gd")

var dock: Control

func _enter_tree() -> void:
	dock = DockScript.new()
	dock.name = "卡面"
	dock.editor_plugin = self
	add_control_to_dock(DOCK_SLOT_RIGHT_UL, dock)

func _exit_tree() -> void:
	if is_instance_valid(dock):
		remove_control_from_docks(dock)
		dock.queue_free()
