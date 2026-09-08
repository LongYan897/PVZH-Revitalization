extends Node2D
class_name BaseHero
@onready var hero:SpineSprite = $"英雄"
@onready var state:SpineAnimationState
var heroData:Hero 
@export var type:Hero.Type
@export var dataResource:SpineSkeletonDataResource
@export var size:Vector2
@export var specialTrack:int = 1
var is_alive:bool = true
var shieldVal:int = 0
var shieldCount:int = 3
var team:int = 1
func _ready() -> void:
	setup()
	await intro()
	special()
	#die()
func intro():
	state.set_animation("intro",false,0)
	await hero.animation_completed
	state.set_animation("idle",true,0)
func hit(val:int):
	var pval = randi_range(3,3)
	if shieldVal+pval >= 8 && shieldCount != 0:
		pval = shieldVal+pval-8
		shieldVal += pval
		if type == Hero.Type.PLANT_HERO:
			TurnManager.plantShield.hitEffect(pval)
			TurnManager.plantShield.setShieldType(shieldCount)
		if type == Hero.Type.ZOMBIE_HERO:
			TurnManager.zombieShield.hitEffect(pval)
			TurnManager.zombieShield.setShieldType(shieldCount)
		shieldVal = 0
		shieldCount -= 1
	elif shieldCount != 0:
		state.set_animation("hit",false,0)
		shieldVal += pval
		heroData.health -= val
		if type == Hero.Type.PLANT_HERO:
			TurnManager.plantShield.hitNormalEffect(shieldVal,pval,heroData.health)
		if type == Hero.Type.ZOMBIE_HERO:
			TurnManager.zombieShield.hitNormalEffect(shieldVal,pval,heroData.health)
		#等待动画结束
		await waitAnimationComP(0)
		state.set_animation("idle",true,0)
	else:
		state.set_animation("hit",false,0)
		heroData.health -= val
		if type == Hero.Type.PLANT_HERO:
			TurnManager.plantShield.updateHealth(heroData.health)
			TurnManager.plantShield.healthStretch()
			TurnManager.plantShield.hitHealthColor()
		if type == Hero.Type.ZOMBIE_HERO:
			TurnManager.zombieShield.updateHealth(heroData.health)
			TurnManager.zombieShield.healthStretch()
			TurnManager.zombieShield.hitHealthColor()
		#等待动画结束
		await waitAnimationComP(0)
		state.set_animation("idle",true,0)
func die():
	state.set_animation("die",false,0)
	await hero.animation_completed
func special():
	while is_alive:
		await get_tree().create_timer(randi_range(4,6)).timeout
		var track0 = state.get_track(0)
		if track0.get_animation().get_name() == "idle":
			state.set_animation("special1",false,specialTrack)
			var a =state.get_track(specialTrack)
			while a.get_animation_time() < a.get_animation_end() - 0.05:
				await get_tree().process_frame
			await get_tree().process_frame
			if specialTrack == 0:
				state.set_animation("idle",true,0)
			else:
				state.clear_track(specialTrack)
func setup():
	print(name, " type=", type, " PLANT=", Hero.Type.PLANT_HERO)
	hero.skeleton_data_res = dataResource
	state = hero.get_animation_state()
	scale = size
	if type == Hero.Type.DISPLAY_ONLY:return
	
	if TurnManager.team == 1 && type == Hero.Type.PLANT_HERO:
		team = 1
	elif TurnManager.team == 2 && type == Hero.Type.PLANT_HERO:
		team = 2
	elif TurnManager.team == 1 && type == Hero.Type.ZOMBIE_HERO:
		team = 2
	elif TurnManager.team == 2 && type == Hero.Type.ZOMBIE_HERO:
		team = 1
	
	heroData = Hero.new()
	if team == 1:
		position = Vector2(360,950)
		z_index = 8
	if team == 2:
		position = Vector2(360,250)
		z_index = 0
	if type == Hero.Type.ZOMBIE_HERO:
		HeroManager.zombieHero = self
	if type == Hero.Type.PLANT_HERO:
		HeroManager.plantHero = self
func getPosition(pos):
	if team == 1:
		return Vector2(pos.x,global_position.y-50)
	else:return Vector2(pos.x,global_position.y+50)
func waitAnimationComP(track:int = 0):
	var a = state.get_track(track)
	while a.get_animation_time() < a.get_animation_end() - 0.05:
		await get_tree().process_frame
	await get_tree().create_timer(0.05).timeout
func playTurnSound(turnCount:int):
	var path = "res://素材/Audio/GreenShadow/GreenShadow" + str(turnCount) + ".mp3"
	SoundManager.setAudioOnST(0,path,0.5,true,6)
