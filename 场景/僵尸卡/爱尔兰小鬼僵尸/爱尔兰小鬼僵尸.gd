extends BaseZombie
func trigger_attack_event():
	var plantNode = RoadList.getRoad(line).getForwardPlantTarget()
	var plantPosition
	if plantNode :
		var val = await plantNode.hit(zombieInstance.get_attack())

func attackAnimation():
	if TurnManager.team == 2:
		state.set_animation("attack",false,0)
	else:
		state.set_animation("attack1",false,0)
	await waitAnimationComP(0)
	state.set_animation("idle",true,0)
