extends BaseZombieCard
func getCanPlaceTarger()->Array:
	var arr:Array[Node2D]
	if !TurnManager.getAllTarget(PVZ.Type.PLANT):return arr
	return TurnManager.getAllTarget(PVZ.Type.ZOMBIE)
