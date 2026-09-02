extends Node2D
@onready var sprite = $"图片"
@onready var label = $"图片/文字"
@export var team:PVZ.Type = PVZ.Type.PLANT
var spendEffect = load("res://场景/UI对象/花费特效.tscn")
func _ready() -> void:
	bindSignal()
	setup()
	scale = Vector2(1,1)
func setTexture():
	if team == PVZ.Type.PLANT:
		sprite.texture = load("res://素材/卡牌属性图片/inhnd_sun_gb.png")
	if team == PVZ.Type.ZOMBIE:
		sprite.texture = load("res://素材/卡牌属性图片/inhnd_brains.png")
	setPosition()
func updateCost():
	if team == PVZ.Type.PLANT:
		if label.text == str(TurnManager.plantCost):return
		label.text = str(TurnManager.plantCost)
	elif team == PVZ.Type.ZOMBIE:
		if label.text == str(TurnManager.zombieCost):return
		label.text = str(TurnManager.zombieCost)
	var tween = create_tween()
	tween.tween_property(sprite,"scale",Vector2(0.4,0.4),0.3)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN)
	await get_tree().create_timer(0.3).timeout
	
	tween = create_tween()
	tween.tween_property(sprite,"scale",Vector2(0.6,0.6),0.3)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.3).timeout
func bindSignal():
	TurnManager.playerCostUpdate.connect(updateCost)
	TurnManager.placeNewCard.connect(func(_type:PVZ.Type,_val:int):spendCost(_type,_val))
func spendCost(mteam:PVZ.Type,val:int):
	if team != mteam :return
	var waitTime:float = 0.5 / val
	for i in range(0,val):
		var spendInstance = spendEffect.instantiate()
		add_child(spendInstance)
		spendInstance.intro(team)
		await get_tree().create_timer(waitTime).timeout
func setPosition():
	if team == PVZ.Type.ZOMBIE && TurnManager.team == 1:
		position = Vector2(660,220)
	elif team == PVZ.Type.ZOMBIE && TurnManager.team == 2:
		position = Vector2(60,940)
	elif team == PVZ.Type.PLANT && TurnManager.team == 1:
		position = Vector2(60,940)
	elif team == PVZ.Type.PLANT && TurnManager.team == 2:
		position = Vector2(660,220)
func setup():
	setPosition()
	setTexture()
