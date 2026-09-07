extends Node
enum Type{
NULL = 0,
CARD_TURN,
ZOMBIE_TURN,
PLANT_TURN,
PLAN_TURN,
ATTACK_TURN,
}
enum AttactMode{
	AI = 0,
	PLAYER,
}
signal turnChange(systemEmit:int)
signal turnChangeEnd1()
signal turnChangeBegin1()
signal turnChangeDown()
signal playerCostUpdate()
signal placeNewCard(type:PVZ.Type,val:int)
signal updateState()
var m_state:int = 0
@export var team:int = 1
var attackOnce:bool = false
var cardOnce:bool = false
var plantCost:int
var zombieCost:int
var plantTurnCost:int
var zombieTurnCost:int
var turnCount:int = 1
var plantShield:Node2D
var zombieShield:Node2D
var attackMode:AttactMode = AttactMode.AI
func _ready() -> void:
	bindSignal()
	stateControl()
func getState():
	return m_state
func setState(type:Type):
	m_state = type
	updateState.emit()
func stateControl():
	while 1:
		if Enemy.gameNode == null:
			await get_tree().process_frame
			continue
		if !AQ.queue.is_empty():
			await get_tree().process_frame
			continue
		if team == 1:
			match m_state:
				Type.CARD_TURN:
					if cardOnce == false:
						turnChange.emit(1)
						cardOnce = true
				Type.ZOMBIE_TURN:
					if attackMode != AttactMode.PLAYER:
						AIManager.startAISim.emit(m_state)
						await AIManager.waitCQListEmpty()
				Type.PLAN_TURN:
					if attackMode != AttactMode.PLAYER:
						AIManager.startAISim.emit(m_state)
						await AIManager.waitCQListEmpty()
				Type.ATTACK_TURN:
					if attackOnce == false:
						turnChange.emit(1)
						attackOnce = true
		elif team == 2:
			match m_state:
				Type.CARD_TURN:
					if cardOnce == false:
						turnChange.emit(1)
						cardOnce = true
				Type.PLANT_TURN:
					if attackMode != AttactMode.PLAYER:
						AIManager.startAISim.emit(m_state)
						await AIManager.waitCQListEmpty()
					#turnChange.emit(1)
				Type.ATTACK_TURN:
					if attackOnce == false:
						turnChange.emit(1)
						attackOnce = true
		await get_tree().process_frame
func bindSignal():
	turnChangeBegin1.connect(listenTurnBegin)
	turnChangeEnd1.connect(listenTurnEnd)
	updateState.connect(triggerPlanTurn)
	WebSocketClient.receiveData.connect(reciveMsg)
func reciveMsg(data):
	if data.has("uuid") and data.uuid == WebSocketClient.uuid:
		return
	var hex_msg := str(data.get("msg", ""))
	if hex_msg == "":
		push_warning("收到的消息没有 msg 字段")
		return
	var bytes := hex_msg.hex_decode()
	var original_data = bytes_to_var(bytes)
	print("原始接收消息:", original_data)
	match original_data.sendType:
		"CQData":
			var CQ = CQData.fromDict(original_data)
			print("转译消息:", CQ)
			match CQ.type:
				CQData.Type.CHOOSE_CARD:
					Enemy.startAnimation(CQ.getData(),CQ.line,CQ.team)
				CQData.Type.END_TURN:
					await AQ.waitQueueEmpty()
					TurnManager.turnChange.emit(1)
				CQData.Type.ZOMBIE_STONE_INTRO:
					var node:BaseZombie = RoadList.list[CQ.line].zombie
					node.zombieInstance.deleteBuff(Buff.Type.STONE)
					node.stone.clear()
					await get_tree().create_timer(1).timeout
					await node.introAnimation()
				CQData.Type.POP_ZOMBIE_TARGET:
					var zombie:BaseZombie = RoadList.list[CQ.line].zombie
					var newZombie = CardManager.getCardRes(zombie.zombieInstance.get_card_name())
					zombie.queue_free()
					RoadList.list[CQ.line].zombie = null
					DrawCard.drawZombieCard(Enemy.gameNode,ZombieInstance.new(newZombie),1,zombie.position)
				CQData.Type.CHOOSE_CQ:
					ChooseManager.chooseCQ.emit(CQ)
func listenTurnBegin():
	print("listen Turn Begin:",Type.keys()[m_state])
	match m_state:
		Type.CARD_TURN:
			SoundManager.setAudioVolOnST(2,0,3)
			if team == 1:
				if turnCount == 1:await get_tree().create_timer(0.1).timeout
				#await get_tree().create_timer(1).timeout
				HeroManager.plantHero.playTurnSound(turnCount)
			startDrawCard()
func listenTurnEnd():
	print("listen Turn End:",Type.keys()[m_state])
	match m_state:
		Type.ATTACK_TURN:
			startAttack()
			var rand = randi_range(1,8)
			var path = "res://素材/Audio/AttackBgm/AttackBgm" + str(rand) + ".mp3"
			var effectPath = "res://素材/Audio/AttackBgm/cameAttack" + str(randi_range(1,3)) + ".mp3"
			SoundManager.createSound(effectPath,SoundManager.Bus.EFFECT)
			SoundManager.setAudioOnST(2,path,0.2,true)
			SoundManager.setAudioVolOnST(0,0,0.2)
			SoundManager.setAudioVolOnST(1,0,0.2)
			
func startAttack():
	AQ.addAction(startAttackCenter.bind(0))
func startAttackCenter(road:int):
	var aRoad = RoadList.getRoad(road)
	await aRoad.attack()
	await get_tree().process_frame
	if road == 4 :return 
	AQ.addAction(startAttackCenter.bind(road+1))
func costPlay():
	var costInstnace1 = load("res://场景/UI对象/阳光_脑子_显示.tscn").instantiate()
	var costInstnace2 = load("res://场景/UI对象/阳光_脑子_显示.tscn").instantiate()
	Enemy.gameNode.add_child(costInstnace1)
	Enemy.gameNode.add_child(costInstnace2)
	costInstnace1.setType(PVZ.Type.PLANT)
	costInstnace2.setType(PVZ.Type.ZOMBIE)
	costInstnace1.intro(plantTurnCost,PVZ.Type.PLANT)
	costInstnace2.intro(zombieTurnCost,PVZ.Type.ZOMBIE)
	await get_tree().create_timer(1.2).timeout
	playerCostUpdate.emit()
func startDrawCard():
	var res = func():
		var mlen =AQ.getlen()
		turnCount += 1
		plantTurnCost += 1
		zombieTurnCost += 1
		plantCost = plantTurnCost
		zombieCost = zombieTurnCost
		await costPlay()
		var plantInstance = PlantInstance.new(CardManager.getCardRes("豌豆射手植物"))
		var zombieInstance = ZombieInstance.new(CardManager.getCardRes("最终任务锦囊"))
		#var zombieInstance = ZombieInstance.new(CardManager.getCardRes("基础僵尸"))
		DrawCard.drawPlantCard(Enemy.gameNode,plantInstance,2)
		DrawCard.drawZombieCard(Enemy.gameNode,zombieInstance,2)
		cardOnce = false
		await AQ.addThread(mlen)
		await get_tree().create_timer(1.2).timeout
	AQ.addAction(res)
func spendCost(player:PVZ.Type,val:int):
	if player == PVZ.Type.PLANT:
		plantCost -= val
	if player == PVZ.Type.ZOMBIE:
		zombieCost -= val
	playerCostUpdate.emit()
	if player == PVZ.Type.PLANT:
		placeNewCard.emit(PVZ.Type.PLANT,val)
	if player == PVZ.Type.ZOMBIE:
		placeNewCard.emit(PVZ.Type.ZOMBIE,val)
func setPlayerCost(type:PVZ.Type,val:int,add:bool = false):
	var costInstnace1 = load("res://场景/UI对象/阳光_脑子_显示.tscn").instantiate()
	Enemy.gameNode.add_child(costInstnace1)
	if add == true:
		if type == PVZ.Type.PLANT:
			plantCost += val
		elif type == PVZ.Type.ZOMBIE:
			zombieCost += val
	else:
		if type == PVZ.Type.PLANT:
			plantCost = val
		elif type == PVZ.Type.ZOMBIE:
			zombieCost = val
	
	if type == PVZ.Type.PLANT:
		costInstnace1.setType(1)
		if team == 1:
			costInstnace1.intro(val,1)
		if team == 2:
			costInstnace1.intro(val,2)
	if type == PVZ.Type.ZOMBIE:
		costInstnace1.setType(2)
		if team == 1:
			costInstnace1.intro(val,2)
		if team == 2:
			costInstnace1.intro(val,1)
	await get_tree().create_timer(1.2).timeout
	playerCostUpdate.emit()
	await get_tree().create_timer(2).timeout
func getAllTarget(type:PVZ.Type = PVZ.Type.PLANT_AND_ZOMBIE,sort:bool = true)->Array[Node2D]:
	var res:Array[Node2D]
	match type:
		PVZ.Type.PLANT:
			res.append_array(get_tree().get_nodes_in_group("plant"))
		PVZ.Type.ZOMBIE:
			res.append_array(get_tree().get_nodes_in_group("zombie"))
		PVZ.Type.PLANT_AND_ZOMBIE:
			res.append_array(get_tree().get_nodes_in_group("plant"))
			res.append_array(get_tree().get_nodes_in_group("zombie"))
	
	if sort == true:res.sort_custom(sortTarget)
	print("getAllTarget:",res)
	return res
func getRoadTarget(type:PVZ.Type = PVZ.Type.PLANT_AND_ZOMBIE,road:int = 1,sort:bool = true)->Array[Node2D]:
	var array = getAllTarget(type,false)
	var res:Array
	for i in array:
		if i.line == road:
			res.append(i)
	if sort == true:res.sort_custom(sortTarget)
	print("getRoadTarget:",res)
	return res
func sortTarget(a:Node2D,b:Node2D):
	if a.line < b.line:
		return true
	elif a.line > b.line:
		return false
	else:
		if a.is_in_group("zombie"):
			return true
func triggerPlanTurn():
	if m_state == Type.PLAN_TURN:
		if team == 1:return
		await get_tree().process_frame
		#await get_tree().create_timer(1).timeout
		for line in range(0,5):
			var array = getRoadTarget(PVZ.Type.ZOMBIE,line)
			for i:BaseZombie in array:
				#print("triggerPlanTurn")
				if !is_instance_valid(i):continue
				var node:BaseZombie = i
				var len = AQ.getlen()
				node.trigger_plan_event()
				await AQ.addThread(len)
func clearAutoLoad():
	RoadList.list.clear()
	Enemy.cardList.clear()
	CardManager.cardList1.clear()
	CardManager.cardList2.clear()
	plantTurnCost = 0
	zombieTurnCost = 0
func iterateRoadList():
	for i in range(RoadList.list.size()):
		if RoadList.list[i].zombie:
			print("Snapshot TarLine:",i,"\tzombie:",RoadList.list[i].zombie.zombieInstance.getStrPro(),"\t 墓碑:",RoadList.list[i].zombie.zombieInstance.getBuffVal(Buff.Type.STONE))
		if RoadList.list[i].plant1:
			print("Snapshot TarLine:",i,"\tplant1:",RoadList.list[i].plant1.plantInstance.getStrPro())
		if RoadList.list[i].plant2:
			print("Snapshot TarLine:",i,"\tplant2:",RoadList.list[i].plant2.plantInstance.getStrPro())
