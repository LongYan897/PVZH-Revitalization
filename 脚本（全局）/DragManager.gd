extends Node
var allTargets: Array = []
var drawNode:Node2D = null
var chooseColor:Color = Color(0.125, 1.0, 0.027,1)

func getAllPlaceTarget()->Array[Node2D]:
	var res:Array[Node2D]
	res.append_array(get_tree().get_nodes_in_group("road"))
	#res.append_array(get_tree().get_nodes_in_group("target"))
	res.append_array(TurnManager.getAllTarget())
	res.append_array(get_tree().get_nodes_in_group("globalPlace"))
	print("getAllPlaceTarget:",res)
	return res
func getRoadPlant(node:Node2D):
	if !node.is_in_group("road"):return
