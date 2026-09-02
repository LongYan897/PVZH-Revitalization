extends Control
@onready var button = $textureButton
var displayY:float = 0
var displayNode
func _ready() -> void:
	pass
func _process(delta: float) -> void:
	if displayNode:
		position.y = displayY - displayNode.rollPosY
func loadSprite(path:String):
	path = "res://素材/ui/英雄图片/"+path+".png"
	button.texture_normal = load(path)
func setDisplayY(y):
	displayY = y
func setDisplayNode(node):
	displayNode = node
func quickSetScale(node,mscale:Vector2):
	var tween = create_tween()
	tween.set_trans(Tween.TRANS_QUAD)
	tween.set_ease(Tween.EASE_IN_OUT)
	tween.parallel().tween_property(node,"scale",mscale, 0.15)
func _on_texture_button_pressed() -> void:
	if !MessageBox.isEmpty():return
	var instance = load("res://场景/UI对象/英雄图鉴.tscn").instantiate()
	add_child(instance)
func _on_texture_button_button_down() -> void:
	quickSetScale(self,Vector2(0.45,0.45))
func _on_texture_button_button_up() -> void:
	quickSetScale(self,Vector2(0.5,0.5))
