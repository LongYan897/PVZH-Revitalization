extends Node2D
class_name BaseAttackEffect
@onready var animation:SpineSprite = $"动画"
@onready var sprite:Sprite2D = $"图片"
enum Type{
	LINEAR = 0,
	QUAD,
}
enum ResPath{
	BASE1 = 0,
}
static var path:Dictionary = {
	ResPath.BASE1:"res://场景/攻击特效/Base1/Base1.tres",
}
func _ready() -> void:
	hide()
	animation.skeleton_data_res = load("res://数据资源/植物动画/基础植物动画.tres")
func loadAnimation(data,clear:bool = false):
	sprite.hide()
	animation.show()
	if data is String:
		data = load(data)
	if data is ResPath:
		setScale(data)
		data = load(path[data])
	animation.skeleton_data_res = data
	show()
	animation.get_animation_state().set_animation("attackEffect",false,0)
	await animation.animation_completed
	if clear:
		clear()
func loadSprite(data):
	sprite.show()
	animation.hide()
	sprite.texture = load(data)
	show()
func setScale(data):
	match data:
		ResPath.BASE1:
			global_scale = 0.4 * Vector2(1,1)
func posMove(curPos:Vector2,tarPos:Vector2,time:float = 0.5,type:Type = Type.LINEAR,autoRotation:bool = false):
	#print("curPos:",curPos)
	#print("tarPos:",tarPos)
	global_position = curPos
	if autoRotation == true:
		rotation = (tarPos - curPos).normalized().angle() + deg_to_rad(90)
	var tween = create_tween()
	if type == Type.LINEAR:
		tween.tween_property(self,"global_position",tarPos,time).\
		set_trans(Tween.TRANS_LINEAR).set_ease(Tween.EASE_IN_OUT)
	elif type == Type.QUAD:
		tween.tween_property(self,"global_position",tarPos,time).\
		set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	await tween.finished
func clear():
	queue_free()
