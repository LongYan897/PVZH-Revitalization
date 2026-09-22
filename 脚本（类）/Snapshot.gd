extends Resource
class_name Snapshot
@export var roadList:Array[DataRoad]
@export var plantHeroHP:int = 100
@export var zombieHeroHP:int = 100
@export var turnCount:int = 0
@export var turnCost:int = 0
@export var cost:int = 0
@export var cardList:Array
@export var CQList:Array[CQData]
@export var ExCQList:Array[CQData]
func _init(data = null) -> void:
	if data == null:
		cardList.clear()
		CQList.clear()
		plantHeroHP = HeroManager.plantHero.heroData.health
		zombieHeroHP = HeroManager.zombieHero.heroData.health
		getCost()
		getRoadList()
		getCardList()
		return
	if data is Snapshot:
		cardList.clear()
		CQList.clear()
		for i in data.roadList:
			roadList.append(DataRoad.new(i))
		CQList = data.copyCQList()
		plantHeroHP = data.plantHeroHP
		zombieHeroHP = data.zombieHeroHP
		cost = data.cost
		turnCost = data.turnCost
		turnCount = data.turnCount
		for i in data.cardList:
			if i is ZombieInstance:
				cardList.append(ZombieInstance.new(i))
			if i is PlantInstance:
				cardList.append(PlantInstance.new(i))
		for i in data.ExCQList:
			ExCQList.append(i)
		return
func copy():
	var res:Snapshot = Snapshot.new()
	res.cardList.clear()
	res.roadList.clear()
	for i in roadList:
		res.roadList.append(DataRoad.new(i))
	res.CQList = copyCQList()
	res.plantHeroHP = plantHeroHP
	res.zombieHeroHP = zombieHeroHP
	res.cost = cost
	res.turnCost = turnCost
	res.turnCount = turnCount
	for i in cardList:
		if i is ZombieInstance:
			res.cardList.append(ZombieInstance.new(i))
		elif i is PlantInstance:
			res.cardList.append(PlantInstance.new(i))
	for i in ExCQList:
			res.ExCQList.append(i)
	return res
func copyCardList():
	var res:Array
	for i in cardList:
		if i is ZombieInstance:
			res.append(ZombieInstance.new(i))
		if i is PlantInstance:
			res.append(PlantInstance.new(i))
	return res
func copyCQList():
	var res:Array[CQData]
	for i in CQList:
		res.append(CQData.new(i))
	return res
func getRoadList():
	roadList.clear()
	for i in RoadList.list:
		var newRoad = DataRoad.new(i)
		roadList.append(newRoad)
	
	#iterateRoadList()
func getCardList():
	if TurnManager.team == 1:
		for i in Enemy.cardList:
			var instance:ZombieInstance = ZombieInstance.new(i.zombieInstance)
			cardList.append(instance)
	elif TurnManager.team == 2:
		for i in Enemy.cardList:
			var instance:PlantInstance = PlantInstance.new(i.plantInstance)
			cardList.append(instance)
			
	#iterateCardList()
func iterateRoadList():
	for i in range(roadList.size()):
		if roadList[i].zombie:
			print("Snapshot TarLine:",i,"\tzombie:",roadList[i].zombie.getStrPro(),"\t 墓碑:",roadList[i].zombie.getBuffVal(Buff.Type.STONE))
		if roadList[i].plant1:
			print("Snapshot TarLine:",i,"\tplant1:",roadList[i].plant1.getStrPro())
		if roadList[i].plant2:
			print("Snapshot TarLine:",i,"\tplant2:",roadList[i].plant2.getStrPro())
func iterateCardList():
	for i in range(cardList.size()):
		print("Snapshot CardNum:",i,"\tcardName",cardList[i].get_card_name())
func getRoadListScore(type:PVZ.Type):
	var score:int = 0
	for i in roadList:
		if i.zombie:
			var zombie = i.zombie
			score += zombie.get_attack()+zombie.get_health()
		if i.plant1:
			var plant = i.plant1
			score -= plant.get_attack()+plant.get_health()
		if i.plant2:
			var plant = i.plant2
			score -= plant.get_attack()+plant.get_health()
	if type == PVZ.Type.PLANT:
		score = -score
	return score
func getSnapScore():
	var score:int = 0
	if TurnManager.team == 1:
		if plantHeroHP <= 0:
			score += 999
		if zombieHeroHP <= 0:
			score -= 999
		score += getRoadListScore(PVZ.Type.ZOMBIE)
		score += zombieHeroHP*2 - plantHeroHP*2
	else:
		if plantHeroHP <= 0:
			score -= 999
		if zombieHeroHP <= 0:
			score += 999
		score += getRoadListScore(PVZ.Type.PLANT)
		score += plantHeroHP*2 - zombieHeroHP*2
	#print("Snapshot score:",score)
	return score
func getPlantTar(line:int = 0):
	if roadList.is_empty():return null
	
	if roadList[line].plant2:
		return roadList[line].plant2
	if roadList[line].plant1:
		return roadList[line].plant1
	return null
func getZombieTar(line:int = 0):
	if roadList.is_empty():return null
	
	if roadList[line].zombie:
		return roadList[line].zombie
	return null
func getCost():
	if TurnManager.team == 1:
		turnCost = TurnManager.zombieTurnCost
		cost = TurnManager.zombieCost
	else:
		turnCost = TurnManager.plantTurnCost
		cost = TurnManager.plantCost
func getAllZombie()->Array[ZombieInstance]:
	var res:Array[ZombieInstance]
	for i in roadList:
		if i.zombie:
			res.append(i.zombie)
	return res
func getAllPlant()->Array[PlantInstance]:
	var res:Array[PlantInstance]
	for i in roadList:
		if i.plant2: res.append(i.plant2)
		if i.plant1: res.append(i.plant1)
	return res
func getAllIntroZombie()->Array[ZombieInstance]:
	var res:Array[ZombieInstance]
	for i in roadList:
		if i.zombie && !i.zombie.getBuffVal(Buff.Type.STONE):
			res.append(i.zombie)
	return res
func getZombieLine(data:ZombieInstance)->int:
	for i in range(roadList.size()):
		if data == roadList[i].zombie:
			return  i
	return -1
func getPlantLine(data:PlantInstance)->int:
	for i in range(roadList.size()):
		if data == roadList[i].plant2 || data == roadList[i].plant1 :
			return  i
	return -1
func getPlantCol(data:PlantInstance)->int:
	for i in range(roadList.size()):
		if data == roadList[i].plant2 : return 2
		if data == roadList[i].plant1 : return 1
	return -1
func getCanPlaceRoad(faction:PVZ.Type,type:BaseData.Type = BaseData.Type.Ground)->Array[DataRoad]:
	var res:Array[DataRoad]
	if faction == PVZ.Type.PLANT:
		for i in roadList:
			if !i.plant1 || !i.plant2:
				if !(i.type == Road.ROAD_TYPE.WATER_WAY && type == BaseData.Type.Ground):
					res.append(i)
	else:
		for i in roadList:
			if !i.zombie:
				if !(i.type == Road.ROAD_TYPE.WATER_WAY && type == BaseData.Type.Ground):
					res.append(i)
	return res
func endCQTurn():
	var CQ:CQData = CQData.new()
	CQ.type = CQData.Type.END_TURN
	CQList.append(CQ)
static func selectCard(arr,type:AIManager.CardType = AIManager.CardType.ALL)->Array:
	var res:Array
	for i:BaseInstance in arr:
		match type:
			AIManager.CardType.ALL:
				pass
			AIManager.CardType.TARGET_ONLY:
				if i.getCardType() == BaseData.CardType.TARGET:res.append(i)
			AIManager.CardType.PLAN_ENVIRONMENT_ONLY:
				if i.getCardType() != BaseData.CardType.TARGET:res.append(i)
	return res
func adjustDie():
	for i in roadList:
		if i.zombie && i.zombie.get_health() <= 0: i.zombie = null
		if i.plant1 && i.plant1.get_health() <= 0: i.plant1 = null
		if i.plant2 && i.plant2.get_health() <= 0: i.plant2 = null
