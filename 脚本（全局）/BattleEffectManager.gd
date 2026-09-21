extends Node
var gameNode:Node = self
var cache
func _ready() -> void:
	Console.add_command("summon", consoleSummonTarget, ["mname", "line", "col"], 3, "测试召唤")
func summonTarget(instance:BaseTarget,line:int,col:int = 1):
	if instance == null:return
	gameNode.add_child(instance)
	if instance is BasePlant:
		(instance as BasePlant).setRoad(Vector2i(line,col))
		instance.loadResource(PlantInstance.new(cache))
		RoadList.getRoad(line).addPlant(instance,col)
		if TurnManager.team == 2:instance.position = Vector2(138+line*107,400)
	elif instance is BaseZombie:
		(instance as BaseZombie).setRoad(Vector2i(line,col))
		instance.loadResource(ZombieInstance.new(cache))
		RoadList.getRoad(line).addZombie(instance)
		if TurnManager.team == 1:instance.position = Vector2(138+line*107,400)
	else:
		push_error("summonTarget：场景根节点不是 BasePlant 或 BaseZombie：" + instance.name)
		instance.queue_free()
func consoleSummonTarget(mname:String,line:String,col:String):
	var res = CardManager.getTargetRes(mname)
	print("consoleSummonTarget res:",res)
	if !res:return
	var instance:BaseTarget = res.instantiate()
	cache = CardManager.getCardRes(mname)
	summonTarget(instance, int(line), int(col))
# summon 豌豆射手植物 0 1
# summon 直升机小鬼僵尸 0 1
# summon 召唤直升机锦囊 0 1
