extends BaseInstance
class_name ZombieInstance
var zombieData:ZombieData
var bonus_attack:int =0
var bonus_health:int =0
var bonus_cost:int =0
var bonus_buffList:Array[Buff] = []
func _init(data):
	if data is String:
		data = CardManager.getCardRes(data)
	if data is ZombieData:
		#print("create cardInstance from ",data.name)
		zombieData = ZombieData.new(data)
	elif data is ZombieInstance:
		#print("create cardInstance from ",data.zombieData.name)
		zombieData = ZombieData.new(data.zombieData)
		bonus_attack = data.bonus_attack
		bonus_health = data.bonus_health
		bonus_cost = data.bonus_cost
		for buff in data.bonus_buffList:
			bonus_buffList.append(Buff.new(buff))
func get_attack()->int:
	return bonus_attack+zombieData.attack
func get_health()->int:
	return bonus_health+zombieData.health
func get_cost()->int:
	return bonus_cost+zombieData.cost
func get_cost_Ori()->int:
	return zombieData.cost
func get_card_name()->String:
	return zombieData.name
func getBuffVal(type:Buff.Type):
	for i in zombieData.buffList:
		if i.type == type:
			return i.val
	for i in bonus_buffList:
		if i.type == type:
			return i.val
	return null
func deleteBuff(type:Buff.Type):
	for i in range(zombieData.buffList.size()-1,-1,-1):
		if zombieData.buffList[i].type == type:
			zombieData.buffList.remove_at(i)
	for i in range(bonus_buffList.size()-1,-1,-1):
		if bonus_buffList[i].type == type:
			bonus_buffList.remove_at(i)
func getStrPro():
	var res:String
	res = "name:"+str(get_card_name())+" at:"+str(get_attack())+" hp:"+str(get_health())+" cost:"+str(get_cost())
	return res
func getTargetPath()->String:
	return "res://场景/僵尸卡/"+zombieData.name+"/"+zombieData.name+".tscn"
func getCardPath()->String:
	return "res://场景/僵尸卡组/"+zombieData.name+"/"+zombieData.name+".tscn"
func getCardType()->BaseData.CardType:
	return zombieData.cardType
func getData()->BaseData:
	return zombieData
func isEqual(data)->bool:
	if data == null:return false
	if !zombieData.isEqual(data.zombieData):return false
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
		"zombieData":zombieData.dict(),
		"bonus_attack":bonus_attack,
		"bonus_health":bonus_health,
		"bonus_cost":bonus_cost,
		"bonus_buffList":buffDicts,
	}
static func from_dict(data)->ZombieInstance:
	var buffDicts:Array[Buff]
	for buff in data.bonus_buffList:
		if buff is Buff:
			buffDicts.append(Buff.new(buff))
		else:
			buffDicts.append(Buff.from_dict(buff))
	var instance = ZombieData.from_dict(data.zombieData)
	var zombieInstance = ZombieInstance.new(instance)
	zombieInstance.bonus_attack = data.bonus_attack
	zombieInstance.bonus_health = data.bonus_health
	zombieInstance.bonus_cost = data.bonus_cost
	zombieInstance.bonus_buffList = buffDicts
	return zombieInstance
