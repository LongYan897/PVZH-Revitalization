extends Node
signal CardUpdatePos
var zombie
var plant
var gameNode:Node2D:
	set(value):
		gameNode = value
		BattleEffectManager.gameNode = value
@onready var cardList:Array[EnemyCard]
func addZombieTarget(line:int,mname:String,data:ZombieInstance=null):
	var path = "res://场景/僵尸卡/"+mname+"/"+mname+".tscn"
	print("add Enemy path:",path)
	zombie = load(path).instantiate()
	var roadNode = RoadList.getRoad(line)
	var zombieInstance:ZombieInstance
	if data == null:
		var dataPath = CardManager.cardPath.get(mname,"res://数据资源/僵尸数据/基础僵尸数据.tres")
		zombieInstance = ZombieInstance.new(load(dataPath))
	else:
		zombieInstance = data
	gameNode.add_child(zombie)
	zombie.setRoad(Vector2(line,3))
	zombie.loadResource(zombieInstance)
	if TurnManager.team == 1:
		zombie.position = Vector2(138+line*107,400)
	else:
		zombie.position = Vector2(138+line*107,690)
	zombie.name = mname+str(line)
	roadNode.addZombie(zombie)
func addPlantTarget(line:int,mname:String,data:PlantInstance=null):
	var path = "res://场景/植物卡/"+mname+"/"+mname+".tscn"
	print("add Enemy path:",path)
	plant = load(path).instantiate()
	var roadNode = RoadList.getRoad(line)
	var plantInstance:PlantInstance
	if data == null:
		var dataPath = "res://数据资源/植物数据/"+mname+"数据.tres"
		plantInstance = PlantInstance.new(load(dataPath))
	else:
		plantInstance = data
	gameNode.add_child(plant)
	plant.setRoad(Vector2(line,1))
	plant.loadResource(plantInstance)
	if TurnManager.team == 1:
		plant.position = Vector2(138+line*107,690)
	else:
		plant.position = Vector2(138+line*107,400)
	plant.name = mname+str(line)
	roadNode.addPlant(plant,1)
func addList(node:Node2D):
	print("enemyList add",node)
	cardList.append(node)
func removeList(node:Node2D):
	print("enemyList remove",node)
	cardList.erase(node)
func updateListNum():
	#print("enemyListNumber update")
	var num:int = 0
	for i in cardList:
		i.number = num
		num +=1
func placeCard(num:int,line:int,type:PVZ.Type):
	var cardData = cardList[num]
	var instance:BaseTarget
	await cardList[num].animation()
	if cardData.getData() is ZombieInstance && !cardData.getData().getBuffVal(Buff.Type.STONE):
		var cardInstance:BaseZombieCard = load(cardData.getData().getCardPath()).instantiate()
		instance = load(cardData.getData().getTargetPath()).instantiate()
		cardInstance.zombieDataInstance = cardData.zombieInstance
		add_child(cardInstance)
		await cardInstance.animationIntro(line,type)
		await get_tree().create_timer(0.1).timeout
		Enemy.gameNode.add_child(instance)
		instance.setRoad(Vector2(line,1))
		if cardData.getData().getCardType() == BaseData.CardType.TARGET:
			RoadList.list[line].zombie = instance
		instance.loadResource(cardData.getData())
		TurnManager.spendCost(PVZ.Type.ZOMBIE,cardData.getData().get_cost())
	elif cardData.getData() is PlantInstance:
		print("enemy cardData")
		var cardInstance:BasePlantCard = load(cardData.getData().getCardPath()).instantiate()
		instance = load(cardData.getData().getTargetPath()).instantiate()
		cardInstance.plantDataInstance = cardData.plantInstance
		add_child(cardInstance)
		await cardInstance.animationIntro(line,type)
		await get_tree().create_timer(0.1).timeout
		Enemy.gameNode.add_child(instance)
		instance.setRoad(Vector2(line,1))
		if cardData.getData().getCardType() == BaseData.CardType.TARGET:
			RoadList.list[line].plant1 = instance
		instance.loadResource(cardData.getData())
		TurnManager.spendCost(PVZ.Type.PLANT,cardData.getData().get_cost())
	elif cardData.getData() is ZombieInstance && cardData.getData().getBuffVal(Buff.Type.STONE):
		await cardData.animationPlace(line) 
		instance = load(cardData.getData().getTargetPath()).instantiate()
		await get_tree().create_timer(0.1).timeout
		Enemy.gameNode.add_child(instance)
		instance.setRoad(Vector2(line,1))
		if cardData.getData().getCardType() == BaseData.CardType.TARGET:
			RoadList.list[line].zombie = instance
		instance.loadResource(cardData.getData())
		TurnManager.spendCost(PVZ.Type.ZOMBIE,cardData.getData().get_cost())
		
	#决定放置位置
	if (TurnManager.team == 2 && type == PVZ.Type.PLANT) || (TurnManager.team == 1 && type == PVZ.Type.ZOMBIE):
		instance.position.y = 400
	elif (TurnManager.team == 1 && type == PVZ.Type.PLANT) || (TurnManager.team == 2 && type == PVZ.Type.ZOMBIE):
		instance.position.y = 690
func getCardNum(data):
	for i in range(cardList.size()):
		if cardList[i].getData().isEqual(data):
			print("enemy getCardNum:",i)
			return i
	return -1
func startAnimation(data,line:int,type:PVZ.Type):
	var fun = func():
		var num = getCardNum(data)
		if num < 0:
			push_warning("未在对方手牌中找到要打出的卡: %s" % data)
			return
		await placeCard(num,line,type)
	AQ.addAction(fun)
func popZombieInstance(line):
	if line < 0 or line >= RoadList.list.size():
		return
	var zombie = RoadList.list[line].zombie
	var instance:ZombieInstance = zombie.zombieInstance
	var cardRes = CardManager.getCardRes(instance.get_card_name())
	if cardRes == null:return
	var newInstance = ZombieInstance.new(cardRes)
	DrawCard.drawZombieCard(gameNode,newInstance,1,zombie.position)
	await get_tree().create_timer(0.3).timeout
	zombie.queue_free()
