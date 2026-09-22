extends BaseZombie
func planIntro():
	SoundManager.createSound("res://场景/僵尸卡/召唤直升机锦囊/intro_1.wav",SoundManager.Bus.EFFECT)
	if type == BaseTarget.Type.Display_only:
		state.set_animation("intro",false,10)
		return
	state.set_animation("intro",false,10)
	await waitAnimationComP(10,0.85)
	var instance:BaseZombie = CardManager.getTargetRes("直升机小鬼僵尸").instantiate()
	instance.setInstance(ZombieInstance.new("直升机小鬼僵尸"))
	BattleEffectManager.summonTarget(instance,line,col)
	await waitAnimationComP(10)
