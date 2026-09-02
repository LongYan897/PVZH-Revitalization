extends Node2D
@onready var lebel = $"文字"
func intro():
	position = Vector2(0,0)
	var tween = create_tween()
	tween.tween_property(self,"position",Vector2(0,-300),0.5)\
	.set_trans(Tween.TRANS_BACK)\
	.set_ease(Tween.EASE_OUT)
	await get_tree().create_timer(0.5).timeout
	tween = create_tween()
	tween.tween_property(self,"modulate",Color(1,1,1,0),0.3)\
	.set_trans(Tween.TRANS_SINE)\
	.set_ease(Tween.EASE_OUT)
	await get_tree().create_timer(0.3).timeout
	queue_free()
func _ready() -> void:
	intro()
func setDamage(val:int):
	lebel.text = "-"+str(val)
