extends Node
# AIManager
signal startAISim(state)
@export var curSnap:Snapshot
@export var checkSnap:Snapshot
@export var playDelta:float = 0
@export var playType:Archetype = Archetype.MIDRANGE
@export var simCount:int = 1
@export var bestScore:int = -INF
@export var bestDepth:int = -INF
@export var CQList:Array[CQData]
enum Archetype{
	AGGRO = 0,	 # 快攻
	CONTROL,	# 控制
	MIDRANGE,	#中速
	COMBO,		# 组合技
	TEMPO,		#节奏
}
enum CardType{
	ALL = 0,
	TARGET_ONLY,
	PLAN_ENVIRONMENT_ONLY,
}
var zombieIntroHandler:Dictionary = {
	"消防员僵尸":
		func(snap:Snapshot,data:ZombieInstance,turn:int,cq:CQData):
			var res:Array[ZombieInstance] = snap.getAllIntroZombie()
			res.erase(data)
			if res.is_empty():return
			var chooseNum = randi() % res.size()
			var line = snap.getZombieLine(res[chooseNum])
			var zombie = res[chooseNum]
			var instanceName = zombie.get_card_name()
			var cardRes = CardManager.getCardRes(instanceName)
			if cardRes == null:return
			var newInstance = ZombieInstance.new(cardRes)
			snap.cardList.append(newInstance)
			if turn == 0:
				var fromLine = snap.getZombieLine(data)
				if fromLine == null:return
				var CQ:CQData = CQData.new()
				CQ.type = CQData.Type.POP_ZOMBIE_TARGET
				CQ.line = line
				CQ.fromLine = fromLine
				snap.CQList.append(CQ)
			snap.roadList[line].zombie = null,
	"滚石锦囊":
		func(snap:Snapshot,data:ZombieInstance,turn:int,cq:CQData):
			var line = cq.line
			cq.team = PVZ.Type.PLANT
			snap.roadList[line].plant1 = null
			snap.roadList[line].plant2 = null,
	"最终任务锦囊":
		func(snap:Snapshot,data:ZombieInstance,turn:int,cq:CQData):
			var line = cq.line
			cq.team = PVZ.Type.ZOMBIE
			snap.roadList[line].zombie = null
			var arr = snap.getAllPlant()
			var node = arr[randi_range(0,arr.size()-1)]
			var mcq = CQData.new()
			mcq.type = CQData.Type.CHOOSE_CQ
			mcq.line = snap.getPlantLine(node)
			mcq.col = snap.getPlantCol(node)
			mcq.plantInstance = node
			snap.ExCQList.append(mcq)
			node.bonus_health -= 4
			snap.adjustDie(),
	"召唤直升机锦囊":
		func(snap:Snapshot,data:ZombieInstance,turn:int,cq:CQData):
			var line = cq.line
			cq.team = PVZ.Type.ZOMBIE
			snap.roadList[line].zombie = ZombieInstance.new(CardManager.getCardRes("直升机小鬼僵尸"))
}
func _ready() -> void:
	bindSignal()
func bindSignal():
	startAISim.connect(getSceneSnap)
func getSceneSnap(state:TurnManager.Type = TurnManager.Type.ZOMBIE_TURN):
	checkSnap = Snapshot.new()
	checkSnap.getSnapScore()
	simPlentyStart(checkSnap,curSnap,100,false,state)
	#simPlentyStart(checkSnap,curSnap,1,true,state)
	#simPlentyStart(checkSnap,curSnap,100,true,state)
	await execCQList()
func setPlayDelta(snap:Snapshot,mtype:Archetype = playType,text:bool = true):
	var cardCount = snap.cardList.size()
	match mtype:
		Archetype.AGGRO:
			playDelta = 0.6
		Archetype.CONTROL:
			playDelta = 0.3
		Archetype.MIDRANGE:
			playDelta = 0.4
		Archetype.COMBO:
			playDelta = 0.4
		Archetype.TEMPO:
			playDelta = 0.5
	var hand_ratio = (10 - cardCount) / 10.0
	playDelta = playDelta * pow(hand_ratio, 0.7)
	# 限制范围，防止极端值
	if TurnManager.team == 2:
		playDelta *= 1.2
	else:
		playDelta *= 0.8
	playDelta = clamp(playDelta, 0.05, 0.8)
	if text:print("Snapshot setPlayDelta:",playDelta)
func getCanPlaceTarget(snap:Snapshot,mName:String)->Array[int]:
	var res:Array[int]
	match mName:
		"滚石锦囊":
			var arr = snap.getAllPlant()
			for i in arr:
				if i.get_attack() <= 2:
					res.append(snap.getPlantLine(i))
		"最终任务锦囊":
			if !snap.getAllPlant():return res
			var arr = snap.getAllZombie()
			for i in arr:
				res.append(snap.getZombieLine(i))
		"召唤直升机锦囊":
			if !snap.getCanPlaceRoad(PVZ.Type.ZOMBIE,BaseData.Type.Amphibious):return res
			var arr = snap.getCanPlaceRoad(PVZ.Type.ZOMBIE,BaseData.Type.Amphibious)
			for i in arr:
				res.append(i.line)
	return res
func simTurn(snap:Snapshot,text:bool = true):
	if text:print("Snapshot 开始模拟回合")
	for i in range(snap.roadList.size()):
		if snap.roadList[i].zombie:
			var zombie = snap.roadList[i].zombie
			var plant = snap.getPlantTar(i)
			#如果是墓碑状态跳过攻击
			if !zombie.getBuffVal(Buff.Type.STONE):
				if plant:
					var plant1:PlantInstance = plant
					plant1.bonus_health -= zombie.get_attack()
				else: snap.plantHeroHP -= zombie.get_attack()
		if snap.roadList[i].plant2:
			var zombie = snap.getZombieTar(i)
			var plant = snap.roadList[i].plant2
			if zombie:
				var zombie1:ZombieInstance = zombie
				#如果是墓碑状态跳过攻击
				if !zombie1.getBuffVal(Buff.Type.STONE):
					zombie1.bonus_health -= plant.get_attack()
			else: snap.zombieHeroHP -= plant.get_attack()
		if snap.roadList[i].plant1:
			var zombie = snap.getZombieTar(i)
			var plant = snap.roadList[i].plant1
			if zombie:
				var zombie1:ZombieInstance = zombie
				#如果是墓碑状态跳过攻击
				if !zombie1.getBuffVal(Buff.Type.STONE):
					zombie1.bonus_health -= plant.get_attack()
			else: snap.zombieHeroHP -= plant.get_attack()
		#清除单位
		if snap.roadList[i].zombie:
			if snap.roadList[i].zombie.get_health() <=0:
				snap.roadList[i].removeTar(snap.roadList[i].zombie)
		if snap.roadList[i].plant1:
			if snap.roadList[i].plant1.get_health() <=0:
				snap.roadList[i].removeTar(snap.roadList[i].plant1)
		if snap.roadList[i].plant2:
			if snap.roadList[i].plant2.get_health() <=0:
				snap.roadList[i].removeTar(snap.roadList[i].plant2)
		
	if text:snap.iterateRoadList()
	if text:snap.getSnapScore()
func simTurnSetup(snap:Snapshot):
	snap.turnCost += 1
	snap.cost = snap.turnCost
	snap.turnCount += 1
func simCard(snap:Snapshot,text:bool = true,cardType:CardType = CardType.ALL):
	setPlayDelta(snap,playType,text)
	if text:print("Snapshot curRoad:",snap.roadList.size())
	var copyCard:Array
	match cardType:
		CardType.ALL:
			copyCard = snap.copyCardList()
		CardType.TARGET_ONLY:
			copyCard = Snapshot.selectCard(snap.copyCardList(),CardType.TARGET_ONLY)
		CardType.PLAN_ENVIRONMENT_ONLY:
			copyCard = Snapshot.selectCard(snap.copyCardList(),CardType.PLAN_ENVIRONMENT_ONLY)
	
	var playCard:Array
	#随机获取卡牌到卡组
	for i in range(copyCard.size()):
		var size = copyCard.size()
		var instance = copyCard[randi()%size]
		for p in range(snap.cardList.size()):
			if snap.cardList[p].isEqual(instance):
				snap.cardList.remove_at(p)
				break
		copyCard.erase(instance)
		if randf() <= playDelta:continue
		if instance.get_cost() > snap.cost:continue
		
		snap.cost -= instance.get_cost()
		playCard.append(instance)
		#直接打出
		simPlaceCard(snap,instance,text)
	#打印消息
	if text:
		var cardName:String
		for i in playCard:
			cardName += i.get_card_name()+" "
		print("Snapshot playCardList:",cardName)
func simPlaceCard(snap:Snapshot,instance:BaseInstance,text:bool=true):
	var data
	var placeRoad:Array[int]
	#判断类别并且给出可放置的位置
	if instance is ZombieInstance:
		data = instance.zombieData
		if instance.getCardType() == BaseData.CardType.TARGET:
			for i in range(snap.roadList.size()):
				if data.type == ZombieData.Type.Ground && snap.roadList[i].type == Road.ROAD_TYPE.WATER_WAY:continue
				if !snap.roadList[i].zombie: placeRoad.append(i)
		elif instance.getCardType() == BaseData.CardType.PLAN:
			placeRoad = getCanPlaceTarget(snap,instance.get_card_name())
	elif instance is PlantInstance:
		data = instance.plantData
		if instance.getCardType() == BaseData.CardType.TARGET:
			for i in range(snap.roadList.size()):
				if data.type == PlantData.Type.Ground && snap.roadList[i].type == Road.ROAD_TYPE.WATER_WAY:continue
				if !(snap.roadList[i].plant1 || snap.roadList[i].plant2): placeRoad.append(i)
	
	#随机放置函数，先不写了FAQ
	if text:print("Snapshot canPlaceLine:",placeRoad)
	if placeRoad.is_empty():
		if text:print("Snapshot 没有放置位置")
		return
	var tarRoad = placeRoad[randi()%placeRoad.size()]
	var CQ:CQData = CQData.new()
	CQ.type = CQData.Type.CHOOSE_CARD
	CQ.line = tarRoad
	if instance.getCardType() == BaseData.CardType.TARGET:
		if instance is ZombieInstance:
			var placedZombie = ZombieInstance.new(instance)
			snap.roadList[tarRoad].zombie = placedZombie
			CQ.zombieInstance = ZombieInstance.new(instance)
			CQ.team = PVZ.Type.ZOMBIE
			if !placedZombie.getBuffVal(Buff.Type.STONE):
				simCardIntro(snap,placedZombie,0,CQ)
			if snap.turnCount == 0:
				snap.CQList.append(CQ)
				snap.CQList.append_array(snap.ExCQList)
				snap.ExCQList.clear()
				if text: 
					print("Snapshot CQList addName:",instance.get_card_name())
		elif instance is PlantInstance:
			snap.roadList[tarRoad].plant1 = PlantInstance.new(instance)
			CQ.plantInstance = PlantInstance.new(instance)
			CQ.team = PVZ.Type.PLANT
			simCardIntro(snap,instance,0,CQ)
			if snap.turnCount == 0:
				snap.CQList.append(CQ)
				snap.CQList.append_array(snap.ExCQList)
				snap.ExCQList.clear()
				if text: 
					print("Snapshot CQList addName:",instance.get_card_name())
	elif instance.getCardType() == BaseData.CardType.PLAN:
		CQ.zombieInstance = ZombieInstance.new(instance)
		simCardIntro(snap,instance,0,CQ)
		if snap.turnCount == 0:
			snap.CQList.append(CQ)
			snap.CQList.append_array(snap.ExCQList)
			snap.ExCQList.clear()
		if text: 
			print("Snapshot CQList addName:",instance.get_card_name())
	if text:print("Snapshot placeLine:",tarRoad,"\tcardName:",instance.get_card_name())
func simCardStoneIntro(snap:Snapshot,line:int = 0):
	var CQ:CQData = CQData.new()
	CQ.type = CQ.Type.ZOMBIE_STONE_INTRO
	CQ.line = line
	snap.CQList.append(CQ)
func simPlanIntro(snap:Snapshot,text:bool = true,turn:int = 0):
	var array = snap.getAllZombie()
	for i in array:
		if i.getBuffVal(Buff.Type.STONE):
			i.deleteBuff(Buff.Type.STONE)
			simCardStoneIntro(snap,snap.getZombieLine(i))
			simCardIntro(snap,i,turn,null)
func simCardIntro(snap:Snapshot,data,turn:int = 0,cq:CQData = null):
	var hanlder
	if data is PlantInstance:
		pass
	elif data is ZombieInstance:
		hanlder = zombieIntroHandler.get(data.get_card_name(),null)
	if hanlder == null:return
	hanlder.call(snap,data,turn,cq)
func simStart(snap:Snapshot,text:bool = true,state:TurnManager.Type = TurnManager.Type.ZOMBIE_TURN):
	var depth:int = 99999
	for i in range(simCount):
		if text:print("Snapshot 模拟回合:",i+1)
		if state == TurnManager.Type.PLAN_TURN && i == 0:
			#TurnManager.iterateRoadList()
			#snap.iterateRoadList()
			simPlanIntro(snap,text,i)
			simCard(snap,text,CardType.PLAN_ENVIRONMENT_ONLY)
		else:
			#区分植物和僵尸阵营
			if TurnManager.team == 2:
				simCard(snap,text,CardType.ALL)
				if i == 0:snap.endCQTurn()
				simPlanIntro(snap,text,i)
			else:
				simCard(snap,text,CardType.TARGET_ONLY)
				if i == 0:snap.endCQTurn()
				simPlanIntro(snap,text,i)
				simCard(snap,text,CardType.PLAN_ENVIRONMENT_ONLY)
		simTurn(snap,text)
		simTurnSetup(snap)
		if text:print("Snapshot 模拟结束:")
		if depth == 99999 && snap.getSnapScore() > 800:
			depth = i
	
	var score = snap.getSnapScore()
	if depth < bestDepth:
		bestScore = score
		bestDepth = depth
		CQList = snap.copyCQList()
		if text:print("Snapshot 更新bestDepth:",bestDepth)
	elif score > bestScore:
		bestScore = score
		CQList = snap.copyCQList()
		if text:print("Snapshot 更新bestScore:",bestScore)
func simPlentyStart(snap:Snapshot,simSnap:Snapshot,pSimCount:int = 100,text:bool = false,state:TurnManager.Type = TurnManager.Type.ZOMBIE_TURN):
	bestScore = -INF
	bestDepth = 99999
	for i in range(pSimCount):
		simSnap = snap.copy()
		simStart(simSnap,text,state)
	print("Snapshot 批量模拟结束\t一共模拟",pSimCount,"次","\t最佳分数:",bestScore,"\t最佳深度:",bestDepth)
	iterateCQList()
func iterateCQList():
	for i in range(CQList.size()):
		print("Snapshot CQList num:",i,"\ttype:",CQData.Type.keys()[CQList[i].type].to_lower(),"\tline:",CQList[i].line,"\tfromLine:",CQList[i].fromLine)
func waitCQListEmpty():
	while 1:
		if CQList.is_empty():
			await get_tree().create_timer(1).timeout
			return
		await get_tree().create_timer(0.05).timeout
func execCQList():
	for i in CQList:
		if i.type == CQData.Type.CHOOSE_CQ:
			ChooseManager.chooseCQ.emit(i)
	while 1:
		#await get_tree().create_timer(999).timeout
		if CQList.is_empty():
			TurnManager.turnChange.emit(1)
			return
		var CQ = CQList[0]
		match CQ.type:
			CQData.Type.CHOOSE_CARD:
				await Enemy.startAnimation(CQ.getData(),CQ.line,CQ.team)
			CQData.Type.POP_ZOMBIE_TARGET:
				await Enemy.popZombieInstance(CQ.line)
			CQData.Type.END_TURN:
				await get_tree().create_timer(1).timeout
				TurnManager.turnChange.emit(1)
				CQList.clear()
				return
			CQData.Type.ZOMBIE_STONE_INTRO:
				if CQ.line < 0 or CQ.line >= RoadList.list.size():
					push_warning("AI ZOMBIE_STONE_INTRO line 越界: %s" % CQ.line)
					CQList.remove_at(0)
					continue
				var node:BaseZombie = RoadList.list[CQ.line].zombie
				if node == null:
					push_warning("AI ZOMBIE_STONE_INTRO 找不到僵尸: %s" % CQ.line)
					CQList.remove_at(0)
					continue
				node.zombieInstance.deleteBuff(Buff.Type.STONE)
				node.stone.clear()
				await get_tree().create_timer(1).timeout
				await node.introAnimation()
			CQData.Type.CHOOSE_CQ:
					pass
		await AQ.waitQueueEmpty()
		CQList.remove_at(0)
		await get_tree().process_frame
