extends Node2D
@onready var sprite = $"图片"
@onready var state:SpineAnimationState = sprite.get_animation_state()
func _ready() -> void:
	position = Vector2(0,0)
	intro()
func intro():
	state.set_animation("intro",false,0)
func clear():
	state.set_animation("clear",false,0)
	await sprite.animation_completed
	queue_free()
