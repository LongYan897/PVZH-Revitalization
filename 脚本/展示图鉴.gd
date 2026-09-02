extends Control
var zombieData:ZombieData
var plantData:PlantData
@onready var rareSprite = $"稀有标签"
@onready var rareSpriteLabel = $"稀有标签label"
@onready var nameLabel = $"名字"
@onready var propertiesLabel = $"属性标签"
@onready var propertiesSprite =$"背景/属性图片"
@onready var descriptionLabel = $"介绍Label"
@onready var costSprite = $"花费数值"
@onready var costSpriteLabel = $"花费数值/花费数值Label"
@onready var storyLabel = $"故事标签"
@onready var blackgourd = $"底版"
@onready var themeBackgroundLeft = $"背景/左背景"
@onready var themeBackgroundRight = $"背景/右背景"
@onready var themeBackground = $"人物框"
var dataInstance
var isEnd:bool = false
var tween:Tween
func _ready() -> void:
	#loadZombieRes(load(CardManager.cardPath.get("基础僵尸")))
	#loadPlantRes(load(CardManager.cardPath.get("基础植物")))
	MessageBox.addMessageBox(self)
	intro()
func loadZombieRes(data:ZombieData):
	costSprite.texture = load("res://素材/卡牌属性图片/inhnd_brains.png")
	costSpriteLabel.text = str(data.cost)
	setRare(data.rare)
	loadDictionary(data.name)
	setPropertiesLabel(data)
	createTarget(data,PVZ.Type.ZOMBIE)
func loadPlantRes(data:PlantData):
	costSprite.texture = load("res://素材/卡牌属性图片/inhnd_sun_gb.png")
	costSpriteLabel.text = str(data.cost)
	loadDictionary(data.name)
	setRare(data.rare)
	setPropertiesLabel(data)
	createTarget(data,PVZ.Type.PLANT)
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
			descriptionLabel.text = block.get("description","NULL")
			storyLabel.text = block.get("story","NULL")
			rareSpriteLabel.text = block.get("rareLabel","NULL")
			setProperties(block.get("properties",CardManager.Properties.BRAINY))
func createTarget(data,type:PVZ.Type):
	var instance
	var datainstance 
	var mname = data.name
	if type == PVZ.Type.PLANT:
		instance = load("res://场景/植物卡/"+mname+"/"+mname+".tscn").instantiate()
		dataInstance = PlantInstance.new(data)
	elif type == PVZ.Type.ZOMBIE:
		instance = load("res://场景/僵尸卡/"+mname+"/"+mname+".tscn").instantiate()
		dataInstance = ZombieInstance.new(data)
	instance.type = BaseZombie.Type.Display_only
	add_child(instance)
	instance.position = Vector2(0,-300)
	instance.scale = Vector2(0.3,0.3)
	instance.loadResource(dataInstance)
	instance.top_level = false
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
			propertiesSprite.texture = load("res://素材/ui/界面ui/有脑.png")
			color = Color(0.78, 0.382, 0.72, 1.0)
		CardManager.Properties.GUARDIAN:
			propertiesSprite.texture = load("res://素材/ui/界面ui/守卫.png")
			color = Color(0.54, 0.341, 0.286, 1.0)
		CardManager.Properties.HEARTY:
			propertiesSprite.texture = load("res://素材/ui/界面ui/健壮.png")
			color = Color(0.918, 0.62, 0.286, 1.0)
		CardManager.Properties.HUNGRY:
			propertiesSprite.texture = load("res://素材/ui/界面ui/猛兽.png")
			color = Color(0.282, 0.58, 0.737, 1.0)
		CardManager.Properties.KABLOOM:
			propertiesSprite.texture = load("res://素材/ui/界面ui/爆花.png")
			color = Color(0.843, 0.184, 0.196, 1.0)
		CardManager.Properties.MADCAP:
			propertiesSprite.texture = load("res://素材/ui/界面ui/疯狂.png")
			color = Color(0.412, 0.184, 0.655, 1.0)
		CardManager.Properties.MEGAGROW:
			propertiesSprite.texture = load("res://素材/ui/界面ui/猛长.png")
			color = Color(0.239, 0.624, 0.055, 1.0)
		CardManager.Properties.SMARTY:
			propertiesSprite.texture = load("res://素材/ui/界面ui/聪明.png")
			color = Color(1,1,1,1.0)
		CardManager.Properties.SNEAKY:
			propertiesSprite.texture = load("res://素材/ui/界面ui/狡猾.png")
			color = Color(0.153, 0.161, 0.153, 1.0)
		CardManager.Properties.SOLAR:
			propertiesSprite.texture = load("res://素材/ui/界面ui/光能.png")
			color = Color(0.91, 0.776, 0.188, 1.0)
		_:
			propertiesSprite.texture = load("res://素材/ui/界面ui/聪明.png")
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
	propertiesLabel.text = str(text)
