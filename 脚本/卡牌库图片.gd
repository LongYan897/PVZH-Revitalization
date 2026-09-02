extends Node2D
var fatherNode
var y:float = 0
@onready var sprite = $"图片"
func setFatherNode(node):
	fatherNode = node
func setY(m_y):
	y = m_y
func loadTexture(path):
	print("loadPath:",path)
	sprite.texture = load(path)
func _process(_delta: float) -> void:
	if fatherNode == null:return
	position.y = y - fatherNode.rollPosY
