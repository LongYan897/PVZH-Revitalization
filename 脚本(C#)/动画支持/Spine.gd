extends SpineSprite

signal spine_anim_event(event_obj)

func set_animation(track:int, anim_name:String, loop:bool) -> void:
	get_animation_state().set_animation(anim_name, loop, track)

func add_animation(track:int, anim_name:String, loop:bool, delay:float=0.0) -> void:
	get_animation_state().add_animation(anim_name, delay, loop ,track)

func set_skin(skin_name:String) -> void:
	get_skeleton().set_skin_by_name(skin_name)
	get_skeleton().set_bones_to_setup_pose()

func get_bone_world_pos(bone_name:String) -> Vector2:
	return get_global_bone_transform(bone_name).origin

func _on_animation_event(evt):
	emit_signal("spine_anim_event", evt)

func _ready():
	connect("animation_event", _on_animation_event)
