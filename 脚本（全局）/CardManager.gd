extends Node
const plantCardPath = "res://数据资源/植物数据/"
const zombieCardPath = "res://数据资源/僵尸数据/"
@export var cardList1:Array[Node2D]
@export var cardList2:Array[Node2D]
signal cardOrderSort
var cardPath:Dictionary = {}
var cardGroup:Dictionary = {}
enum CardType{
	NULL = 0,
	LIBRARY,
	ANIMATION,
}
enum Rare{
	Common = 0,
	Uncommon,
	Rare,
	SuperRare,
	Legendary,
	Event,
}
enum Properties{
	BRAINY = 0,
	GUARDIAN,
	HEARTY,
	HUNGRY,
	KABLOOM,
	MADCAP,
	MEGAGROW,
	SMARTY,
	SNEAKY,
	SOLAR,
}
enum CardProperties{
	PLANT = 0,
	ZOMBIE,
	CLASS,
	TREE,
	BERRY,
}
var TranslateCardProperties:Dictionary = {
	CardProperties.PLANT:"植物",
	CardProperties.ZOMBIE:"僵尸",
	CardProperties.CLASS:"职业",
	CardProperties.TREE:"树木",
	CardProperties.BERRY:"莓果",
}
var cardDescription:Array[Dictionary] = [
	{"name":"NULL","showName":"NULL","rareLabel":"NULL","description":"NULL","story":"NULL","properties":Properties.SNEAKY},
	{"name":"消防员僵尸","showName":"消防员僵尸","rareLabel":"高级-常见","description":"墓碑\n现身：弹射另一只僵尸。","story":"NULL","properties":Properties.SNEAKY},
	{"name":"基础僵尸","showName":"基础僵尸","rareLabel":"高级-常见","description":"你不应该看到这个","story":"NULL","properties":Properties.SNEAKY},
	{"name":"基础植物","showName":"基础植物","rareLabel":"高级-常见","description":"你不应该看到这个","story":"NULL","properties":Properties.SMARTY},
	{"name":"蔓越莓植物","showName":"蔓越莓植物","rareLabel":"高级-超稀有","description":"该文案还在设计中，嗯","story":"NULL","properties":Properties.SMARTY},
]
func _ready() -> void:
	scanCards()
	initGroup()
	addCardsGroup()
	var array:Array[CardProperties] = [CardProperties.ZOMBIE,CardProperties.CLASS]
	selectByPro(array)
func scanCards():
	cardPath.clear()
	var dir1 = DirAccess.open(plantCardPath)
	var dir2 = DirAccess.open(zombieCardPath)
	scanFolder(dir1,plantCardPath)
	scanFolder(dir2,zombieCardPath)
func scanFolder(dir:DirAccess,path:String):
	dir.list_dir_begin()
	var fileName = dir.get_next()
	while fileName != "":
		var fullName = path+fileName
		print("fullName:",fullName)
		if dir.current_is_dir():
			var newDir = DirAccess.open(fullName+"/")
			scanFolder(newDir,fullName+"/")
		else:
			if fileName.ends_with(".tres") || fileName.ends_with(".tres.remap"):
				print("找到资源name:",fileName)
				var loadPath = fullName
				if loadPath.ends_with(".remap"):
					loadPath = loadPath.trim_suffix(".remap")
				var cardData = load(loadPath)
				var cardName = cardData.get("name")
				if cardPath.has(cardName):
					print("error:重复的卡牌名字")
					print("error:名字:",cardName)
					print("error:已有路径:",cardPath[cardName])
					print("error:新路径:",fullName)
					assert(false, "卡牌名字重复:"+cardName)
				cardPath[cardName] = loadPath
				
		fileName = dir.get_next()
func initGroup():
	cardGroup.clear()
	for i in CardProperties.values():
		var proName = CardProperties.keys()[i].to_lower()
		cardGroup[proName] = []
	cardGroup["cost0"] = []
	cardGroup["cost1"] = []
	cardGroup["cost2"] = []
	cardGroup["cost3"] = []
	cardGroup["cost4"] = []
	cardGroup["cost5"] = []
	cardGroup["cost6"] = []
	cardGroup["cost7"] = []
	cardGroup["cost8"] = []
	cardGroup["cost9"] = []
	cardGroup["cost10"] = []
	cardGroup["cost11"] = []
	print("创建了",cardGroup.size(),"个分组")
func addCardsGroup():
	for key in cardPath:
		var card = load(cardPath[key])
		for pro in card.properties:
			var proName = CardProperties.keys()[pro].to_lower()
			cardGroup[proName].append(key)
		cardGroup["cost"+str(card.cost)].append(key)
func addCardList1(node:Node2D):
	print("cardList1 add ",node.name)
	cardList1.append(node)
func addCardList2(node:Node2D):
	print("cardList2 add ",node.name)
	cardList2.append(node)
func getCardCount1()->int:
	return cardList1.size()
func getCardCount2()->int:
	return cardList2.size()
func getCardCountAll()->int:
	return cardList1.size() + cardList2.size()
func getLine(node:Node2D)->int:
	if cardList1.has(node):return 1
	if cardList2.has(node):return 2
	return -1
func getCol(node:Node2D)->int:
	if cardList1.find(node)!=-1:return cardList1.find(node)
	if cardList2.find(node)!=-1:return cardList2.find(node)
	return -1
func cardListErase(node:Node2D):
	if node.c_Line == 1:
		cardList1.erase(node)
		print("cardErase from list1")
	if node.c_Line == 2:
		cardList2.erase(node)
		print("cardErase from list2")
func moveList(node:Node2D,listNumber:int):
	if listNumber == 1:
		cardListErase(node)
		addCardList1(node)
		print("move CardList 1",node.name)
	if listNumber == 2:
		cardListErase(node)
		addCardList2(node)
		print("move CardList 2",node.name)
func cardListOrderSort():
	if getCardCountAll() <= 4:
		cardList1.append_array(cardList2)
		cardList2.clear()
	elif cardList1.size() - cardList2.size() > 1:
		cardList2.insert(0,cardList1[cardList1.size()-1])
		cardList1.remove_at(cardList1.size()-1)
	elif cardList2.size() - cardList1.size() > 0:
		cardList1.insert(cardList2.size()-1,cardList2[0])
		cardList2.remove_at(0)
	CardManager.cardOrderSort.emit()
func getCardRes(mname:String):
	#print("cardres查找名:", mname)
	#print("cardres已有key:", cardPath.keys())
	var res:String = "NULL"
	res = cardPath.get(mname,"NULL")
	if res == "NULL":
		print("error getCardRes fail")
		return null
	return load(res)
func findCardScene(type:PVZ.Type,name:String):
	if type == PVZ.Type.PLANT:
		return "res://场景/植物卡组/"+name+"/"+name+".tscn"
	elif type == PVZ.Type.ZOMBIE:
		return "res://场景/僵尸卡组/"+name+"/"+name+".tscn"
func selectByPro(array:Array[CardProperties])->Array:
	array = array.duplicate()
	var res:Array
	if array.is_empty():
		print("属性列表为空")
		return res
	var firstPro = array[0]
	var proFirstName = CardProperties.keys()[firstPro].to_lower()
	for i in cardGroup[proFirstName]:
		res.append(i)
		print("selectByPro addArrayName:",i)
	array.remove_at(0)
	for i in array:
		var proName = CardProperties.keys()[i].to_lower()
		for num in range(res.size()-1,-1,-1):
			var block = res[num]
			if !cardGroup[proName].has(block):
				print("selectByPro removeName:",res[num])
				res.remove_at(num)
	if res.is_empty():
		print("selectByPro res = 空，没有任何交集属性",)
	else:
		print("selectByPro res:",res)
	return res
func getcardDesDiction(mname:String):
	for i in cardDescription:
		if i.get("name") == mname:
			return i
	return cardDescription[0]
