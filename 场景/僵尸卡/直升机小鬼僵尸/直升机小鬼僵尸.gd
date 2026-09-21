extends BaseZombie
func attackAnimation():
	SoundManager.createSound("res://场景/僵尸卡/直升机小鬼僵尸/intro_1.wav",SoundManager.Bus.EFFECT)
	if TurnManager.team == 1:
		state.set_animation("attack1",false,0)
		await waitAnimationComP(0)
		state.set_animation("idle",true,0)
	else:
		state.set_animation("attack",false,0)
		await waitAnimationComP(0)
		state.set_animation("idle",true,0)
