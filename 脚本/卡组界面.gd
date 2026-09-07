extends Node2D
@onready var cardLibrary = $cardLibrary
@onready var background = $"背景图"
@onready var upSprite = $"顶部遮挡"
@onready var upSprite1 = $"顶部遮挡1"
@onready var button1 = $"返回按钮"
@onready var button2 = $"植物按钮"
@onready var button3 = $"僵尸按钮"
var rollPosY:float = 0
var rollPosYSpeed:float = 0
var themeColor1 = Color(0.411, 0.69, 0.38, 1.0)
var themeColor2 = Color(0.535, 0.9, 0.495, 1.0)
var themeColor3 = Color(0.274, 0.46, 0.253, 1.0)
var allPlantCard = [
	{"type":"texture","name":"头像","x":0,"y":-25,"scale":Vector2(0.2,0.2)},
	{"type":"sidebar","y":200},
	{"type":"label","x":-80,"y":0,"text":"本重制版作者"},
	#{"type":"animation","name":"logo","x":0,"y":350},
	{"type":"sidebar","y":50},
	{"type":"label","x":-300,"y":0,"text":"英雄"},
	{"type":"heroCard","sprite":"cornerimage_greenshadow","index":0,"y":100},
	{"type":"heroCard","sprite":"cornerimage_solarflare","index":1,"y":0},
	{"type":"heroCard","sprite":"cornerimage_BetaCarrotina","index":2,"y":0},
	{"type":"heroCard","sprite":"cornerimage_grassknuckles","index":3,"y":0},
	{"type":"heroCard","sprite":"cornerimage_captaincombustible","index":4,"y":0},
	{"type":"sidebar","y":250},
	{"type":"texture","name":"光能","x":-330,"y":0,"scale":Vector2(0.3,0.3)},
	{"type":"label","x":-300,"y":0,"text":"光能"},
	{"type":"plantCard","name":"向日葵植物","index":0,"y":90},
	{"type":"plantCard","name":"向日葵植物","index":1,"y":0},
	{"type":"plantCard","name":"向日葵植物","index":2,"y":0},
	{"type":"plantCard","name":"向日葵植物","index":3,"y":0},
	{"type":"plantCard","name":"蔓越莓植物","index":0,"y":120},
	{"type":"plantCard","name":"蔓越莓植物","index":1,"y":0},
	{"type":"plantCard","name":"蔓越莓植物","index":2,"y":0},
	{"type":"plantCard","name":"蔓越莓植物","index":3,"y":0},
	{"type":"zombieCard","name":"基础僵尸","index":0,"y":120},
	{"type":"zombieCard","name":"基础僵尸","index":1,"y":0},
	{"type":"zombieCard","name":"基础僵尸","index":2,"y":0},
	{"type":"zombieCard","name":"基础僵尸","index":3,"y":0},
	{"type":"zombieCard","name":"消防员僵尸","index":0,"y":120},
	{"type":"zombieCard","name":"消防员僵尸","index":1,"y":0},
	{"type":"zombieCard","name":"消防员僵尸","index":2,"y":0},
	{"type":"zombieCard","name":"消防员僵尸","index":3,"y":0},
	{"type":"texture","name":"button_store","x":0,"y":800},
]
var holdPos:Vector2 = Vector2(0,0)
var isHold:bool = false
var isEnd:bool = false
func _ready() -> void:
	SoundManager.setAudioOnST(0,"res://素材/Audio/Collection.mp3",0.5,true)
	rollPosY = 0
	#updateCard()
	testUpdateCard()
	updateThemeColor()
func testUpdateCard():
	var dic := [
	{"type":"texture","name":"头像","x":0,"y":-25,"scale":Vector2(0.2,0.2)},
	{"type":"sidebar","y":200},
	{"type":"label","x":-80,"y":0,"text":"本重制版作者"},
	#{"type":"animation","name":"logo","x":0,"y":350},
	{"type":"sidebar","y":50},
	{"type":"label","x":-300,"y":0,"text":"英雄"},
	{"type":"heroCard","sprite":"cornerimage_greenshadow","index":0,"y":100},
	{"type":"heroCard","sprite":"cornerimage_solarflare","index":1,"y":0},
	{"type":"heroCard","sprite":"cornerimage_BetaCarrotina","index":2,"y":0},
	{"type":"heroCard","sprite":"cornerimage_grassknuckles","index":3,"y":0},
	{"type":"heroCard","sprite":"cornerimage_captaincombustible","index":4,"y":0},
	{"type":"sidebar","y":250},
	{"type":"texture","name":"健壮","x":-330,"y":0,"scale":Vector2(0.3,0.3)},
	{"type":"label","x":-300,"y":0,"text":"僵尸"},
	]
	var dir = DirAccess.open("res://场景/僵尸卡组/")
	dir.list_dir_begin()
	var item = dir.get_next()
	var count = 0
	while item != "":
		if dir.current_is_dir() and item != "." and item != "..":
			var itemName = item
			var nitem
			if count % 4 == 0:
				if count == 0:
					nitem = {"type":"zombieCard","name":itemName,"index":0,"y":90}
				else:
					nitem = {"type":"zombieCard","name":itemName,"index":0,"y":120}
			else:
				nitem = {"type":"zombieCard","name":itemName,"index":count % 4,"y":0}
			dic.append(nitem)
			count += 1
		item = dir.get_next()
	dir.list_dir_end()
	
	var arr = [
	{"type":"sidebar","y":120},
	{"type":"texture","name":"光能","x":-330,"y":0,"scale":Vector2(0.3,0.3)},
	{"type":"label","x":-300,"y":0,"text":"植物"}
	]
	dic.append_array(arr)
				
	dir = DirAccess.open("res://场景/植物卡组/")
	dir.list_dir_begin()
	item = dir.get_next()
	count = 0
	while item != "":
		if dir.current_is_dir() and item != "." and item != "..":
			var itemName = item
			var nitem
			if count % 4 == 0:
				if count == 0:
					nitem = {"type":"plantCard","name":itemName,"index":0,"y":90}
				else:
					nitem = {"type":"plantCard","name":itemName,"index":0,"y":120}
			else:
				nitem = {"type":"plantCard","name":itemName,"index":count % 4,"y":0}
			dic.append(nitem)
			count += 1
		item = dir.get_next()
	dir.list_dir_end()
	updateCard(dic)
func updateCard(cardDict:Array = allPlantCard):
	var current_y = 0
	for block in cardDict:
		current_y += block["y"]
		match block["type"]:
			"texture":
				createTextureBlock(block,current_y)
			"animation":
				createSpineBlock(block,current_y)
			"plantCard":
				createPlantCardBlock(block,current_y)
			"zombieCard":
				createZombieCardBlock(block,current_y)
			"sidebar":
				block["name"] = "侧边栏"
				block["x"] = -100
				var node = createTextureBlock(block,current_y)
				node.modulate = Color(0,0,0,1)
				node.scale = Vector2(4,0.6)
			"label":
				createLabelBlock(block,current_y)
			"heroCard":
				createHeroCardBlock(block,current_y)
func createTextureBlock(block,y):
	var path = "res://素材/ui/界面ui/"+block["name"]+".png"
	var instance = load("res://场景/UI对象/卡牌库图片.tscn").instantiate()
	cardLibrary.add_child(instance)
	instance.setFatherNode(self)
	instance.setY(y)
	instance.position.x = block["x"]+360
	instance.loadTexture(path)
	instance.scale = block.get("scale",Vector2(1,1))
	return instance
func createPlantCardBlock(block,y):
	var node = load(CardManager.findCardScene(PVZ.Type.PLANT,block["name"])).instantiate()
	cardLibrary.add_child(node)
	node.setType("display")
	node.setDisplayY(y)
	node.position.x = block["index"]*175+105
	node.setDisplayNode(self)
func createZombieCardBlock(block,y):
	var node = load(CardManager.findCardScene(PVZ.Type.ZOMBIE,block["name"])).instantiate()
	cardLibrary.add_child(node)
	node.setType("display")
	node.setDisplayY(y)
	node.position.x = block["index"]*175+105
	node.setDisplayNode(self)
func createSpineBlock(block,y):
	var path = "res://素材/ui/界面ui/"+block["name"]+".tres"
	var instance = load("res://场景/UI对象/卡牌库动画.tscn").instantiate()
	cardLibrary.add_child(instance)
	instance.setFatherNode(self)
	instance.setY(y)
	instance.position.x = block["x"]+360
	instance.loadTexture(path)
	instance.setAnimation("intro")
	instance.scale = Vector2(0.5,0.5)
func createLabelBlock(block,y):
	var instance = load("res://场景/UI对象/卡牌库标签.tscn").instantiate()
	cardLibrary.add_child(instance)
	instance.setFatherNode(self)
	instance.setY(y)
	instance.setLabel(block.get("text","text"))
	instance.position.x = block["x"]+360
	instance.scale = block.get("scale",Vector2(1,1))
	return instance
func createHeroCardBlock(block,y):
	var node = load("res://场景/UI对象/英雄按钮.tscn").instantiate()
	cardLibrary.add_child(node)
	node.setDisplayY(y)
	#node.position.x = block["index"]*175+105
	node.position.x = (block["index"]-2)*135+360
	node.setDisplayNode(self)
	node.loadSprite(block["sprite"])
func _input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			if rollPosYSpeed > 0:rollPosYSpeed = 0
			rollPosYSpeed -= 150
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			if rollPosYSpeed < 0:rollPosYSpeed = 0
			rollPosYSpeed += 150
		elif event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed:
				holdPos = event.position
				isHold = true
			else:
				isHold = false
	elif event is InputEventMouseMotion:
		if isHold == true:
			var pos = event.position.y
			var speed = -(pos - holdPos.y) * 3.0
			holdPos = event.position
			if speed > 0:
				if rollPosYSpeed < 0:rollPosYSpeed = 0
				rollPosYSpeed += speed
			elif speed < 0:
				if rollPosYSpeed > 0:rollPosYSpeed = 0
				rollPosYSpeed += speed
func _process(delta: float) -> void:
	if rollPosY <= 0 && rollPosYSpeed <0:
		rollPosY += (0-rollPosY)/0.08 * delta
	rollPosY += rollPosYSpeed * delta
	rollPosYSpeed += (0-rollPosYSpeed)/0.3 * delta
func updateThemeColor():
	var tween = create_tween()
	var tween1 = create_tween()
	var tween2 = create_tween()
	tween.tween_property(background,"modulate",themeColor1,0.1).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	tween1.tween_property(upSprite,"modulate",themeColor2,0.1).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
	tween2.tween_property(upSprite1,"modulate",themeColor3,0.1).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN_OUT)
func buttonScaleChange(node,scale:Vector2):
	var tween = create_tween()
	tween.tween_property(node,"scale",scale,0.1)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)




func _on_texture_button_pressed() -> void:
	if isEnd == true:return
	isEnd = true
	SceneManager.change_scene("res://场景/UI对象/主界面.tscn",{"speed":3,"wait_time":0.2})
func _on_texture_button_button_down() -> void:
	buttonScaleChange(button1,Vector2(0.8,0.8))
func _on_texture_button_button_up() -> void:
	buttonScaleChange(button1,Vector2(1,1))
func _on_植物按钮_button_down() -> void:
	buttonScaleChange(button2,Vector2(0.30,0.30))
func _on_植物按钮_button_up() -> void:
	buttonScaleChange(button2,Vector2(0.32,0.32))
func _on_僵尸按钮_button_down() -> void:
	buttonScaleChange(button3,Vector2(0.30,0.30))
func _on_僵尸按钮_button_up() -> void:
	buttonScaleChange(button3,Vector2(0.32,0.32))
func _on_僵尸按钮_pressed() -> void:
	themeColor1 = Color(0.365, 0.192, 0.439, 1.0)
	themeColor2 = Color(0.657, 0.348, 0.79, 1.0)
	themeColor3 = Color(0.25, 0.132, 0.3, 1.0)
	updateThemeColor()
func _on_植物按钮_pressed() -> void:
	themeColor1 = Color(0.411, 0.69, 0.38, 1.0)
	themeColor2 = Color(0.535, 0.9, 0.495, 1.0)
	themeColor3 = Color(0.274, 0.46, 0.253, 1.0)
	updateThemeColor()
