extends Node
signal chooseTarget(node)
signal chooseCQ(cq)
var chooseCount:int = 0
var choosePath := "res://场景/UI对象/单位选择高亮.tscn"
var chooseArr:Array
func _ready() -> void:
	chooseTarget.connect(func(_node): clearChooseCount())
	chooseCQ.connect(addChooseArr)
func clearChooseCount():
	chooseCount = 0
func choose(array:Array,team:PVZ.Type):
	var len = array.size()
	if len == 1: return array[0]
	if (team == PVZ.Type.ZOMBIE && TurnManager.team == 2) || (team == PVZ.Type.PLANT && TurnManager.team == 1):
		var effect = load("res://场景/UI对象/单位选择高亮.tscn")
		for i in array:
			var instance:ChooseHighlight = load(choosePath).instantiate()
			instance.position = i.position
			instance.placeNode = i
			add_child(instance)
			instance.position = i.global_position
			instance.intro()
		var node:BaseTarget = (await chooseTarget).placeNode
		if TurnManager.attackMode == TurnManager.AttactMode.PLAYER:
			var cq = CQData.new()
			cq.type = CQData.Type.CHOOSE_CQ
			if node is BaseZombie:
				cq.zombieInstance = node.zombieInstance
			elif node is BasePlant:
				cq.plantInstance = node.plantInstance
			cq.line = node.line
			cq.col = node.col
			WebSocketClient.chat(cq.dict())
		return node
	else:
		var cq:CQData = await getArr()
		var instance = cq.getData()
		var line = cq.line
		var col = cq.col
		if instance is PlantInstance:
			return RoadList.list[line].getPlantCol(col)
		elif instance is ZombieInstance:
			return RoadList.list[line].getZombie()
		push_warning("CHOOSE_INSTANCE: 未找到对应的场景目标")
		return null
func addChooseArr(cq:CQData):
	chooseArr.append(cq)
func getArr():
	while 1:
		if !chooseArr.is_empty():
			var cq = chooseArr[0]
			chooseArr.remove_at(0)
			return cq
		await get_tree().process_frame
