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

func _on_animation_event(_sprite, _anim_state, track_entry, evt):
	var anim = track_entry.get_animation()
	var duration = anim.get_duration()
	var track_time = track_entry.get_track_time()
	var time_scale = track_entry.get_time_scale()
	var remaining = (duration - track_time) / time_scale
	emit_signal("spine_anim_event", evt.get_data().get_event_name(), remaining)

func _ready():
	connect("animation_event", _on_animation_event)

func clear_track(track:int):
	get_animation_state().clear_track(track)

func clear_tracks():
	get_animation_state().clear_tracks()

func set_attachment(id:String,_name:String):
	get_skeleton().set_attachment(id,_name)

func get_animation_duration(anim_name:String) -> float:
	var anim = get_skeleton().get_data().find_animation(anim_name)
	if anim == null:
		return 0.0
	return anim.get_duration()

func pause_animation(track:int) -> void:
	var state = get_animation_state()
	var entry = state.get_current(track)
	if entry:
		entry.set_time_scale(0.0)
