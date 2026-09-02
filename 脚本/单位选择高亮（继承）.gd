extends ChooseHighlight
func _readyExtra():
	ChooseManager.chooseCount += 1
func _input(event: InputEvent) -> void:
	if event is InputEventMouseButton && event.button_index == MOUSE_BUTTON_MASK_LEFT:
		if !event.is_pressed() && hover == true:
			ChooseManager.chooseTarget.emit(self)
func _on_检测_mouse_click() -> void:
	if lock == true:return
	hover = true
	if tween:tween.kill()
	tween = create_tween()
	tween1 = create_tween()
	sprite.modulate = Color(1,1,1,1)
	tween.tween_property(sprite,"scale",Vector2.ZERO,0.2).\
	set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_IN)
	sprite.modulate = DragManager.chooseColor
	tween1.tween_property(sprite,"modulate",DragManager.chooseColor,0.2).\
	set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
