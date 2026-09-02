extends Node2D


func _ready() -> void:
	intro()

func intro():
	modulate = Color(0,0,0,0)
	rotation = 0
	scale = Vector2(1,1)
	var tween = create_tween() 
	var tween1 = create_tween() 
	tween.tween_property(self,"modulate",Color(1,1,1,1),0.15)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_OUT)
	tween1.tween_property(self,"rotation",TAU,0.2)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN)
	await tween1.finished
	tween1 = create_tween() 
	tween1.tween_property(self,"rotation",1.5*TAU,0.2)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_OUT)
	var tween2 = create_tween() 
	tween2.tween_property(self,"scale",Vector2(0.2,0.2),0.3)\
	.set_trans(Tween.TRANS_BACK)\
	.set_ease(Tween.EASE_IN)
	await get_tree().create_timer(0.15).timeout
	tween = create_tween() 
	tween.tween_property(self,"modulate",Color(0,0,0,0),0.2)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_OUT)
	await tween.finished 
	queue_free()
