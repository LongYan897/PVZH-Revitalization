extends BaseZombie
func trigger_intro_event():
	if TurnManager.team == 1:return
	var effect = load("res://场景/UI对象/单位选择高亮.tscn")
	var nodeArray = TurnManager.getAllTarget(PVZ.Type.ZOMBIE)
	var index:int = 0
	var chooseNode:ChooseHighlight
	for i:BaseZombie in nodeArray:
		if i != self && !i.zombieInstance.getBuffVal(Buff.Type.STONE):
			chooseNode = effect.instantiate()
			add_child(chooseNode)
			chooseNode.position = i.global_position
			chooseNode.chooseNode = i
			chooseNode.intro()
			index += 1
	if index >= 2:
		var node:Node2D = await ChooseManager.chooseTarget
		var chooseZombie:BaseZombie = node.chooseNode
		print("chooseZombie:",chooseZombie)
		var zombieData = chooseZombie.zombieInstance.get_card_name()
		var instance = ZombieInstance.new(CardManager.getCardRes(zombieData))
		var len = AQ.getlen()
		DrawCard.drawZombieCard(Enemy.gameNode,instance,1,chooseZombie.position)
		if TurnManager.attackMode == TurnManager.AttactMode.PLAYER:
			var cq = CQData.new()
			cq.type = CQData.Type.POP_ZOMBIE_TARGET
			cq.line = chooseZombie.line
			WebSocketClient.chat(cq.dict())
		await AQ.addThread(len)
		await get_tree().create_timer(0.3).timeout
		chooseZombie.queue_free()
	elif index == 1:
		ChooseManager.chooseTarget.emit(null)
		var node = chooseNode
		var chooseZombie:BaseZombie = node.chooseNode
		print("chooseZombie:",chooseZombie)
		var zombieData = chooseZombie.zombieInstance.get_card_name()
		var instance = ZombieInstance.new(CardManager.getCardRes(zombieData))
		var len = AQ.getlen()
		DrawCard.drawZombieCard(Enemy.gameNode,instance,1,chooseZombie.position)
		if TurnManager.attackMode == TurnManager.AttactMode.PLAYER:
			var cq = CQData.new()
			cq.type = CQData.Type.POP_ZOMBIE_TARGET
			cq.line = chooseZombie.line
			WebSocketClient.chat(cq.dict())
		await AQ.addThread(len)
		await get_tree().create_timer(0.3).timeout
		chooseZombie.queue_free()
func trigger_attack_event():
	var plantNode = RoadList.getRoad(line).getForwardPlantTarget()
	var plantPosition
	if plantNode :
		plantPosition = plantNode.getPosition(global_position)
		var attackNode:BaseAttackEffect = attackEffect.instantiate()
		add_child(attackNode)
		attackNode.loadAnimation(skeletonDataRes,false)
		await attackNode.posMove(position,plantPosition+Vector2(0,0),0.50,BaseAttackEffect.Type.LINEAR,true)
		var val = await plantNode.hit(zombieInstance.get_attack())
func extraReady():
	while 1:
		await get_tree().create_timer(randi_range(3,6)).timeout
		var track = state.get_track(0)
		if track == null || track.get_animation() == null:continue
		if track.get_animation().get_name() == "idle":
			state.set_animation("special1",false,0)
			await waitAnimationComP(0)
			state.set_animation("idle",true,0)
