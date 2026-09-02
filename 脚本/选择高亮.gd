extends Node2D
class_name ChooseHighlight
enum choice{
	ROAD,
	TARGET,
	ALL
}
@onready var sprite = $"ExSprite/高亮"
@onready var ExSprite = $ExSprite
@onready var area = $"检测"
@onready var hover:bool = false
@onready var lock:bool = false
@onready var m_pos:Vector2 = Vector2(0,0)
@export var hoverScale:Vector2 = Vector2(1.2,1.2)
@export var commonScale:Vector2 = Vector2(1,1)
var tween:Tween
var tween1:Tween
var chooseNode
var placeNode:Node2D
var ExSpriteSca:float = 1
func _ready() -> void:
	top_level = true
	add_to_group("choice")
	bindSignal()
	_readyExtra()
func setType(type:int):
	match type:
		choice.ROAD:
			print("choice Road create")
			sprite.texture = load("res://素材/对象高亮框/单位.png")
func setRoad(pos:Vector2):
	var x = pos.x
	var y = pos.y-1
	position = Vector2(138+x*107,720-200*y)
	m_pos = pos 
	#print("highlight pos ",position)
func intro():
	tween = create_tween()
	sprite.modulate = Color(1,1,1,1)
	sprite.scale = Vector2(0,0)
	tween.tween_property(sprite,"scale",commonScale,0.5).\
	set_trans(Tween.TRANS_BOUNCE).set_ease(Tween.EASE_OUT)
	await tween.finished
func introAnimation1():
	if tween.is_running():
		tween.kill()
	tween = create_tween()
	tween.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	sprite.modulate = Color(1,1,1,0)
	sprite.scale = Vector2(2,2)
	tween.parallel().tween_property(sprite,"scale",commonScale,0.4)
	tween.parallel().tween_property(sprite,"modulate",Color(1,1,1,1),0.4)
	await tween.finished
func clear():
	remove_from_group("choice")
	lock = true
	tween.kill()
	tween = create_tween()
	tween.tween_property(sprite,"scale",Vector2(0,0),0.3).\
	set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_IN)
	await tween.finished
	queue_free()
func _on_检测_mouse_entered() -> void:
	if lock == true:return
	hover = true
	if tween:tween.kill()
	tween = create_tween()
	tween1 = create_tween()
	sprite.modulate = Color(1,1,1,1)
	sprite.scale = Vector2(1,1)
	var chooseColor = DragManager.chooseColor
	tween.tween_property(sprite,"scale",hoverScale,0.15).\
	set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
	tween1.tween_property(sprite,"modulate",chooseColor+Color(0,0,0,0.2),0.15).\
	set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
func _on_检测_mouse_exited() -> void:
	if lock == true:return
	hover = false
	tween = create_tween()
	tween1= create_tween()
	sprite.modulate = Color(1,1,1,1)
	sprite.scale = Vector2(1.2,1.2)
	tween.tween_property(sprite,"scale",commonScale,0.15).\
	set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
	tween1.tween_property(sprite,"modulate",Color(1,1,1,1),0.15).\
	set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
func _on_检测_mouse_click() -> void:
	_on_检测_mouse_entered()
func getPos():
	return m_pos
func getRoad()->Node2D:
	var arr = get_tree().get_nodes_in_group("road")
	for i in arr:
		if i.getLine() == m_pos.x:
			return i
	return null
func bindSignal():
	ChooseManager.chooseTarget.connect(func(_node):clear())
	area.input_event.connect(_on_area_input)
func _on_area_input(_viewport, event, _shape_idx) -> void:
	if lock == true:return
	if event is InputEventScreenTouch:
		if event.pressed:
			_on_检测_mouse_click()
		else:
			ChooseManager.chooseTarget.emit(self)
func _readyExtra():
	pass
func loadSprite(path:String):
	if !path:return
	sprite.texture = load(path)
func setExSpriteSca(mScale:float):
	ExSprite.scale = Vector2(1,1) * mScale
