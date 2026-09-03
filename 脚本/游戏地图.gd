extends Node2D
@onready var road1:Node2D = $"路1"
@onready var road2:Node2D = $"路2"
@onready var road3:Node2D = $"路3"
@onready var road4:Node2D = $"路4"
@onready var road5:Node2D = $"路5"
@onready var plantCost = $"玩家显示阳光脑子1"
@onready var zombieCost = $"玩家显示阳光脑子2"
func _ready() -> void:
	addRoadToGroup()
	setup()
	await get_tree().create_timer(0.1).timeout
	Enemy.addPlantTarget(3,"蔓越莓植物")
	#Enemy.addPlantTarget(2,"蔓越莓植物")
	#Enemy.addZombieTarget(2,"基础僵尸")
	Enemy.addZombieTarget(3,"爱尔兰小鬼僵尸")
	Enemy.addZombieTarget(2,"跳舞僵尸")
func addRoadToGroup():
	road1.add_to_group("road")
	road2.add_to_group("road")
	road3.add_to_group("road")
	road4.add_to_group("road")
	road5.add_to_group("road")
	road1.setLine(0)
	road2.setLine(1)
	road3.setLine(2)
	road4.setLine(3)
	road5.setLine(4)
	road1.setRoadType(Road.ROAD_TYPE.HIGH_GROUND)
	road2.setRoadType(Road.ROAD_TYPE.GROUND)
	road3.setRoadType(Road.ROAD_TYPE.GROUND)
	road4.setRoadType(Road.ROAD_TYPE.GROUND)
	road5.setRoadType(Road.ROAD_TYPE.WATER_WAY)
	RoadList.addRoad(road1)
	RoadList.addRoad(road2)
	RoadList.addRoad(road3)
	RoadList.addRoad(road4)
	RoadList.addRoad(road5)
func sendPlantCard(mname:String="基础植物"):
	print("添加卡牌 名字 ",name)
	var path = "res://场景/植物卡/"+mname+"/"+mname+".tscn"
	var instance = load(path).instantiate()
	add_child(instance)
	instance.setRoad(Vector2(3,1))
func sendZombieCard(mname:String="基础植物"):
	print("添加卡牌 名字 ",name)
	var path = "res://场景/僵尸卡/"+mname+"/"+mname+".tscn"
	var instance = load(path).instantiate()
	add_child(instance)
	instance.setRoad(Vector2(3,1))
func setup():
	TurnManager.plantTurnCost = 0
	TurnManager.zombieTurnCost = 0
	Enemy.gameNode = self
	TurnManager.m_state = TurnManager.Type.CARD_TURN
	plantCost.updateCost()
	zombieCost.updateCost()
