extends BaseZombie
func attackAnimation():
	#SoundManager.createSound("res://场景/僵尸卡/直升机小鬼僵尸/intro_1.mp3",SoundManager.Bus.EFFECT)
	if TurnManager.team == 1:
		state.set_animation("attack1",false,10)
		await waitAnimationComP(10)
		state.set_animation("idle",true,10)
	else:
		state.set_animation("attack",false,10)
		await waitAnimationComP(10)
		state.set_animation("idle",true,10)
func afterIntro():
	var entry = state.set_animation("swing",true,2)
	state.set_animation("lights",true,1)
	entry.set_additive(true)
	entry.set_alpha(1.0)
func extraAnimationEvent(eventName):
	match eventName:
		"introAudio":
			SoundManager.createSound("res://场景/僵尸卡/直升机小鬼僵尸/intro_1.mp3",SoundManager.Bus.EFFECT)
