extends BasePlant
func trigger_attack_event():
	var zombieNode:Node2D = RoadList.getRoad(line).getZombie()
	var zombiePosition
	if zombieNode:
		zombiePosition = zombieNode.getPosition(global_position)
		var attackNode:BaseAttackEffect = attackEffect.instantiate()
		add_child(attackNode)
		attackNode.loadAnimation(skeletonDataRes,false)
		attackNode.scale = Vector2(5,5)
		await attackNode.posMove(position+Vector2(-45,-90),zombiePosition,0.25,BaseAttackEffect.Type.QUAD)
		zombieNode.hit(plantInstance.get_attack())
		attackNode.loadAnimation(attackNode.ResPath.BASE1,true)
		
