extends BaseData
class_name PlantData

@export var name: String = "plant"
@export var cost: int = 1
@export var attack: int = 1
@export var health: int = 1
@export var buffList:Array[Buff] = []
@export var type:Type = Type.Ground
@export var rare:CardManager.Rare = CardManager.Rare.Common
@export var properties:Array[CardManager.CardProperties] = [CardManager.CardProperties.PLANT]

func _init(data:PlantData = null) -> void:
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
func isEqual(data:PlantData)->bool:
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
static func from_dict(data)->PlantData:
	var name = data.name
	var plantData:PlantData = CardManager.getCardRes(name)
	plantData.buffList.clear()
	for buff in data.buffList:
		if buff is Buff:
			plantData.buffList.append(Buff.new(buff))
		else:
			plantData.buffList.append(Buff.from_dict(buff))
	return plantData
