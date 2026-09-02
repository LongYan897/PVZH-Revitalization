extends BaseTarget
class_name BaseZombie
@export var zombie_data: ZombieData  # 引用上面的资源类型
@export var zombieInstance: ZombieInstance #实际计算伤害
@onready var zombie:SpineSprite = $"僵尸/僵尸动画"
@onready var label = $"僵尸/伤害和血量"
@onready var area = $"碰撞体积"
@onready var state: SpineAnimationState = zombie.get_animation_state()
@onready var dirt = $"土坑"
@onready var dieEffect = $"死亡特效"
@onready var skeletonDataRes = zombie.skeleton_data_res
func _ready():
	hide()
	await get_tree().process_frame
	show()
	setup()
	bindSignal()
	extraReady()
	if !zombieInstance.getBuffVal(Buff.Type.STONE) || type == Type.Display_only:
		intro()
		introAnimation()
	else:
		stoneIntro()
func _process(delta: float) -> void:
	if isDrag == true:
		dragTime += delta
	else:
		if dragTime > 0:
			if type == Type.Normal && TurnManager.team == 1 && zombieInstance.getBuffVal(Buff.Type.STONE):pass
			else:libraryIntro()
		dragTime = 0
func setup():
	#zombie.scale = Vector2(1,1) #大小
	dieEffect.hide()
	zombie.hide()
	if type == Type.Normal:
		add_to_group("zombie")
	if zombie_data.getCardType() != BaseData.CardType.TARGET:
		dirt.hide()
		await get_tree().process_frame
		label.hide()
func intro():
	if type == Type.Display_only:return
	var len = AQ.getlen()
	AQ.addAction(trigger_intro_event)
	#锦囊阻塞
	if zombie_data.getCardType() == BaseData.CardType.PLAN && type == Type.Normal:
		z_index = 10
		AQ.addAction(func():
			while 1:
				if planClear == true:
					queue_free()
					return
				await get_tree().process_frame )
	await AQ.addThread(len)
func introAnimation():
	zombie.show()
	label.show()
	label.intro()
	introEvent()
	if zombie_data.getCardType() == BaseData.CardType.TARGET:
		state.set_animation("intro",false,0)
		await zombie.animation_completed
		state.set_animation("idle",true,0)
	elif zombie_data.getCardType() == BaseData.CardType.PLAN:
		#await get_tree().create_timer(100).timeout
		await planIntro()
		planClear = true
func hit(val:int):
	if zombie.visible == false:return
	state.set_animation("hit",false,0)
	zombieInstance.bonus_health -= val
	var offsetHP:int = 0
	if zombieInstance.get_health() < 0:
		offsetHP = -zombieInstance.get_health()
		zombieInstance.bonus_health += offsetHP
	label.updateZombieData(zombieInstance)
	label.hit(val - offsetHP)
	await waitAnimationComP(0)
	if state.get_track(0).get_animation().get_name() != "die":
		state.set_animation("idle",true,0)
func adjustDie():
	if zombieInstance.get_health() <= 0:die()
func die():
	RoadList.erase(self)
	remove_from_group("zombie")
	is_alive = false
	var action=func():
		trigger_die_event()
	AQ.addAction(action)
	while 1:
		if state.get_track(0).get_animation().get_name() != "hit":break
		await get_tree().process_frame
	state.set_animation("die",false,0)
	await zombie.animation_completed
	await get_tree().create_timer(0.3).timeout
	label.die()
	state.clear_track(0)
	dirt.clear()
	await dieEffect.intro()
	self.queue_free()
func attack():
	if zombieInstance.get_attack() == 0 || zombie.visible == false:return
	var action = func():
		await attackAnimation()
	AQ.addAction(action)
func attackAnimation():
		state.set_animation("attack",false,0)
		await waitAnimationComP(0)
		state.set_animation("idle",true,0)
func setRoad(pos:Vector2i):
	line = pos.x
	col = pos.y
	position = Vector2(140+line*107,690-200*(col-1))
	dirt.setRoad(line)
func setEnemyRoad(pos:Vector2i):
	line = pos.x 
	col = pos.y
	position = Vector2(140+line*107,400-200*(col-1))
	dirt.setRoad(line)
func loadResource(data:ZombieInstance):
	zombieInstance = ZombieInstance.new(data)
	label.updateZombieData(zombieInstance,true)
func getPosition(pos = null):
	return global_position
func trigger_intro_event():
	pass
func trigger_attack_event():
	var plantNode = RoadList.getRoad(line).getForwardPlantTarget()
	var plantPosition
	if plantNode :
		plantPosition = plantNode.getPosition(global_position)
		var attackNode:BaseAttackEffect = attackEffect.instantiate()
		add_child(attackNode)
		attackNode.loadAnimation(skeletonDataRes,false)
		await attackNode.posMove(position,plantPosition+Vector2(0,0),0.50,BaseAttackEffect.Type.LINEAR,true)
		var val = await plantNode.hit(zombieInstance.get_attack())
func trigger_die_event():
	pass
func trigger_plan_event():
	print("trigger_plan_event",self)
	if !zombieInstance.getBuffVal(Buff.Type.STONE):return
	var fun = func():
		zombieInstance.deleteBuff(Buff.Type.STONE)
		stone.clear()
		await get_tree().create_timer(1).timeout
		introAnimation()
		await intro()
	AQ.addAction(fun)
	var cq = CQData.new()
	cq.line = line
	cq.type = CQData.Type.ZOMBIE_STONE_INTRO
	WebSocketClient.chat(cq.dict())
func stoneIntro():
	stone = load("res://场景/UI对象/墓碑.tscn").instantiate()
	label.hide()
	add_child(stone)
func bindSignal():
	area.mouse_entered.connect(func(): isOver = true)
	area.mouse_exited.connect(func(): isOver = false)
	zombie.animation_event.connect(animationEvent)
func animationEvent(spine_sprite: Object, animation_state: Object, track_entry:Object, event:Object):
	var eventName = event.get_data().get_event_name()
	match eventName:
		"attack":
			if type != Type.Display_only:
				trigger_attack_event()
		"die":
			var tween = create_tween()
			tween.tween_property(zombie,"modulate",Color(1,1,1,0),0.3)\
			.set_trans(Tween.TRANS_QUAD)\
			.set_ease(Tween.EASE_IN_OUT)
		_:
			extraAnimationEvent(eventName)
func libraryIntro():
	var library = load("res://场景/UI对象/展示图鉴.tscn").instantiate()
	add_child(library)
	library.loadZombieRes(zombieInstance.zombieData)
	MessageBox.addMessageBox(library)
func _input(event: InputEvent) -> void:
	if ChooseManager.chooseCount != 0:return
	if type == Type.Display_only:return
	if !MessageBox.isEmpty():return
	
	if event is InputEventScreenTouch:
		if !event.pressed:
			isDrag = false
			return
		if isOver == false:return
		isDrag = true
	elif event is InputEventMouseButton && event.button_index == MOUSE_BUTTON_LEFT:
		if !isOver:return
		if event.is_pressed():
			isDrag = true
		else:
			isDrag = false
func waitAnimationComP(track:int = 0):
	var a = state.get_track(track)
	while a.get_animation_time() < a.get_animation_end() - 0.05:
		await get_tree().process_frame
	await get_tree().create_timer(0.05).timeout
func extraAnimationEvent(eventName):
	pass
