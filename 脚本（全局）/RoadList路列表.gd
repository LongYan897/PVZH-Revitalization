extends Node
@onready var list:Array[Road]
func addRoad(node:Node2D):
	list.append(node)
func getRoad(line:int):
	if line < 0 or line >= list.size():
		return null
	return list[line]
func getLinePosX(mLine:int):
	return 140+(mLine)*107
func getLinePosY(mCol:int = 1):
	return 690-200*(mCol-1)
func erase(node):
	if !node:return
	for i in list:
		if i.plant1 == node: 
			i.plant1 = null
		elif i.plant2 == node: 
			i.plant2 = null
		elif i.zombie == node: 
			i.zombie = null
