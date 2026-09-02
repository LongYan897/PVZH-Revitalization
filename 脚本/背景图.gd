extends Node2D
@export var color:Color
@onready var sprite = $"图片"
func _ready() -> void:
	sprite.material.set_shader_parameter("tint_color",color)
func _process(delta: float) -> void:
	move(delta)
func move(delta):
		var curVal = sprite.material.get_shader_parameter("direction")
		curVal += Vector2(0.05, -0.05)*delta
		sprite.material.set_shader_parameter("direction", curVal)
