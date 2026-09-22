extends BaseTarget
class_name BasePlant
@export var plant_data: PlantData  # 引用上面的资源类型
@export var plantInstance: PlantInstance #实际资源

@onready var plant = $"植物/植物动画"
@onready var label = $"植物/伤害和血量"
@onready var area = $"碰撞体积"
@onready var state: SpineAnimationState = plant.get_animation_state()
@onready var dieEffect = $"死亡特效"
@onready var skeletonDataRes = plant.skeleton_data_res

func _ready():
	await get_tree().process_frame
	if plantInstance:label.updatePlantData(plantInstance,true)
	setup()
	bindSignal()
	intro()
	introAnimation()
func _process(delta: float) -> void:
	if isDrag == true:
		dragTime += delta
	else:
		if dragTime > 0:
			libraryIntro()
		dragTime = 0
func setup():
	dieEffect.hide()
	wink()
	if type == Type.Normal:
		add_to_group("plant")
	faction = PVZ.Type.PLANT
func intro():
	if type == Type.Display_only:return
	var len = AQ.getlen()
	AQ.addAction(trigger_intro_event)
	await AQ.addThread(len)
func introAnimation():
	cancelWink()
	state.set_animation("intro",false,10)
	await waitAnimationComP(10)
	state.set_animation("idle",true,10)
	afterIntro()
func hit(val:int):
	cancelWink()
	state.set_animation("hit",false,10)
	plantInstance.bonus_health -= val
	var offsetHP:int = 0
	if plantInstance.get_health() < 0:
		offsetHP = -plantInstance.get_health()
		plantInstance.bonus_health += offsetHP
	label.updatePlantData(plantInstance)
	label.hit(val - offsetHP)
	await waitAnimationComP(10)
	if state.get_track(10).get_animation().get_name() == "hit":
		state.set_animation("idle",true,10)
	var action=func():
		pass
	AQ.addAction(action)
func adjustDie():
	if plantInstance.get_health() <= 0:die()
func die():
	RoadList.erase(self)
	remove_from_group("plant")
	is_alive = false
	var action=func():
		trigger_die_event()
	AQ.addAction(action)
	while 1:
		if state.get_track(10).get_animation().get_name() == "idle":break
		await get_tree().process_frame
	cancelWink()
	state.set_animation("die",false,10)
	await waitAnimationComP(10)
	await get_tree().create_timer(0.3).timeout
	label.die()
	state.clear_track(10)
	await dieEffect.intro()
	self.queue_free()
func attack():
	if plantInstance.get_attack() == 0:return
	var action=func():
		cancelWink()
		await attackAnimation()
	AQ.addAction(action)
func attackAnimation():
		state.set_animation("attack",false,10)
		await waitAnimationComP(10)
		state.set_animation("idle",true,10)
func wink():
	while is_alive:
		var wtime:int = randi_range(2,4)
		await get_tree().create_timer(wtime).timeout
		var track0 = state.get_track(10)
		if track0 && track0.get_animation().get_name() == "idle":
			state.set_animation("wink",false,11)
func cancelWink():
	state.clear_track(11)
func setRoad(pos:Vector2i):
	line = pos.x
	col = pos.y
	position = Vector2(140+line*107,690-200*(col-1))
func loadResource(data:PlantInstance):
	plantInstance = PlantInstance.new(data)
	label.updatePlantData(plantInstance,true)
func getPosition(pos):
	return global_position
func trigger_intro_event():
	pass
func trigger_attack_event():
	var zombieNode:Node2D = RoadList.getRoad(line).getZombie()
	var zombiePosition
	if zombieNode:
		zombiePosition = zombieNode.getPosition(global_position)
		var attackNode = attackEffect.instantiate()
		add_child(attackNode)
		attackNode.loadAnimation(skeletonDataRes,false)
		await attackNode.posMove(position+Vector2(-45,-70),zombiePosition,0.40,BaseAttackEffect.Type.QUAD)
		zombieNode.hit(plantInstance.get_attack())
		await attackNode.posMove(zombiePosition,position+Vector2(-45,-70),0.5)
		attackNode.clear()
func trigger_die_event():
	pass
func bindSignal():
	area.mouse_entered.connect(func(): isOver = true)
	area.mouse_exited.connect(func(): isOver = false)
	plant.animation_event.connect(animationEvent)
func animationEvent(spine_sprite: Object, animation_state: Object, track_entry:Object, event:Object):
	var eventName = event.get_data().get_event_name()
	match eventName:
		"attack":
			trigger_attack_event()
		"die":
			var tween = create_tween()
			tween.tween_property(plant,"modulate",Color(1,1,1,0),0.3)\
			.set_trans(Tween.TRANS_QUAD)\
			.set_ease(Tween.EASE_IN_OUT)
		_:
			extraAnimationEvent(eventName)
func libraryIntro():
	var library = load("res://场景/UI对象/展示图鉴.tscn").instantiate()
	add_child(library)
	library.loadPlantRes(plantInstance.plantData)
	MessageBox.addMessageBox(library)
func _input(event: InputEvent) -> void:
	if ChooseManager.chooseCount != 0:return
	if type == Type.Display_only:return
	if !MessageBox.isEmpty():return
	
	if event is InputEventScreenTouch:
		if !event.pressed:
			isDrag = false
			return
		if isOver == false:return
		isDrag = true
	elif event is InputEventMouseButton && event.button_index == MOUSE_BUTTON_LEFT:
		if !isOver:return
		if event.is_pressed():
			isDrag = true
		else:
			isDrag = false
func waitAnimationComP(track:int = 0,precent:float = 1):
	var a = state.get_track(track)
	while a.get_animation_time() < a.get_animation_end() * precent - 0.05:
		await get_tree().process_frame
	await get_tree().create_timer(0.05).timeout
func extraAnimationEvent(eventName):
	pass
func setInstance(instance):
	plantInstance = instance
