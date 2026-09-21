extends BaseZombie
func planIntro():
	if type == BaseTarget.Type.Display_only:
		state.set_animation("intro",false,0)
		return
	state.set_animation("intro",false,0)
	await waitAnimationComP(0,0.7)
	var instance = CardManager.getTargetRes("直升机小鬼僵尸").instantiate()
	BattleEffectManager.summonTarget(instance,line,col)
	await waitAnimationComP(0)
	queue_free()
