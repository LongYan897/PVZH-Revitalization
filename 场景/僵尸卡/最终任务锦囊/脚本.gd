extends BaseZombie
func planIntro():
	hide()
	if type == BaseTarget.Type.Display_only:
		show()
		state.set_animation("intro",false,10)
		return
	var tar = TurnManager.getAllTarget(PVZ.Type.PLANT)
	var node:BasePlant = await ChooseManager.choose(tar,PVZ.Type.ZOMBIE)
	if node == null or !is_instance_valid(node):
		push_warning("最终任务锦囊：未找到所选植物")
		return
	var zombieTar = RoadList.list[line].zombie
	state.set_animation("intro",false,10)
	show()
	await zombie.animation_completed
	position = node.position
	await get_tree().create_timer(0.5).timeout
	state.set_animation("hit",false,10)
	zombieTar.die()
	await zombie.animation_completed
	zombie.hide()
	var len = AQ.getlen()
	await node.hit(4)
	AQ.addThread(len)
	node.adjustDie()
	await get_tree().create_timer(5).timeout
func extraAnimationEvent(eventName):
	match eventName:
		"effect":
			var effect:Node2D = load("res://场景/粒子特效/粒子特效.tscn").instantiate()
			add_child(effect)
			var n_time:float = 0
			while n_time <= 2:
				n_time += get_process_delta_time()
				effect.position = zombie.get_skeleton().find_bone("骨骼").get_global_transform().origin\
				*4.5 + Vector2(-1600,-2800)
				await get_tree().process_frame
			effect.part.emitting = false
