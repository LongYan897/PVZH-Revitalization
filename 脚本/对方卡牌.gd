extends Node2D
class_name EnemyCard
@onready var sprite = $"图片"
var plantInstance:PlantInstance
var zombieInstance:ZombieInstance
var number:int = 0
var type:CardManager.CardType = CardManager.CardType.NULL
var tween:Tween
func _ready() -> void:
	setup()
	intro()
	bindSignal()
	#await get_tree().create_timer(1).timeout
	#updatePos()
func bindSignal():
	Enemy.CardUpdatePos.connect(updatePos)
func setup():
	if TurnManager.team == 1:
		sprite.texture = load("res://素材/牌背、卡面/cardback_zombies #143926.png")
	else:
		sprite.texture = load("res://素材/牌背、卡面/cardback_plants #176244.png")
	# 两个阵营都必须登记对方手牌，供网络 CHOOSE_CARD 消息按数据定位。
	Enemy.addList(self)
func updatePos():
	if type != CardManager.CardType.NULL:return
	var pos:Vector2 = Vector2(100+30*number,100)
	var tween = create_tween()
	tween.tween_property(self,"position",pos,0.4)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
func intro():
	scale = Vector2(0,0)
	var tween = create_tween()
	tween.tween_property(self,"scale",Vector2(0.55,0.55),0.4)\
	.set_trans(Tween.TRANS_BACK)\
	.set_ease(Tween.EASE_OUT)
func setNum(val:int):
	number = val
	z_index = 100 - val
func animation(pos:Vector2 = Vector2(360,600)):
	Enemy.removeList(self)
	Enemy.updateListNum()
	Enemy.CardUpdatePos.emit()
	tween = create_tween()
	tween.tween_property(self,"position",pos,0.4)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
	await tween.finished
	tween = create_tween()
	tween.tween_property(self,"scale",Vector2.ZERO,0.2)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
func getData():
	if plantInstance:return plantInstance
	if zombieInstance:return zombieInstance
	return null
func animationPlace(line):
	if tween.is_running():tween.kill()
	scale = Vector2(0.55,0.55)
	type = CardManager.CardType.ANIMATION
	position = Vector2(360,600)
	await get_tree().create_timer(0.5).timeout
	var tween = create_tween()
	tween.tween_property(self,"position",Vector2(RoadList.getLinePosX(line),400),0.3)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
	await tween.finished
	tween = create_tween()
	tween.tween_property(self,"scale",Vector2(0,0),0.3)\
	.set_trans(Tween.TRANS_BACK)\
	.set_ease(Tween.EASE_IN)
