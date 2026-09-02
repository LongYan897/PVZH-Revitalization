extends Node2D
@onready var wheel:SpineSprite = $"轮盘图片"
@onready var state = wheel.get_animation_state()
@onready var team:int = 0
@onready var area = $"碰撞体积"
@onready var animationName:int =-1
enum animationType{
	ZOMBIE_START_ZOMBIE_TURN = 0,
	ZOMBIE_MOVE_PLANT_TURN,
	ZOMBIE_MOVE_PLAN_TURN,
	ZOMBIE_MOVE_ATTACK_TURN,
	ZOMBIE_BACK_CARD_TURN,
	PLANT_START_ZOMBIE_TURN,
	PLANT_MOVE_PLANT_TURN,
	PLANT_MOVE_PLAN_TURN,
	PLANT_MOVE_ATTACK_TURN,
	PLANT_BACK_CARD_TURN,
}
func _ready() -> void:
	setup()
	setTeam()
	if TurnManager.team == 1:
		var res = func():
			animation(animationType.PLANT_START_ZOMBIE_TURN)
			await wheel.animation_completed
		AQ.addAction(res)
	if TurnManager.team == 2:
		var res = func():
			animation(animationType.ZOMBIE_BACK_CARD_TURN)
			await wheel.animation_completed
		AQ.addAction(res)
func setup():
	position = Vector2(630,920)
	area.input_event.connect(_on_area_click)
	TurnManager.turnChange.connect(turnChange)
func animation(type:animationType):
	animationName = type
	match type:
		animationType.ZOMBIE_START_ZOMBIE_TURN:
			state.set_animation("zombieStartZombieTurn",false,0)
		animationType.ZOMBIE_MOVE_PLANT_TURN:
			state.set_animation("zombieMovePlantTurn",false,0)
		animationType.ZOMBIE_MOVE_PLAN_TURN:
			state.set_animation("zombieMovePlanTurn",false,0)
		animationType.ZOMBIE_MOVE_ATTACK_TURN:
			state.set_animation("zombieMoveAttackTurn",false,0)
		animationType.ZOMBIE_BACK_CARD_TURN:
			state.set_animation("zombieBackCardTurn",false,0)
			
		animationType.PLANT_START_ZOMBIE_TURN:
			state.set_animation("plantStartZombieTurn",false,0)
		animationType.PLANT_MOVE_PLANT_TURN:
			state.set_animation("plantMovePlantTurn",false,0)
		animationType.PLANT_MOVE_PLAN_TURN:
			state.set_animation("plantMovePlanTurn",false,0)
		animationType.PLANT_MOVE_ATTACK_TURN:
			state.set_animation("plantMoveAttackTurn",false,0)
		animationType.PLANT_BACK_CARD_TURN:
			state.set_animation("plantBackCardTurn",false,0)
	await wheel.animation_completed
	animationName = -1
func setTeam():
	if TurnManager.team == -1:
		print("Team set fail")
	team = TurnManager.team
func _on_area_click(viewport, event, shape_idx):
	if !MessageBox.isEmpty():
		return
	if event is InputEventScreenTouch:
		if !event.pressed:return
		if !AQ.queue.is_empty():return
		if animationName != -1 :return
		turnChange()
func turnChange(systemEmit:int = 0):
	var res = func():
		var turnState = TurnManager.getState()
		TurnManager.turnChangeBegin1.emit()
		if team == 1:
			match turnState:
				TurnManager.Type.CARD_TURN:
					if systemEmit == 0:return
					await animation(animationType.PLANT_START_ZOMBIE_TURN)
					TurnManager.setState(TurnManager.Type.ZOMBIE_TURN)
				TurnManager.Type.ZOMBIE_TURN:
					if systemEmit == 0:return
					await animation(animationType.PLANT_MOVE_PLANT_TURN)
					TurnManager.setState(TurnManager.Type.PLANT_TURN)
				TurnManager.Type.PLANT_TURN:
					if TurnManager.attackMode == TurnManager.AttactMode.PLAYER:
						CQData.sendEndTurn()
					await animation(animationType.PLANT_MOVE_PLAN_TURN)
					TurnManager.setState(TurnManager.Type.PLAN_TURN)
				TurnManager.Type.PLAN_TURN:
					if systemEmit == 0:return
					await animation(animationType.PLANT_MOVE_ATTACK_TURN)
					TurnManager.setState(TurnManager.Type.ATTACK_TURN)
				TurnManager.Type.ATTACK_TURN:
					if systemEmit == 0:return
					await animation(animationType.PLANT_BACK_CARD_TURN)
					TurnManager.setState(TurnManager.Type.CARD_TURN)
					TurnManager.attackOnce = false
		elif team == 2:
			match turnState:
				TurnManager.Type.CARD_TURN:
					if systemEmit == 0:return
					await animation(animationType.ZOMBIE_START_ZOMBIE_TURN)
					TurnManager.setState(TurnManager.Type.ZOMBIE_TURN)
				TurnManager.Type.ZOMBIE_TURN:
					if TurnManager.attackMode == TurnManager.AttactMode.PLAYER:
						CQData.sendEndTurn()
					await animation(animationType.ZOMBIE_MOVE_PLANT_TURN)
					TurnManager.setState(TurnManager.Type.PLANT_TURN)
				TurnManager.Type.PLANT_TURN:
					if systemEmit == 0:return
					await animation(animationType.ZOMBIE_MOVE_PLAN_TURN)
					TurnManager.setState(TurnManager.Type.PLAN_TURN)
				TurnManager.Type.PLAN_TURN:
					if TurnManager.attackMode == TurnManager.AttactMode.PLAYER:
						CQData.sendEndTurn()
					await animation(animationType.ZOMBIE_MOVE_ATTACK_TURN)
					TurnManager.setState(TurnManager.Type.ATTACK_TURN)
				TurnManager.Type.ATTACK_TURN:
					if systemEmit == 0:return
					await animation(animationType.ZOMBIE_BACK_CARD_TURN)
					TurnManager.setState(TurnManager.Type.CARD_TURN)
					TurnManager.attackOnce = false
		
		TurnManager.turnChangeDown.emit()
		TurnManager.turnChangeEnd1.emit()
	if TurnManager.m_state == TurnManager.Type.ATTACK_TURN:
		while !AQ.queue.is_empty():
			#print("wait")
			await get_tree().process_frame
	AQ.addAction(res)
