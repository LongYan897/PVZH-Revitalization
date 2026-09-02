extends Node2D
@onready var sprite = $"图片"
func intro(type:PVZ.Type):
	position = Vector2(0,0)
	if type == PVZ.Type.PLANT:
		sprite.texture = load("res://素材/卡牌属性图片/inhnd_sun_gb.png")
	else:
		sprite.texture = load("res://素材/卡牌属性图片/inhnd_brains.png")
	var tween = create_tween()
	tween.tween_property(self,"position",Vector2(0,-100),0.3)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN)
	var tween1 = create_tween()
	tween1.tween_property(self,"modulate",Color(1,1,1,0),0.3)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN)
	await get_tree().create_timer(1).timeout
	queue_free()
