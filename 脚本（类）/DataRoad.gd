extends Resource
class_name DataRoad

var type: Road.ROAD_TYPE = Road.ROAD_TYPE.NULL
var plant1: PlantInstance = null
var plant2: PlantInstance = null
var zombie: ZombieInstance = null
var line:int = 0

func _init(data = null) -> void:
	if data == null:return
	if data is Road:
		if  data.plant1:
			plant1 = PlantInstance.new(data.plant1.plantInstance)
		if  data.plant2:
			plant2 = PlantInstance.new(data.plant2.plantInstance)
		if  data.zombie:
			zombie = ZombieInstance.new(data.zombie.zombieInstance)
		type = data.type
		line = data.line
		return
	if data is DataRoad:
		if  data.plant1:
			plant1 = PlantInstance.new(data.plant1)
		if  data.plant2:
			plant2 = PlantInstance.new(data.plant2)
		if  data.zombie:
			zombie = ZombieInstance.new(data.zombie)
		type = data.type
		line = data.line
		return
func copy():
	var res:DataRoad = DataRoad.new()
	res.plant1 = PlantInstance.new(plant1)
	res.plant2 = PlantInstance.new(plant2)
	res.zombie = ZombieInstance.new(zombie)
	res.type = type
	res.line = line
	return res
func removeTar(data):
	if data == null:return
	if data is PlantInstance:
		if plant1 == data: plant1 = null
		elif plant2 == data: plant2 = null
	else:
		if zombie == data: zombie = null
