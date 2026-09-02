extends Control
@onready var page = $"界面"
@onready var closeButton = $"界面/关闭按钮"
@onready var backButton = $"界面/返回界面"
@onready var box = $"界面"
@onready var dataButton = $"界面/获取ai按钮"

var isShow:bool = false
var isEnd:bool = false
func _ready() -> void:
	page.hide()
func quickButtonScale(node,mscale:Vector2 = Vector2(0.7,0.7),time:float = 0.15):
	var tween = create_tween()
	tween.tween_property(node,"scale",mscale,time)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
	await tween.finished
func quickButtonRotation(node,mrotation:float = 0,time:float = 0.3):
	var tween = create_tween()
	tween.tween_property(node,"rotation",mrotation,time)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
	await tween.finished
func quickButtonPos(node,mpos:Vector2 = Vector2(0,0),time:float = 0.3):
	var tween = create_tween()
	tween.tween_property(node,"position",mpos,time)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
	await tween.finished
func _on_mouse_entered() -> void:
	quickButtonScale(self,Vector2(0.7,0.7))
func _on_mouse_exited() -> void:
	quickButtonScale(self,Vector2(0.6,0.6))
func _on_pressed() -> void:
	page.show()
	if isShow == true:return
	page.modulate = Color(1,1,1,1)
	quickButtonRotation(self,deg_to_rad(90))
	closeButton.scale = Vector2.ZERO
	quickButtonScale(closeButton,Vector2(1,1))
	box.position = Vector2(0,1300)
	quickButtonPos(box,Vector2(0,144))
	MessageBox.addMessageBox(self)
	isShow = true
func _on_关闭按钮_mouse_entered() -> void:
	quickButtonScale(closeButton,Vector2(1.1,1.1))
func _on_关闭按钮_mouse_exited() -> void:
	quickButtonScale(closeButton,Vector2(1,1))
func _on_关闭按钮_pressed() -> void:
	if isShow == false:return
	quickButtonScale(closeButton,Vector2.ZERO)
	quickButtonRotation(self,deg_to_rad(0))
	box.position = Vector2(0,144)
	await quickButtonPos(box,Vector2(0,1300))
	page.hide()
	MessageBox.deleteMessageBox(self)
	isShow = false
func _on_返回界面_mouse_entered() -> void:
	if isEnd == true:return
	quickButtonScale(backButton,Vector2(1.1,1.1))
func _on_返回界面_mouse_exited() -> void:
	if isEnd == true:return
	quickButtonScale(backButton,Vector2(1,1))
func _on_返回界面_pressed() -> void:
	if isEnd == true:return
	if !AQ.queue.is_empty():return
	if TurnManager.team == 1 && TurnManager.m_state != TurnManager.Type.PLANT_TURN:return
	if TurnManager.team == 2 && !(TurnManager.m_state == TurnManager.Type.ZOMBIE_TURN || TurnManager.m_state == TurnManager.Type.PLAN_TURN):return
	isEnd = true
	quickButtonRotation(self,deg_to_rad(0))
	MessageBox.deleteMessageBox(self)
	SceneManager.change_scene("res://场景/UI对象/主界面.tscn",{"speed":3,"wait_time":0.2})
	TurnManager.clearAutoLoad()
	box.position = Vector2(0,144)
	quickButtonPos(box,Vector2(0,1300))
	await quickButtonScale(backButton,Vector2(0.8,0.8),0.12)
	await quickButtonScale(backButton,Vector2(1,1),0.12)
	page.hide()
func _on_获取ai按钮_mouse_entered() -> void:
	if isEnd == true:return
	quickButtonScale(dataButton,Vector2(1.1,1.1))
func _on_获取ai按钮_mouse_exited() -> void:
	if isEnd == true:return
	quickButtonScale(dataButton,Vector2(1,1))
func _on_获取ai按钮_pressed() -> void:
	if isEnd == true:return
	MessageBox.deleteMessageBox(self)
	box.position = Vector2(0,144)
	quickButtonPos(box,Vector2(0,1300))
	quickButtonRotation(self,deg_to_rad(0))
	AIManager.getSceneSnap()
	await quickButtonScale(backButton,Vector2(0.8,0.8),0.12)
	await quickButtonScale(backButton,Vector2(1,1),0.12)
	page.hide()
	isShow = false
