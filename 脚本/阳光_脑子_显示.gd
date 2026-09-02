extends Node2D
@onready var label:Label = $"图片/文字"
@onready var sprite:Sprite2D = $"图片"
@onready var effect:Sprite2D = $"发光"
@onready var type:PVZ.Type = -1
@onready var tween1:Tween 
@onready var tween2:Tween 
@onready var tween3:Tween 
@onready var tween4:Tween 

func setType(type1:PVZ.Type):
	type = type1
	if type == PVZ.Type.PLANT:
		sprite.texture = load("res://素材/卡牌属性图片/inhnd_sun_gb.png")
	if type == PVZ.Type.ZOMBIE:
		effect.hide()
		sprite.texture = load("res://素材/卡牌属性图片/inhnd_brains.png")
func intro(val:int,mtype:PVZ.Type):
	label.text = str(val)
	tween1 = create_tween()
	if TurnManager.team == 1:
		if mtype == PVZ.Type.PLANT:
			position = Vector2(0,580)
			tween1.tween_property(self,"position",Vector2(280,580),0.3).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
		if mtype == PVZ.Type.ZOMBIE:
			position = Vector2(720,580)
			tween1.tween_property(self,"position",Vector2(440,580),0.3).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	else:
		if mtype == PVZ.Type.PLANT:
			position = Vector2(720,580)
			tween1.tween_property(self,"position",Vector2(440,580),0.3).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
		if mtype == PVZ.Type.ZOMBIE:
			position = Vector2(0,580)
			tween1.tween_property(self,"position",Vector2(280,580),0.3).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	await get_tree().create_timer(0.15).timeout
	tween3 = create_tween()
	tween3.tween_property(self,"scale",Vector2(0.4,0.8),0.1)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.1).timeout
	tween3 = create_tween()
	tween3.tween_property(self,"scale",Vector2(0.65,0.5),0.2)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.2).timeout
	tween3 = create_tween()
	tween3.tween_property(self,"scale",Vector2(0.6,0.6),0.1)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.3).timeout
	tween1 = create_tween()
	if TurnManager.team == 1:
		if mtype == PVZ.Type.PLANT:
			tween1.tween_property(self,"position",Vector2(60,940),0.5).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
		if mtype == PVZ.Type.ZOMBIE:
			tween1.tween_property(self,"position",Vector2(660,220),0.5).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	else:
		if mtype == PVZ.Type.PLANT:
			tween1.tween_property(self,"position",Vector2(660,220),0.5).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
		if mtype == PVZ.Type.ZOMBIE:
			tween1.tween_property(self,"position",Vector2(60,940),0.5).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.5).timeout
	tween1 = create_tween()
	tween1.tween_property(sprite,"scale",Vector2(0,0),0.4)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN)
	tween2 = create_tween()
	tween2.tween_property(effect,"scale",Vector2(0.3,0.3),0.4)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.4).timeout
	tween1 = create_tween()
	tween1.tween_property(effect,"scale",Vector2(1,1),0.3)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
	tween2 = create_tween()
	tween2.tween_property(effect,"modulate",Color(1,1,1,0),0.6)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.4).timeout
	queue_free()
