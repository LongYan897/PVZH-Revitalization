extends BaseCard
class_name BaseZombieCard
@export var zombieData:ZombieData
@export var zombieDataInstance:ZombieInstance
@onready var m_card = $"僵尸卡牌数值"
@onready var area = $"碰撞体积"
@onready var sprite = $"卡框"
@onready var bright = $"背景底版发光"
@onready var shaderMaterial: ShaderMaterial = sprite.material as ShaderMaterial
@onready var particles = $"粒子特效"
func _ready() -> void:
	setup()
	bindSignal()
	intro()
func _process(_delta: float) -> void:
	if type == 1:
		if displayNode:
			position.y = displayY - displayNode.rollPosY
	
	elif isDrag:
		global_position = touchPos - offset
		dragTime += _delta
	else:
		if dragTime != 0:
			if dragTime <= 0.10:
				libraryIntro()
		dragTime = 0
		offset = Vector2(0,0)
func setup():
	zombieData = ZombieData.new(zombieData)
	add_to_group("target")#添加到对应的组
	bright.modulate = Color(1.0, 0.932, 0.63, 0.0)
	if zombieDataInstance == null :
		zombieDataInstance = ZombieInstance.new(zombieData)
	particles.emitting = false
	print("加载植物资源 来自 ",zombieDataInstance.zombieData.name)
	m_card.set_attack(zombieDataInstance.get_attack())
	m_card.set_health(zombieDataInstance.get_health())
	m_card.set_cost(zombieDataInstance.get_cost())
	m_card.setPicture("BRAIN")
func bindSignal():
	area.mouse_entered.connect(func(): isOver = true)
	area.mouse_exited.connect(func(): isOver = false)
	CardManager.cardOrderSort.connect(setCardNumber)
	CardManager.cardOrderSort.connect(updatePosition)
	CardManager.cardOrderSort.connect(updateColor)
	TurnManager.turnChangeDown.connect(func():updateColor())
	TurnManager.placeNewCard.connect(func(_type:int,_val:int):updateColor())
	AQ.AQSizeChange.connect(func():updateColor())
func _input(event: InputEvent) -> void:
	if type == CardManager.CardType.LIBRARY:
		if !MessageBox.isEmpty():return
		if event is InputEventScreenTouch:
			if !event.pressed:
				return
			if isOver == false:
				buttonPressed = false
				if informationBox:updateInformationBox()
				return
			if buttonPressed == false:buttonPressed = true
			elif buttonPressed == true:buttonPressed = false
			updateInformationBox()
			tween = create_tween()
			tween.tween_property(self,"scale",Vector2(0.55,0.55),0.10)\
			.set_trans(Tween.TRANS_QUAD)\
			.set_ease(Tween.EASE_IN_OUT)
			await get_tree().create_timer(0.10).timeout
			tween = create_tween()
			tween.tween_property(self,"scale",tarScale,0.10)\
			.set_trans(Tween.TRANS_QUAD)\
			.set_ease(Tween.EASE_IN_OUT)
		return
	elif type == CardManager.CardType.ANIMATION:
		pass
	elif event is InputEventScreenTouch:
		if !MessageBox.isEmpty():return
		if event.pressed:
			if isOver == false:return
			if DragManager.drawNode != null: return
			DragManager.drawNode = self
			#print("执行按下逻辑",self.name)
			tween.kill()
			isDrag = true
			touchPos = event.position
			offset = event.position - global_position

			particles.emitting = false
			startDrag()
		else:
			if isDrag == false:return
			DragManager.drawNode = null
			#print("执行松手逻辑",self.name)
			isDrag = false
			particles.emitting = false
			endDrag()
	if event is InputEventScreenDrag:
		touchPos = event.position
func getCanPlaceTarger()->Array[Node2D]:
	var arr:Array[Node2D] = DragManager.getAllPlaceTarget()
	var res:Array[Node2D]
	for i in arr:
		if i.is_in_group("road"):
			if i.getRoadType() == Road.ROAD_TYPE.HIGH_GROUND:
				if !i.getZombieTarget():
					res.append(i)
			if i.getRoadType() == Road.ROAD_TYPE.GROUND:
				if !i.getZombieTarget():
					res.append(i)
			if i.getRoadType() == Road.ROAD_TYPE.WATER_WAY:
				if !i.getZombieTarget() && zombieData.type == ZombieData.Type.Amphibious:
					res.append(i)
	return res
func startDrag():
	z_index = 20
	particles.emitting = false
	if dragInformation:dragInformation.queue_free()
	dragInformation = load("res://场景/UI对象/拖拽图鉴.tscn").instantiate()
	add_child(dragInformation)
	dragInformation.setText(CardManager.getcardDesDiction(zombieData.name).get("description","NULL"))
	dragInformation.followNode = self
	while 1:
		await AQ.waitQueueEmpty()
		if zombieData.cardType == BaseData.CardType.TARGET:
			if TurnManager.getState() == TurnManager.Type.ZOMBIE_TURN || TurnManager.getState() == TurnManager.Type.PLAN_TURN:break
		elif zombieData.cardType == BaseData.CardType.PLAN:
			if TurnManager.getState() == TurnManager.Type.PLAN_TURN:break
		if isDrag == false: return
		await get_tree().process_frame
	if isDrag == false: return
	updateColor()
	print("startDrag from ",zombieDataInstance.get_card_name())
	if TurnManager.zombieCost < zombieDataInstance.get_cost():return
	#var arr:Array[Node2D] = DragManager.getAllPlaceTarget()
	var arr:Array[Node2D] = getCanPlaceTarger()
	if arr.is_empty():return
	particles.emitting = true
	for i in arr:
		if i.is_in_group("road"):
			var line:int = i.getLine()
			addChoice(Vector2(line,1))
		elif i.is_in_group("plant") || i.is_in_group("zombie"):
			var line:int = i.line
			var node = addChoice(Vector2(line,1),BaseCard.AddChoiceType.TARGET)
			node.position = i.position
			node.placeNode = i
func endDrag():
	z_index = 19
	var arr:Array[Node2D]
	arr.append_array(get_tree().get_nodes_in_group("choice"))
	var is_place:bool = false
	if !AQ.queue.is_empty():
		CardManager.cardListOrderSort()
		showLabelClear()
		return
	for i in arr:
		if i.hover == false:i.clear()
		if i.hover == true:
			var pos = i.getPos()
			var placeNode = i.placeNode
			i.clear()
			if is_place != true:
				var zombieInstance = place(pos,placeNode)
				if zombieData.getCardType() == BaseData.CardType.TARGET:
					i.getRoad().addZombie(zombieInstance)
				is_place = true
		if is_place == true:clear()
	if is_place == false:
		CardManager.cardListOrderSort()
		updateColor()
		showLabelClear(false)
	else:
		showLabelClear(true)
func place(pos:Vector2,placeNode)->Node2D:
	isPlace = true
	var path = "res://场景/僵尸卡/"+zombieData.name+"/"+zombieData.name+".tscn"
	var zombieInstance = load(path).instantiate()
	add_child(zombieInstance)
	zombieInstance.name = zombieDataInstance.get_card_name()+str(int(pos.x))+"_"+str(int(pos.y))
	if !placeNode:
		zombieInstance.setRoad(pos)
	elif placeNode is BaseZombie:
		zombieInstance.setRoad(pos)
	elif placeNode is BasePlant:
		zombieInstance.setEnemyRoad(pos)
	zombieInstance.loadResource(zombieDataInstance)
	zombieInstance.reparent(get_parent())
	print("cardList remove",self.name)
		#网络传递
	if TurnManager.attackMode == TurnManager.AttactMode.PLAYER:
		var cq:CQData = CQData.new()
		cq.type = CQData.Type.CHOOSE_CARD
		cq.zombieInstance = zombieDataInstance
		cq.line = pos.x
		if !placeNode:
			cq.team = PVZ.Type.ZOMBIE
		elif placeNode is BaseZombie:
			cq.team = PVZ.Type.ZOMBIE
		elif placeNode is BasePlant:
			cq.team = PVZ.Type.PLANT
		WebSocketClient.chat(cq.dict())
	CardManager.cardListErase(self)
	CardManager.cardListOrderSort()
	TurnManager.spendCost(PVZ.Type.ZOMBIE,zombieDataInstance.get_cost())
	TurnManager.placeNewCard.emit(PVZ.Type.ZOMBIE,zombieDataInstance.get_cost())
	return zombieInstance
func updateColor():
	if type != CardManager.CardType.NULL:return
	var targetColor:Color
	var targetColor1:Color
	var state:bool
	#print("card state type:",BaseData.CardType.keys()[zombieData.cardType])
	match zombieData.cardType:
		BaseData.CardType.TARGET:
			state = (TurnManager.team == 1 &&TurnManager.m_state == TurnManager.Type.PLANT_TURN)||\
			(TurnManager.team == 2 &&TurnManager.m_state == TurnManager.Type.ZOMBIE_TURN)
		BaseData.CardType.PLAN:
			state = (TurnManager.team == 1 &&TurnManager.m_state == TurnManager.Type.PLANT_TURN)||\
			(TurnManager.team == 2 &&TurnManager.m_state == TurnManager.Type.PLAN_TURN)
	#print("card state:",state)
	if TurnManager.zombieCost < zombieDataInstance.get_cost() || !state || getCanPlaceTarger().is_empty() || !AQ.queue.is_empty():
		targetColor = Color(1.0, 0.932, 0.63, 0.0)
		targetColor1 = Color(0.7,0.7,0.7,1)
	else:
		if isDrag == true:
			targetColor = Color(0.125, 1.0, 0.027,1)
		else:
			targetColor = Color(1.0, 0.933, 0.631,1)	
		targetColor1 = Color(1,1,1,1)
	var tween1 = create_tween()
	tween1.tween_property(bright,"modulate",targetColor,0.3)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_OUT)
	var tween2 = create_tween()
	tween2.tween_property(self,"modulate",targetColor1,0.3)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_OUT)
func updateInformationBox():
	if showLock == true:return
	if buttonPressed == true:
		SoundManager.createSound("res://素材/Audio/ButtonClick1.mp3",SoundManager.Bus.EFFECT)
		z_index = 25
		informationBox = load("res://场景/UI对象/信息框1.tscn").instantiate()
		information = load("res://场景/UI对象/信息按钮.tscn").instantiate()
		informationBox.position = Vector2(-10,-100)
		informationBox.z_index = -1
		add_child(informationBox)
		informationBox.add_child(information)
		informationBox.setSize(Vector2(270,0),Vector2(0.5,0),0)
		informationBox.setSize(Vector2(270,350),Vector2(0.5,0))
		information.setCenterPosition(Vector2(0,90))
	elif buttonPressed == false && is_instance_valid(informationBox):
		z_index = 25
		var node = informationBox
		var node1 = information
		showLock = true
		node.setSize(Vector2(270,0),Vector2(0.5,0))
		if is_instance_valid(node1):
			node1.clear()
		if is_instance_valid(node1) and node1.isOver == true:
			SoundManager.createSound("res://素材/Audio/ButtonCannel1.mp3",SoundManager.Bus.EFFECT)
			var instance = load("res://场景/UI对象/展示图鉴.tscn").instantiate()
			add_child(instance)
			instance.loadZombieRes(zombieData)
		await get_tree().create_timer(0.2).timeout
		if is_instance_valid(node):
			node.queue_free()
		informationBox = null
		information = null
		showLock = false
		if buttonPressed == false:
			z_index = 20
func libraryIntro():
	var library = load("res://场景/UI对象/展示图鉴.tscn").instantiate()
	add_child(library)
	library.loadZombieRes(zombieDataInstance.zombieData)
func animationIntro(line,mtype:PVZ.Type):
	type = CardManager.CardType.ANIMATION
	dragInformation = load("res://场景/UI对象/拖拽图鉴.tscn").instantiate()
	add_child(dragInformation)
	dragInformation.setText(CardManager.getcardDesDiction(zombieData.name).get("description","NULL"))
	dragInformation.followNode = self
	position = Vector2(360,600)
	bright.hide()
	await get_tree().create_timer(0.5).timeout
	var tween = create_tween()
	if (TurnManager.team == 2 && mtype == PVZ.Type.PLANT) || (TurnManager.team == 1 && mtype == PVZ.Type.ZOMBIE):
		tween.tween_property(self,"position",Vector2(RoadList.getLinePosX(line),400),0.3).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	elif (TurnManager.team == 1 && mtype == PVZ.Type.PLANT) || (TurnManager.team == 2 && mtype == PVZ.Type.ZOMBIE):
		tween.tween_property(self,"position",Vector2(RoadList.getLinePosX(line),690),0.3).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	await tween.finished
	clear()
	dragInformation.quickClear()
