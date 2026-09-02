extends Node2D
@onready var dirt:SpineSprite = $"图片"
@onready var state:SpineAnimationState = $"图片".get_animation_state()
func _ready() -> void:
	position = Vector2(0,230)
	scale = Vector2(3.5,3.5)
	state.set_animation("intro",false,0)
func clear():
	state.set_animation("clear",false,0)
	await dirt.animation_completed
func setRoad(line:int):
	var road = RoadList.getRoad(line).type
	var skeleton = dirt.get_skeleton()
	#print("土坑的路种类line:",line,"road:",road)
	if road == Road.ROAD_TYPE.HIGH_GROUND:
		skeleton.set_attachment("土坑","zombie_roof_back")
	if road == Road.ROAD_TYPE.GROUND:
		skeleton.set_attachment("土坑","zombie_dirt_back")
	if road == Road.ROAD_TYPE.WATER_WAY:
		skeleton.set_attachment("土坑","zombie_water_back")
