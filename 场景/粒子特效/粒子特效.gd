extends Node2D
@onready var part = $"粒子"
func _ready() -> void:
	scale = Vector2(4,4)
	part.visibility_rect = Rect2(-4000, -4000, 8000, 8000)
