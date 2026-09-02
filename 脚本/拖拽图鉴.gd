extends Control
@onready var box = $Panel
@onready var boxStyle = box.get_theme_stylebox("panel")
@onready var label = $Panel/RichTextLabel
@export var panel_padding := Vector2(40, 24)
@export var min_panel_size := Vector2(200, 80)
var followNode = null
var isEnd:bool = false
var introColor:Color = Color(1,1,1,1)
var offsetY:float = -110
func _ready() -> void:
	followingNode()
	#await get_tree().create_timer(0.3).timeout
	stretch(0.15)
	#setSize(Vector2.ZERO,0)
	#setSize(Vector2(500,200),0.3)
func setSize(msize:Vector2,time:float):
	var tarSize = box.size
	var downSize = (msize-tarSize)/2.0
	var pos = box.position - downSize
	var tween = create_tween()
	tween.set_trans(Tween.TRANS_QUAD)
	tween.set_ease(Tween.EASE_IN_OUT)
	tween.parallel().tween_property(box, "size",msize, time)
	tween.parallel().tween_property(box,"position",pos,time)
func fitToText():
	await get_tree().process_frame
	var textSize = Vector2(label.get_content_width(), label.get_content_height())
	var targetSize = Vector2(
		max(min_panel_size.x, textSize.x + panel_padding.x),
		max(min_panel_size.y, textSize.y + panel_padding.y)
	)
	label.custom_minimum_size = textSize
	setSize(targetSize,0)
func stretch(time:float):
	scale = Vector2(0,0)
	boxStyle.bg_color = Color(1,1,1,1)
	var tweenColor = create_tween()
	tweenColor.set_trans(Tween.TRANS_QUAD)
	tweenColor.set_ease(Tween.EASE_IN_OUT)
	tweenColor.parallel().tween_property(boxStyle,"bg_color",Color(0.1, 0.1, 0.1, 0.725),3*time)
	var tween = create_tween()
	tween.set_trans(Tween.TRANS_QUAD)
	tween.set_ease(Tween.EASE_IN_OUT)
	tween.parallel().tween_property(self,"scale",Vector2(0.3,1.5),time)
	await get_tree().create_timer(time*0.7).timeout
	if isEnd == true:return
	tween = create_tween()
	tween.set_trans(Tween.TRANS_QUAD)
	tween.set_ease(Tween.EASE_IN_OUT)
	tween.parallel().tween_property(self,"scale",Vector2(1.3,0.5),time)
	await tween.finished
	if isEnd == true:return
	tween = create_tween()
	tween.set_trans(Tween.TRANS_BACK)
	tween.set_ease(Tween.EASE_OUT)
	tween.parallel().tween_property(self,"scale",Vector2(1,1),time)
func setText(text:String):
	label.text = text
	fitToText()
func clear(time = 0.15):
	isEnd = true
	#boxStyle.bg_color = Color(0.1, 0.1, 0.1, 0.725)
	var tweenColor = create_tween()
	tweenColor.set_trans(Tween.TRANS_QUAD)
	tweenColor.set_ease(Tween.EASE_IN_OUT)
	tweenColor.parallel().tween_property(boxStyle,"bg_color",Color(1,1,1,1),3*time)
	var tween = create_tween()
	tween.set_trans(Tween.TRANS_QUAD)
	tween.set_ease(Tween.EASE_IN_OUT)
	tween.parallel().tween_property(self,"scale",Vector2(1.3,0.5),time)
	await get_tree().create_timer(time*0.7).timeout
	tween = create_tween()
	tween.set_trans(Tween.TRANS_QUAD)
	tween.set_ease(Tween.EASE_IN_OUT)
	tween.parallel().tween_property(self,"scale",Vector2(0.3,1.5),time)
	await tween.finished
	tween = create_tween()
	tween.set_trans(Tween.TRANS_QUAD)
	tween.set_ease(Tween.EASE_OUT)
	tween.parallel().tween_property(self,"scale",Vector2(0.01,0),time)
	tween.parallel().tween_property(self,"offsetY",-60,time)
	await tween.finished
	queue_free()
func followingNode():
	while 1:
		if followNode != null && is_instance_valid(followNode):
			position = followNode.position + Vector2(0,offsetY)
		await get_tree().process_frame
func quickClear(time = 0.1):
	isEnd = true
	var tween = create_tween()
	tween.set_trans(Tween.TRANS_BACK)
	tween.set_ease(Tween.EASE_IN)
	tween.parallel().tween_property(self,"scale",Vector2(0,0),time)
	tween.parallel().tween_property(self,"offsetY",-60,time)
	await get_tree().create_timer(time).timeout
	queue_free()
