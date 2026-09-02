extends BaseData
class_name ZombieData

@export var name: String = "zombie"
@export var cost: int = 1
@export var attack: int = 1
@export var health: int = 1
@export var buffList:Array[Buff] = []
@export var type:Type = Type.Ground
@export var rare:CardManager.Rare = CardManager.Rare.Common
@export var properties:Array[CardManager.CardProperties] = [CardManager.CardProperties.ZOMBIE]
func _init(data:ZombieData = null) -> void:
	if data == null:return
	name = data.name
	cost = data.cost
	attack = data.attack
	health = data.health
	for buff in data.buffList:
		buffList.append(Buff.new(buff))
	type = data.type
	rare = data.rare
	properties = data.properties.duplicate()
	cardType = data.cardType
func isEqual(data:ZombieData)->bool:
	if data == null:return false
	if name != data.name:return false
	return true
func dict():
	var buffDicts = []
	for buff in buffList:
		buffDicts.append(buff.dict())
	return {
		"name":name,
		"buffList":buffDicts,
		#这里不做完全复制
	}
static func from_dict(data)->ZombieData:
	var name = data.name
	var zombieData:ZombieData = CardManager.getCardRes(name)
	zombieData.buffList.clear()
	for buff in data.buffList:
		if buff is Buff:
			zombieData.buffList.append(Buff.new(buff))
		else:
			zombieData.buffList.append(Buff.from_dict(buff))
	return zombieData
