extends Node2D
var fatherNode
var y:float = 0
@onready var label = $"标签"
func setFatherNode(node):
	fatherNode = node
func setY(m_y):
	y = m_y
func setLabel(str:String):
	label.text = str
func _process(_delta: float) -> void:
	if fatherNode == null:return
	position.y = y - fatherNode.rollPosY
