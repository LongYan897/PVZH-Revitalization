extends Node2D
var fatherNode
var y:float = 0
@onready var sprite:SpineSprite = $"动画"
func setFatherNode(node):
	fatherNode = node
func setY(m_y):
	y = m_y
func loadTexture(path):
	print("loadPath:",path)
	sprite.skeleton_data_res = load(path)
func _process(_delta: float) -> void:
	if fatherNode == null:return
	position.y = y - fatherNode.rollPosY
func setAnimation(mname:String):
	sprite.get_animation_state().set_animation(mname,false,0)
