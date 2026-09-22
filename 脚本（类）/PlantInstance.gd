extends BaseInstance
class_name PlantInstance
var plantData:PlantData
var bonus_attack:int =0
var bonus_health:int =0
var bonus_cost:int =0
var bonus_buffList:Array[Buff] = []
func _init(data):
	if data is String:
		data = CardManager.getCardRes(data)
	if data is PlantData:
		#print("create cardInstance from ",data.name)
		plantData = PlantData.new(data)
	elif data is PlantInstance:
		#print("create cardInstance from ",data.plantData.name)
		plantData = PlantData.new(data.plantData)
		bonus_attack = data.bonus_attack
		bonus_health = data.bonus_health
		bonus_cost = data.bonus_cost
		for buff in data.bonus_buffList:
			bonus_buffList.append(Buff.new(buff))
func get_attack()->int:
	return bonus_attack+plantData.attack
func get_health()->int:
	return bonus_health+plantData.health
func get_cost()->int:
	return bonus_cost+plantData.cost
func get_cost_Ori()->int:
	return plantData.cost
func get_card_name()->String:
	return plantData.name
func getBuffVal(type:Buff.Type):
	for i in plantData.buffList:
		if i.type == type:
			return i.val
	for i in bonus_buffList:
		if i.type == type:
			return i.val
	return null
func getStrPro():
	var res:String
	res = "name:"+str(get_card_name())+" at:"+str(get_attack())+" hp:"+str(get_health())+" cost:"+str(get_cost())
	return res
func getTargetPath()->String:
	return "res://场景/植物卡/"+plantData.name+"/"+plantData.name+".tscn"
func getCardPath()->String:
	return "res://场景/植物卡组/"+plantData.name+"/"+plantData.name+".tscn"
func getCardType()->BaseData.CardType:
	return plantData.cardType
func getData()->BaseData:
	return plantData
func isEqual(data)->bool:
	if data == null:return false
	if !plantData.isEqual(data.plantData):return false
	if bonus_attack != data.bonus_attack:return false
	if bonus_health != data.bonus_health:return false
	if bonus_cost != data.bonus_cost:return false
	if bonus_buffList.size() != data.bonus_buffList.size():return false
	for i in range(bonus_buffList.size()):
		if !bonus_buffList[i].isEqual(data.bonus_buffList[i]):return false
	return true
func dict():
	var buffDicts = []
	for buff in bonus_buffList:
		buffDicts.append(buff.dict())
	return {
		"plantData":plantData.dict(),
		"bonus_attack":bonus_attack,
		"bonus_health":bonus_health,
		"bonus_cost":bonus_cost,
		"bonus_buffList":buffDicts,
	}
static func from_dict(data)->PlantInstance:
	var buffDicts:Array[Buff]
	for buff in data.bonus_buffList:
		if buff is Buff:
			buffDicts.append(Buff.new(buff))
		else:
			buffDicts.append(Buff.from_dict(buff))
	var instance = PlantData.from_dict(data.plantData)
	var plantInstance = PlantInstance.new(instance)
	plantInstance.bonus_attack = data.bonus_attack
	plantInstance.bonus_health = data.bonus_health
	plantInstance.bonus_cost = data.bonus_cost
	plantInstance.bonus_buffList = buffDicts
	return plantInstance
