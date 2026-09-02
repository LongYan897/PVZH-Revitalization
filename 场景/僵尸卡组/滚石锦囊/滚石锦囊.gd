extends BaseZombieCard
func getCanPlaceTarger()->Array[Node2D]:
	var arr:Array[Node2D]
	for i:BasePlant in TurnManager.getAllTarget(PVZ.Type.PLANT):
		if i.plantInstance.get_attack() <= 2:
			arr.append(i)
	return arr
