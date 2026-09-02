extends Node2D
@onready var at:SpineSprite = $"伤害"
@onready var hp:SpineSprite = $"血量"
@onready var lv:SpineSprite = $"等级"
@onready var atState:SpineAnimationState = at.get_animation_state()
@onready var hpState:SpineAnimationState = hp.get_animation_state()
@onready var lvState:SpineAnimationState = lv.get_animation_state()
@onready var atLabel:Label = $"伤害/伤害数值"
@onready var hpLabel:Label = $"血量/血量数值"
@onready var damageInstance = preload("res://场景/扣血特效/扣血特效.tscn")
func _ready() -> void:
	setup()
	intro()
func updatePlantData(data:PlantInstance,show:bool = false):
	atLabel.text = str(data.get_attack()) 
	hpLabel.text = str(data.get_health())
	updateColor(data.plantData.attack,data.get_attack(),data.plantData.health,data.get_health())
	if !show:return
	if atLabel.text == "0":
		at.hide()
	else:
		at.show()
		
	if hpLabel.text == "0":
		hp.hide()
	else:
		hp.show()
func updateZombieData(data:ZombieInstance,show:bool = false):
	atLabel.text = str(data.get_attack()) 
	hpLabel.text = str(data.get_health())
	updateColor(data.zombieData.attack,data.get_attack(),data.zombieData.health,data.get_health())
	if !show:return
	if atLabel.text == "0":
		at.hide()
	else:
		at.show()
		
	if hpLabel.text == "0":
		hp.hide()
	else:
		hp.show()
func label_intro(label:Label):
	var tween = create_tween()
	tween.tween_property(label, "scale", Vector2(1, 1), 1)\
	.set_trans(Tween.TRANS_BOUNCE)\
	.set_ease(Tween.EASE_OUT)
func setup():
	atLabel.pivot_offset = atLabel.size / 2
	hpLabel.pivot_offset = hpLabel.size / 2
	atLabel.scale = Vector2(0,0)
	hpLabel.scale = Vector2(0,0)
func hit(val:int):
	at.get_animation_state().set_animation("hitBase",false,0)
	hp.get_animation_state().set_animation("hitBase",false,0)
	lv.get_animation_state().set_animation("hitBase",false,0)
	setDamage(val)
	await followOffset(1)
func die():
	at.get_animation_state().set_animation("dieBase",false,0)
	hp.get_animation_state().set_animation("dieBase",false,0)
	lv.get_animation_state().set_animation("dieBase",false,0)
	labelDie()
	followOffset(1)
func followOffset(time:float=1):
	var offset1 
	var offset2
	var n_time:float = 0
	var worldOffset1 = at.get_global_bone_transform("root").origin
	var worldOffset2 = hp.get_global_bone_transform("root").origin
	while n_time <= time:
		offset1 = at.get_global_bone_transform("root").origin - worldOffset1
		offset2 = hp.get_global_bone_transform("root").origin - worldOffset2
		atLabel.position = 6*offset1 + Vector2(-112,-114)
		hpLabel.position = 6*offset2 + Vector2(-95.5,-121.1)
		await get_tree().process_frame
		n_time += get_process_delta_time()
func labelDie():
	var tween = create_tween()
	var tween1 = create_tween()
	tween.tween_property(atLabel,"modulate",Color(1,1,1,0),0.6)
	tween1.tween_property(hpLabel,"modulate",Color(1,1,1,0),0.6)
func updateColor(fat,nat,fhp,nhp):
	var color1 = Color(1.0, 1.0, 1.0, 1.0)
	var color2 = Color(1,1,1,1)
	if fat != nat: 
		if nat < fat: color1 = Color(1,1-0.7*(fat-nat)/(fat-1),1-0.7*(fat-nat)/(fat-1),1)
		else: color1 = Color(0.0, 1.0, 0.333, 1.0)
	if fhp != nhp:
		if nhp < fhp: color2 = Color(1,1-0.7*(fhp-nhp)/(fhp-1),1-0.7*(fhp-nhp)/(fhp-1),1)
		else: color2 = Color(0.0, 1.0, 0.333, 1.0)
	#print("开始重置颜色color1:",color1,"color2:",color2)
	#print("fat:",fat,"nat:",nat,"fhp",fhp,"nhp",nhp)
	var tween1 = create_tween()
	var tween2 = create_tween()
	tween1.tween_property(atLabel,"modulate",color1,0.2)\
	.set_trans(Tween.TRANS_SINE)\
	.set_ease(Tween.EASE_IN_OUT)
	tween2.tween_property(hpLabel,"modulate",color2,0.2)\
	.set_trans(Tween.TRANS_SINE)\
	.set_ease(Tween.EASE_IN_OUT)
	await tween1.finished
func setDamage(val:int):
	var newInstance = damageInstance.instantiate()
	hp.add_child(newInstance)
	newInstance.setDamage(val)
	newInstance.intro()
func intro():
	atLabel.scale = Vector2(0,0)
	hpLabel.scale = Vector2(0,0)
	atState.set_animation("introBase",false,0)
	hpState.set_animation("introBase",false,0)
	lvState.set_animation("introBase",false,0)
	await get_tree().create_timer(0.4).timeout
	label_intro(atLabel)
	label_intro(hpLabel)
