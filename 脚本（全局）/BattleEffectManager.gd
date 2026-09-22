extends Node
var gameNode:Node = self
func _ready() -> void:
	Console.add_command("summon", consoleSummonTarget, ["mname", "line", "col"], 3, "测试召唤")
	Console.add_command("speed", consoleSpeed, ["scale"], 1, "设置游戏速度，例如 speed 2")

func consoleSpeed(value: String) -> void:
	var scale := value.to_float()
	if scale < 0.0:
		Console.print_error("speed 必须大于等于 0")
		return
	Engine.time_scale = scale
	Console.print_line("游戏速度已设置为 " + str(Engine.time_scale))
func summonTarget(instance:BaseTarget,line:int,col:int = 1):
	if instance == null:return
	gameNode.add_child(instance)
	if instance is BasePlant:
		(instance as BasePlant).setRoad(Vector2i(line,col))
		RoadList.getRoad(line).addPlant(instance,col)
		if TurnManager.team == 2:instance.position = Vector2(138+line*107,400)
	elif instance is BaseZombie:
		(instance as BaseZombie).setRoad(Vector2i(line,col))
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
	if instance is BasePlant:
		instance.setInstance(PlantInstance.new(mname))
	else:
		instance.setInstance(ZombieInstance.new(mname))
	summonTarget(instance, int(line), int(col))
# summon 豌豆射手植物 0 1
# summon 直升机小鬼僵尸 0 1
# summon 召唤直升机锦囊 0 1
