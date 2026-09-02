extends Node2D
@onready var spine:SpineSprite = $"图片"
@onready var state:SpineAnimationState = spine.get_animation_state()
func intro():
	show()
	modulate = Color(1,1,1,1)
	state.set_animation("intro",false,0)
	await spine.animation_completed
func _ready() -> void:
	scale = Vector2(4,4)
	modulate = Color(1,1,1,0)
