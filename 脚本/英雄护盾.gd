extends Node2D

@export var type:PVZ.Type = PVZ.Type.PLANT
var team:int = 1

@onready var shield_visual: Sprite2D = $"护盾"
@onready var shield = $"护盾"
@onready var shieldEffect:SpineSprite = $"打击特效"
@onready var shieldEffectState:SpineAnimationState = shieldEffect.get_animation_state()
@onready var heroShield:SpineSprite = $"护盾特效"
@onready var heroShieldState:SpineAnimationState = heroShield.get_animation_state()
@onready var wheel:SpineSprite = $"轮盘"
@onready var wheelState:SpineAnimationState = wheel.get_animation_state()
@onready var box = $"框"
@onready var underGround = $"底层"
@onready var label:Label = $"血量/伤害数值"
@onready var shieldNode:Array =[$"小护盾1",$"小护盾2",$"小护盾3"]
func _ready() -> void:
	setup()
	await get_tree().process_frame
	heroShield.reparent(heroShield.get_parent().get_parent())
	#hitEffect(1)
func setType():
	if type == PVZ.Type.PLANT:
		TurnManager.plantShield = self
		heroShield.modulate = Color(0.496, 0.9, 0.279, 1.0)
	elif type == PVZ.Type.ZOMBIE:
		TurnManager.zombieShield = self
		heroShield.modulate = Color(0.723, 0.07, 0.87, 1.0)
		box.modulate = Color(0.927, 0.703, 0.994, 1.0)
		underGround.modulate = Color(0.927, 0.703, 0.994, 1.0)
		for i in shieldNode:
			i.texture = load("res://素材/ui/护盾/pip_zombie.png")
func setTeam():
	if TurnManager.team == 1 && type == PVZ.Type.PLANT:
		team = 1
	elif TurnManager.team == 2 && type == PVZ.Type.PLANT:
		team = 2
	elif TurnManager.team == 1 && type == PVZ.Type.ZOMBIE:
		team = 2
	elif TurnManager.team == 2 && type == PVZ.Type.ZOMBIE:
		team = 1
	if team == 1:
		position = Vector2(170,930)
	elif team == 2:
		position = Vector2(550,220)
func fillShield(val:int):
	var pval:float
	match val:
		0: pval = 0 / 360.0
		1: pval = 45 / 360.0
		2: pval = 90 / 360.0
		3: pval = 135 / 360.0
		4: pval = 180 / 360.0
		5: pval = 225 / 360.0
		6: pval = 270 / 360.0
		7: pval = 315 / 360.0
		8: pval = 1.0
	var nTime:float = 0
	var forVal:float = shield.material.get_shader_parameter("progress")
	while nTime<0.75:
		nTime += get_process_delta_time()
		shield.material.set_shader_parameter("progress",forVal+(pval-forVal)*Calc.aease((nTime/0.75),"QUAD",3))
		await get_tree().process_frame
func hitEffect(val:int):
	var len = AQ.getlen()
	var fun = func():
		fillShield(8)
		stretch()
		await get_tree().create_timer(0.25).timeout
		shieldEffect.modulate = Color(1,1,1,1)
		wheelState.set_animation("shieldIntro",false,0)
		shield.modulate = Color(1,1,1,1)
		shieldEffect.position = Vector2(0,0)
		shieldEffectState.set_animation("effect",false,0)
		await get_tree().create_timer(0.14).timeout
		var tween = create_tween()
		if team == 1:
			tween.tween_property(shieldEffect,"position",Vector2(400,-150),0.14)\
			.set_trans(Tween.TRANS_QUAD)\
			.set_ease(Tween.EASE_IN_OUT)
		else:
			shieldEffect.scale = Vector2(-1,1)
			tween.tween_property(shieldEffect,"position",Vector2(-400,-150),0.14)\
			.set_trans(Tween.TRANS_QUAD)\
			.set_ease(Tween.EASE_IN_OUT)
		await get_tree().create_timer(0.14).timeout
		if type == 1:
			heroShieldState.set_animation("plantshield",false,0)
		else:
			heroShieldState.set_animation("zombieshield",false,0)
		if position.y<640:
			heroShield.global_position = Vector2(360,200)
		else:
			heroShield.global_position = Vector2(360,900)
		await get_tree().create_timer(0.6).timeout
		fillShield(0)
		if type == 1:
			shieldEffect.modulate = Color(0.496, 0.9, 0.279, 1.0)
		else:
			shieldEffect.modulate = Color(0.723, 0.07, 0.87, 1.0)
		if position.y<640:
			shieldEffect.global_position = Vector2(360,200)
		else:
			shieldEffect.global_position = Vector2(360,900)
		shieldEffectState.set_animation("cardIntro",false,0)
		await get_tree().create_timer(0.07).timeout
		shieldEffect.modulate = Color(1.0, 1.0, 1.0, 1.0)
	AQ.addAction(fun)
	
func stretch():
	scale = Vector2(0.5,0.5)
	var tween = create_tween()
	tween.tween_property(self,"scale",Vector2(0.4,0.6),0.14).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.14).timeout
	tween = create_tween()
	tween.tween_property(self,"scale",Vector2(0.55,0.45),0.14).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.14).timeout
	tween = create_tween()
	tween.tween_property(self,"scale",Vector2(0.5,0.5),0.14).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	
	await get_tree().create_timer(0.8).timeout
	
	tween = create_tween()
	tween.tween_property(self,"scale",Vector2(0.4,0.6),0.14).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.14).timeout
	tween = create_tween()
	tween.tween_property(self,"scale",Vector2(0.55,0.45),0.14).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.14).timeout
	tween = create_tween()
	tween.tween_property(self,"scale",Vector2(0.5,0.5),0.14).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
func hitNormalEffect(val:int,difval:int,hp:int):
	updateHealth(hp)
	hitHealthColor()
	stretch()
	await get_tree().create_timer(0.25).timeout
	shieldEffect.modulate = Color(1,1,1,1)
	setShieldSlot(difval)
	wheelState.set_animation("shieldIntro",false,0)
	await get_tree().create_timer(0.75).timeout
	fillShield(val)
func setup():
	shieldEffect.position = Vector2(999,999)
	heroShield.position = Vector2(999,999)
	make_shader_unique()
	setType()
	setTeam()
func setShieldSlot(val:int):
	var skeleton = wheel.get_skeleton()
	if type == PVZ.Type.PLANT:
		if val == 1:
			skeleton.set_attachment("护盾槽位","p1")
		elif val == 2:
			skeleton.set_attachment("护盾槽位","p2")
		elif val == 3:
			skeleton.set_attachment("护盾槽位","p3")
	else:
		if val == 1:
			skeleton.set_attachment("护盾槽位","z1")
		elif val == 2:
			skeleton.set_attachment("护盾槽位","z2")
		elif val == 3:
			skeleton.set_attachment("护盾槽位","z3")
func make_shader_unique():
	if shield_visual.material == null:
		return
	shield_visual.material = shield_visual.material.duplicate()
func updateHealth(val:int):
	label.text = str(val)
func setShieldType(val:int):
	var removeNode
	if val == 3:
		removeNode = shieldNode[0]
		box.texture = load("res://素材/ui/护盾/shieldframe_break1.png")
	elif val == 2:
		removeNode = shieldNode[1]
		box.texture = load("res://素材/ui/护盾/shieldframe_break2.png")
	elif val == 1:
		await get_tree().create_timer(2).timeout
		removeNode = shieldNode[2]
		var tween = create_tween()
		tween.tween_property(box,"scale",Vector2(0,0),0.7).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		var tween1 = create_tween()
		tween1.tween_property(underGround,"scale",Vector2(0,0),0.7).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	var tween = create_tween()
	tween.tween_property(removeNode,"scale",Vector2(0,0),0.3).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
func healthStretch():
	scale = Vector2(0.5,0.5)
	var tween = create_tween()
	tween.tween_property(self,"scale",Vector2(0.4,0.6),0.14).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.14).timeout
	tween = create_tween()
	tween.tween_property(self,"scale",Vector2(0.55,0.45),0.14).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.14).timeout
	tween = create_tween()
	tween.tween_property(self,"scale",Vector2(0.5,0.5),0.14).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
func hitHealthColor():
	var tween = create_tween()
	tween.tween_property(label,"modulate",Color(1.0, 0.19, 0.19, 1.0),0.14).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.14).timeout
	tween = create_tween()
	tween.tween_property(label,"modulate",Color(1.0, 1.0, 1.0, 1.0),0.14).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
