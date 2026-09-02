extends Resource
class_name CQData
enum Type{
	NULL = 0,
	CHOOSE_CARD,
	CHOOSE_TARGET,
	CHOOSE_CQ,
	POP_ZOMBIE_TARGET,
	POP_PLANT_TARGET,
	ZOMBIE_STONE_INTRO,
	END_TURN,
	START,
}
@export var plantInstance:PlantInstance = null
@export var zombieInstance:ZombieInstance = null
@export var type:Type = Type.NULL
@export var line:int = -1
@export var col:int = -1
@export var fromLine:int = 0
@export var team:PVZ.Type
func _init(data = null) -> void:
	if data == null:return
	if data is CQData:
		type = data.type
		line = data.line
		col = data.col
		if data.plantInstance:
			plantInstance = PlantInstance.new(data.plantInstance)
		if data.zombieInstance:
			zombieInstance = ZombieInstance.new(data.zombieInstance)
		fromLine = data.fromLine
		team = data.team
		return
func getData():
	if plantInstance:return plantInstance
	if zombieInstance:return zombieInstance
	return null
func _to_string() -> String:
	return str("CQData type:",Type.keys()[type].to_lower(),"\tline:",line,"\tcol:",col,"\tfromLine:",fromLine)
func dict():
	return {
		"sendType":"CQData",
		"type": int(type),
		"line": line,
		"col": col,
		"fromLine": fromLine,
		"plantInstance":plantInstance.dict() if plantInstance else null,
		"zombieInstance":zombieInstance.dict() if zombieInstance else null,
		"team":team,
	}
static func fromDict(data)->CQData:
	var cq = CQData.new()
	cq.type = int(data.get("type", Type.NULL))
	cq.line = int(data.get("line", 0))
	cq.col = int(data.get("col", 0))
	cq.fromLine = int(data.get("fromLine", 0))
	if data.plantInstance:
		cq.plantInstance = PlantInstance.from_dict(data.plantInstance)
	if data.zombieInstance:
		cq.zombieInstance = ZombieInstance.from_dict(data.zombieInstance)
	cq.team = int(data.get("team", 0))
	return cq
static func sendEndTurn():
		var cq = CQData.new()
		cq.type = CQData.Type.END_TURN
		WebSocketClient.chat(cq.dict())
