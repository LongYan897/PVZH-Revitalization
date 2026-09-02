extends BasePlant
func attackAnimation():
	if TurnManager.team == 1:
		state.set_animation("attack",false,0)
	else:
		state.set_animation("attack1",false,0)
	await waitAnimationComP(0)
	state.set_animation("idle",true,0)
func trigger_attack_event():
	var zombie = RoadList.getRoad(line).getZombie()
	if zombie:
		var effect:BaseAttackEffect = attackEffect.instantiate()
		add_child(effect)
		effect.loadSprite("res://场景/植物卡/豌豆射手植物/peashooter_projectile #177900.png")
		var offset:Vector2
		if TurnManager.team == 1:
			offset = Vector2(50,-40)
		else:
			offset = Vector2(50,40)
		effect.scale = 10 * Vector2(1,1)
		await effect.posMove(position+offset,zombie.getPosition(position),0.3,BaseAttackEffect.Type.LINEAR)
		effect.queue_free()
		zombie.hit(plantInstance.get_attack())
		var hitEffect:BaseAttackEffect = attackEffect.instantiate()
		add_child(hitEffect)
		hitEffect.loadAnimation(BaseAttackEffect.ResPath.BASE1,true)
		hitEffect.global_position = zombie.getPosition(position)
