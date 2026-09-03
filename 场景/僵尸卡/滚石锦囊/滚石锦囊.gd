extends BaseZombie
func planIntro():
	hide()
	if type == BaseTarget.Type.Display_only:
		show()
		state.set_animation("intro",false,0)
		return
func trigger_attack_event():
	#print("line:",line,"col:",col)
	var plant = RoadList.getRoad(line).getPlantCol(col)
	plant.die()
func introEvent():
	SoundManager.createSound("res://场景/僵尸卡/滚石锦囊/锦囊-滚石.wav",SoundManager.Bus.EFFECT)
