extends Control
var zombieData:ZombieData
var plantData:PlantData
@onready var rareSprite = $"稀有标签"
@onready var rareSpriteLabel = $"稀有标签label"
@onready var nameLabel = $"名字"
@onready var propertiesSpriteLeft = $"背景/属性图片左"
@onready var propertiesSpriteRight = $"背景/属性图片右"
@onready var storyLabel = $"故事标签"
@onready var blackgourd = $"底版"
@onready var themeBackgroundLeft = $"背景/左背景"
@onready var themeBackgroundRight = $"背景/右背景"
@onready var themeBackground = $"人物框"
var dataInstance
var isEnd:bool = false
var tween:Tween
func _ready() -> void:
	MessageBox.addMessageBox(self)
	intro()
	createTarget(1,PVZ.Type.PLANT)
func setRare(rare:CardManager.Rare):
	match rare:
		CardManager.Rare.Common:
			rareSprite.texture = load("res://素材/ui/稀有标签/rarity_0.png")
			rareSprite.position.y = 200
		CardManager.Rare.Uncommon:
			rareSprite.texture = load("res://素材/ui/稀有标签/rarity_1.png")
			rareSprite.position.y = 200
		CardManager.Rare.Rare:
			rareSprite.texture = load("res://素材/ui/稀有标签/rarity_2.png")
			rareSprite.position.y = 215
		CardManager.Rare.SuperRare:
			rareSprite.texture = load("res://素材/ui/稀有标签/rarity_3.png")
			rareSprite.position.y = 216
		CardManager.Rare.Legendary:
			rareSprite.texture = load("res://素材/ui/稀有标签/rarity_4.png")
			rareSprite.position.y = 235
		CardManager.Rare.Event:
			rareSprite.texture = load("res://素材/ui/稀有标签/rarity_E.png")
			rareSprite.position.y = 192
func intro():
	scale = Vector2(0,0)
	blackgourd.modulate = Color(0,0,0,0)
	tween = create_tween()
	tween.set_trans(Tween.TRANS_BACK)
	tween.set_ease(Tween.EASE_OUT)
	tween.parallel().tween_property(self, "scale", Vector2(1,1), 0.5)
	tween.parallel().tween_property(blackgourd,"modulate",Color(0,0,0,0.5), 0.5)
func clear():
	if isEnd == true:return
	if tween.is_running():tween.kill()
	tween = create_tween()
	tween.set_trans(Tween.TRANS_BACK)
	tween.set_ease(Tween.EASE_IN)
	tween.parallel().tween_property(self, "scale", Vector2(0,0), 0.3)
	tween.parallel().tween_property(blackgourd,"modulate",Color(0,0,0,0), 0.3)
	await get_tree().create_timer(0.1).timeout
	MessageBox.deleteMessageBox(self)
	blackgourd.mouse_filter = MOUSE_FILTER_IGNORE
	await get_tree().create_timer(0.4).timeout
	queue_free()
func loadDictionary(mname:String):
	for block in CardManager.cardDescription:
		if block.name == mname:
			nameLabel.text = block.get("showName","NULL")
			storyLabel.text = block.get("story","NULL")
			rareSpriteLabel.text = block.get("rareLabel","NULL")
			setProperties(block.get("properties",CardManager.Properties.BRAINY))
func createTarget(data,type:PVZ.Type):
	var path = "res://场景/英雄/"+"英雄通用模板.tscn"
	print("path:",path)
	var instance = load(path).instantiate()
	instance.type = Hero.Type.DISPLAY_ONLY
	instance.z_index = 100
	instance.position = Vector2(0,-220)
	add_child(instance)
func _input(event: InputEvent) -> void:
	if event is InputEventScreenTouch:
		if !event.is_pressed():return
		elif isEnd == false:
			clear()
			isEnd = true
			get_viewport().set_input_as_handled()
func setProperties(properties:CardManager.Properties):
	var color
	match properties:
		CardManager.Properties.BRAINY:
			propertiesSpriteLeft.texture = load("res://素材/ui/界面ui/有脑.png")
			color = Color(0.78, 0.382, 0.72, 1.0)
		CardManager.Properties.GUARDIAN:
			propertiesSpriteLeft.texture = load("res://素材/ui/界面ui/守卫.png")
			color = Color(0.54, 0.341, 0.286, 1.0)
		CardManager.Properties.HEARTY:
			propertiesSpriteLeft.texture = load("res://素材/ui/界面ui/健壮.png")
			color = Color(0.918, 0.62, 0.286, 1.0)
		CardManager.Properties.HUNGRY:
			propertiesSpriteLeft.texture = load("res://素材/ui/界面ui/猛兽.png")
			color = Color(0.282, 0.58, 0.737, 1.0)
		CardManager.Properties.KABLOOM:
			propertiesSpriteLeft.texture = load("res://素材/ui/界面ui/爆花.png")
			color = Color(0.843, 0.184, 0.196, 1.0)
		CardManager.Properties.MADCAP:
			propertiesSpriteLeft.texture = load("res://素材/ui/界面ui/疯狂.png")
			color = Color(0.412, 0.184, 0.655, 1.0)
		CardManager.Properties.MEGAGROW:
			propertiesSpriteLeft.texture = load("res://素材/ui/界面ui/猛长.png")
			color = Color(0.239, 0.624, 0.055, 1.0)
		CardManager.Properties.SMARTY:
			propertiesSpriteLeft.texture = load("res://素材/ui/界面ui/聪明.png")
			color = Color(1,1,1,1.0)
		CardManager.Properties.SNEAKY:
			propertiesSpriteLeft.texture = load("res://素材/ui/界面ui/狡猾.png")
			color = Color(0.153, 0.161, 0.153, 1.0)
		CardManager.Properties.SOLAR:
			propertiesSpriteLeft.texture = load("res://素材/ui/界面ui/光能.png")
			color = Color(0.91, 0.776, 0.188, 1.0)
		_:
			propertiesSpriteLeft.texture = load("res://素材/ui/界面ui/聪明.png")
			color = Color(1,1,1,1.0)
	updateThemeColor(color)
func updateThemeColor(color:Color):
	themeBackground.self_modulate = color
	themeBackgroundLeft.self_modulate = color
	themeBackgroundRight.self_modulate = color
func setPropertiesLabel(data):
	var text:String = "-"
	for i in data.properties:
		if text == "":
			text += CardManager.TranslateCardProperties[i]
		else:
			text += " "+CardManager.TranslateCardProperties[i]
	text += "-"
