extends Node2D
var tween:Tween
func _ready() -> void:
	intro()
func intro():
	tween = create_tween()
	modulate = Color(1,1,1,0)
	tween.tween_property(self,"modulate",Color(1,1,1,0.8),0.25)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_OUT)
func clear():
	tween = create_tween()
	modulate = Color(1,1,1,0.8)
	tween.tween_property(self,"modulate",Color(1,1,1,0),0.25)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_OUT)
	await tween.finished
	queue_free()
func setRoad(line:int):
	top_level = true
	global_position = Vector2(140+line*107,600)
