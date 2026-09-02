extends Node
class_name Road

enum ROAD_TYPE {
	NULL = 0,
	HIGH_GROUND,
	GROUND,
	WATER_WAY,}

var type: ROAD_TYPE = ROAD_TYPE.NULL
var plant1: BasePlant = null
var plant2: BasePlant = null
var zombie: BaseZombie = null
var line:int = 0
var roadHighLight = load("res://场景/UI对象/地图高亮.tscn").instantiate()

func _init(data = null) -> void:
	if data == null:
		return
	elif data is Road:
		print("road的拷贝构造")
		type = data.type
		plant1 = data.plant1
		plant2 = data.plant2
		zombie = data.zombie
		line = data.line
func setRoadType(rtype:int):
	type = rtype
func getRoadType():
	return type
func setLine(val:int):
	line = val
func getLine():
	return line
func getPlantCount():
	return int(plant1 != null) + int(plant2 != null)
func getZombie():
	if zombie:return zombie
	return HeroManager.zombieHero
func getZombieTarget():
	return zombie
func addPlant(node:Node2D,col:int):
	if col == 1:plant1 = node
	if col == 2:plant2 = node
func addZombie(node:Node2D):
	zombie = node
func attack():
	roadHighLight = load("res://场景/UI对象/地图高亮.tscn").instantiate()
	add_child(roadHighLight)
	roadHighLight.setRoad(line)
	var listlen = AQ.getlen()
	#print("RoadLine:",line," ","attack")
	#print("RoadDepth:",listlen)
	if zombie:
		zombie.attack()
		await get_tree().process_frame
		await AQ.addThread(listlen)
	if plant1:plant1.attack()
	if plant2:plant2.attack()
	await get_tree().process_frame
	await AQ.addThread(listlen)
	await get_tree().create_timer(0.1).timeout
	if plant1 && plant1.plantInstance.get_health()<=0:plant1.die()
	if plant2 && plant2.plantInstance.get_health()<=0:plant2.die()
	if zombie && zombie.zombieInstance.get_health()<=0:zombie.die()
	#await get_tree().create_timer(0.2).timeout
	roadHighLight.clear()
func getForwardPlant():
	if plant2:return plant2
	elif plant1:return plant1
	else:
		print("find null plant")
		return null
func getForwardPlantTarget():
	if plant2:return plant2
	elif plant1:return plant1
	elif HeroManager.plantHero!= null:return HeroManager.plantHero
	else:
		print("find null plant")
		return null
func getPlantCol(col:int):
	if col == 1: return plant1
	elif col == 2: return plant2
