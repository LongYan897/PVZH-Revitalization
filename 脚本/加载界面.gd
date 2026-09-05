extends Node2D
@onready var sprite = $"背景"
@onready var loadSprite = $"加载"
@onready var loadRect = $"黑屏"
@onready var pressButton = $"进入按钮"
@onready var pressButtonLabel = $"进入按钮/Label"
var canClick:bool
var tween1:Tween
var endScene:bool 
func _ready() -> void:
	loadRect.show()
	pressButton.scale = Vector2.ZERO
	sprite.scale = Vector2(1,1) * 0.8
	var tween = create_tween()
	tween.parallel().tween_property(sprite,"scale",Vector2(1,1) * 0.85,1).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	tween.parallel().tween_property(loadRect,"modulate",Color(1,1,1,0),0.5).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(2).timeout
	buttonShow()
func _process(delta: float) -> void:
	loadSprite.rotation += rad_to_deg(PI * 0.05) * delta
func buttonShow():
	pressButtonLabel.scale = Vector2.ZERO
	#pressButton.modulate = Color(4.416, 4.416, 4.416, 1.0)
	var tween = create_tween()
	tween.parallel().tween_property(loadSprite,"scale",Vector2.ZERO,0.7).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_IN)
	await get_tree().create_timer(0.7).timeout
	tween = create_tween()
	tween.parallel().tween_property(pressButton,"scale",Vector2(0.35,0.7),0.5).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tween.parallel().tween_property(pressButton,"modulate",Color(1,1,1,1),0.25).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_IN)
	await get_tree().create_timer(0.5).timeout
	tween = create_tween()
	tween.parallel().tween_property(pressButton,"position",Vector2(175,1180),0.7).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(0.6).timeout
	tween = create_tween()
	tween.parallel().tween_property(pressButton,"scale",Vector2(1,1) * 1.5,1).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_IN_OUT)
	tween.parallel().tween_property(pressButton,"position",Vector2(175,1210),1).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(1).timeout
	tween = create_tween()
	tween.parallel().tween_property(pressButtonLabel,"scale",Vector2(1,1),0.3).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	canClick = true

func _on_进入按钮_mouse_entered() -> void:
	if !canClick:
		while !canClick:
			await get_tree().process_frame
	var mouse_pos = get_viewport().get_mouse_position()
	var button_rect = Rect2(pressButton.global_position, pressButton.size)
	if !button_rect.has_point(mouse_pos):return
	tween1 = create_tween()
	var color = Color(0.118, 0.98, 0.348)
	tween1.parallel().tween_property(pressButton,"self_modulate",color,0.3).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
func _on_进入按钮_mouse_exited() -> void:
	if !canClick:return
	if tween1 && tween1.is_running():tween1.kill()
	pressButton.self_modulate = Color(1,1,1)
func _on_进入按钮_pressed() -> void:
	if !canClick:return
	if endScene:return
	endScene = true
	await SceneManager.pictureScene(get_tree().root,"res://场景/UI对象/主界面.tscn")
