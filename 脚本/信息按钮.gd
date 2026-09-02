extends TextureButton
var _tween: Tween
var isEnd:bool = false
var isOver:bool = false
func _ready() -> void:
	intro()
	setCenterPosition(Vector2(360,640))
func center_position(center_pos: Vector2 = Vector2.ZERO) -> void:
	if center_pos == Vector2.ZERO:
		center_pos = get_viewport_rect().size * 0.5
	position = center_pos - size * 0.5
func setCenterPosition(center_pos: Vector2) -> void:
	center_position(center_pos)
func intro():
	scale = Vector2.ZERO
	_tween = create_tween()
	_tween.set_trans(Tween.TRANS_BACK)
	_tween.set_ease(Tween.EASE_OUT)
	_tween.parallel().tween_property(self, "scale", Vector2(0.7,0.7), 0.3)
func buttonScaleChange(mscale:Vector2,time:float = 0.15):
	var tween = create_tween()
	tween.tween_property(self,"scale",mscale,time)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
func _on_mouse_entered() -> void:
	if isEnd == true:return
	isOver = true
	buttonScaleChange(Vector2(0.78,0.78))
func _on_mouse_exited() -> void:
	if isEnd == true:return
	isOver = false
	buttonScaleChange(Vector2(0.7,0.7))
func _gui_input(event: InputEvent) -> void:
	if isEnd == true:return
	if event is InputEventScreenTouch:
		if event.pressed:
			_on_mouse_entered()
		else:
			_on_pressed()
func _on_pressed() -> void:
	isEnd = true
	buttonScaleChange(Vector2(0,0),0.2)
	await get_tree().create_timer(0.2).timeout
	queue_free()
func clear():
	await get_tree().create_timer(0.2).timeout
	queue_free()
