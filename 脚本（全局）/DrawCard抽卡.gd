extends Node
var drawInstance
var plantInstance:BasePlantCard
var zombieInstance:BaseZombieCard
var team1Pos = Vector2(360,700)
var team2Pos = Vector2(360,500)
func drawPlantCard(node:Node2D,data:PlantInstance,val:int = 1,pos = false):
	if node == null:return
	if pos is bool && pos == false:
		if TurnManager.team == 1:
			pos = team1Pos
		else:
			pos = team2Pos
	
	var res = func():
		for i in range(0,val):
			drawInstance = load("res://场景/抽卡特效/抽卡特效.tscn").instantiate()
			node.add_child(drawInstance)
			if TurnManager.team == 1:
				drawPlantCardInstance(data,node,i,val,pos)
				drawInstance.position = Vector2((i-(val-1)/2.0)*180,0) + pos
			else:
				enemyDrawPlantCardInstance(data,node,i,val,pos)
				drawInstance.position = Vector2((i-(val-1)/2.0)*180,0) + pos
		drawPlantCardInstanceEmit()
	AQ.addAction(res)
func drawPlantCardInstance(data:PlantInstance,node:Node2D,num:int,val:int,pos):
	await get_tree().create_timer(0.35).timeout
	sound()
	var path = "res://场景/植物卡组/"+data.get_card_name()+"/"+data.get_card_name()+".tscn"
	print("load plant card from:",path)
	plantInstance = load(path).instantiate()
	plantInstance.plantDataInstance = data
	node.add_child(plantInstance)
	plantInstance.name = "card_"+data.get_card_name()
	plantInstance.position = Vector2((num-(val-1)/2.0)*180,0) + pos
	if CardManager.getCardCountAll() < 4:
		CardManager.addCardList1(plantInstance)
	elif CardManager.getCardCount1() == 4 && CardManager.getCardCount2() == 0:
		var cnode = CardManager.cardList1[3]
		CardManager.moveList(cnode,2)
		CardManager.addCardList2(plantInstance)
	elif CardManager.getCardCount1()-CardManager.getCardCount2() == 0:
		CardManager.addCardList1(plantInstance)
	elif CardManager.getCardCount1()-CardManager.getCardCount2() != 0:
		CardManager.addCardList2(plantInstance)
	plantInstance.setCardNumber()
func drawPlantCardInstanceEmit():
	await get_tree().create_timer(1).timeout
	CardManager.cardOrderSort.emit()
func drawZombieCard(node:Node2D,data:ZombieInstance,val:int = 1,pos = false):
	if node == null:return
	if pos is bool && pos == false:
		if TurnManager.team == 1:
			pos = team2Pos
		else:
			pos = team1Pos
	var res = func():
		for i in range(0,val):
			drawInstance = load("res://场景/抽卡特效/抽卡特效.tscn").instantiate()
			node.add_child(drawInstance)
			if TurnManager.team == 1:
				enemyDrawZombieCardInstance(data,node,i,val,pos)
				drawInstance.position = Vector2((i-(val-1)/2.0)*180,0) + pos
			else:
				drawZombieCardInstance(data,node,i,val,pos)
				drawInstance.position = Vector2((i-(val-1)/2.0)*180,0) + pos
		drawZombieCardInstanceEmit()
	AQ.addAction(res)
func drawZombieCardInstance(data:ZombieInstance,node:Node2D,num:int,val:int,pos):
	await get_tree().create_timer(0.35).timeout
	sound()
	var path = "res://场景/僵尸卡组/"+data.get_card_name()+"/"+data.get_card_name()+".tscn"
	print("load zombie card from:",path)
	zombieInstance = load(path).instantiate()
	zombieInstance.zombieDataInstance = data
	node.add_child(zombieInstance)
	zombieInstance.name = "card_"+data.get_card_name()
	zombieInstance.position = Vector2((num-(val-1)/2.0)*180,0) + pos
	if CardManager.getCardCountAll() < 4:
		CardManager.addCardList1(zombieInstance)
	elif CardManager.getCardCount1() == 4 && CardManager.getCardCount2() == 0:
		var cnode = CardManager.cardList1[3]
		CardManager.moveList(cnode,2)
		CardManager.addCardList2(zombieInstance)
	elif CardManager.getCardCount1()-CardManager.getCardCount2() == 0:
		CardManager.addCardList1(zombieInstance)
	elif CardManager.getCardCount1()-CardManager.getCardCount2() != 0:
		CardManager.addCardList2(zombieInstance)
	zombieInstance.setCardNumber()
func drawZombieCardInstanceEmit():
	await get_tree().create_timer(1).timeout
	print("cardOrderSort")
	CardManager.cardOrderSort.emit()
func enemyDrawPlantCardInstance(data:PlantInstance,node:Node2D,num:int,val:int,pos):
	await get_tree().create_timer(0.35).timeout
	sound()
	var path = "res://场景/UI对象/对方卡牌.tscn"
	print("load plant card from:",path)
	var plantInstance = load(path).instantiate()
	node.add_child(plantInstance)
	plantInstance.name = "enemyCard_"+data.get_card_name()
	plantInstance.position = Vector2((num-(val-1)/2.0)*180,0) + pos
	plantInstance.plantInstance = data
	await get_tree().create_timer(0.5).timeout
	Enemy.updateListNum()
	plantInstance.updatePos()
func enemyDrawZombieCardInstance(data:ZombieInstance,node:Node2D,num:int,val:int,pos):
	await get_tree().create_timer(0.35).timeout
	sound()
	var path = "res://场景/UI对象/对方卡牌.tscn"
	print("load zombie card from:",path)
	var zombieInstance = load(path).instantiate()
	node.add_child(zombieInstance)
	zombieInstance.name = "enemyCard_"+data.get_card_name()
	zombieInstance.position = Vector2((num-(val-1)/2.0)*180,0) + pos
	zombieInstance.zombieInstance = data
	await get_tree().create_timer(0.5).timeout
	Enemy.updateListNum()
	zombieInstance.updatePos()
func sound():
	SoundManager.createSound("res://场景/抽卡特效/抽牌.wav",SoundManager.Bus.EFFECT)
