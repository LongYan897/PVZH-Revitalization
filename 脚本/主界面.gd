extends Node2D
@onready var uiCard = $"卡组"
@onready var uiAct = $"活动"
@onready var uiPack = $"开包"
@onready var uiFriend = $"好友"
@onready var plantBattleButton = $"植物战斗按钮"
@onready var zombieBattleButton = $"僵尸战斗按钮"
@onready var eventBattleButton = $"今日挑战"
@onready var BattleButton = $"对战"
var isEnd:bool = false
func _ready() -> void:
	SoundManager.setAudioOnST(0,"res://素材/Audio/Main Menu.mp3",0.5,true)
func quickCardInput(node,_viewport: Node, event: InputEvent, _shape_idx: int):
	if event is InputEventScreenTouch:
		if event.pressed:
			var tween = create_tween()
			tween.tween_property(node,"scale",Vector2(0.6,0.6),0.15)\
			.set_trans(Tween.TRANS_QUAD)\
			.set_ease(Tween.EASE_IN_OUT)
		else:
			var tween = create_tween()
			tween.tween_property(node,"scale",Vector2(0.7,0.7),0.15)\
			.set_trans(Tween.TRANS_QUAD)\
			.set_ease(Tween.EASE_IN_OUT)
			return 1
func quickCardExited(node,mscale:Vector2 = Vector2(0.7,0.7)):
	var tween = create_tween()
	tween.tween_property(node,"scale",mscale,0.15)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
func quickCardPressed(node,mscale:Vector2 = Vector2(0.8,0.8)):
	var tween = create_tween()
	tween.tween_property(node,"scale",mscale,0.15)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
func uiCardInput(viewport: Node, event: InputEvent, shape_idx: int) -> void:
	if isEnd == true:return
	var res = quickCardInput(uiCard,viewport,event,shape_idx)
	if res == 1:
		isEnd = true
		SceneManager.change_scene("res://场景/UI对象/卡组界面.tscn",{"speed":3.0,"wait_time":0.2})
func uiCardMouseExited() -> void:
	quickCardExited(uiCard)
func uiActInput(viewport: Node, event: InputEvent, shape_idx: int) -> void:
	var _res = quickCardInput(uiAct,viewport,event,shape_idx)
func uiActMouseExited() -> void:
	quickCardExited(uiAct)
func uiPackInput(viewport: Node, event: InputEvent, shape_idx: int) -> void:
	var _res = quickCardInput(uiPack,viewport,event,shape_idx)
func uiPackMouseExited() -> void:
	quickCardExited(uiPack)
func uiFriendInput(viewport: Node, event: InputEvent, shape_idx: int) -> void:
	var _res = quickCardInput(uiFriend,viewport,event,shape_idx)
func uiFriendMouseExited() -> void:
	quickCardExited(uiFriend)

func _on_植物战斗按钮_button_down() -> void:
	quickCardPressed(plantBattleButton)
func _on_植物战斗按钮_button_up() -> void:
	quickCardExited(plantBattleButton,Vector2(0.9,0.9))
func _on_僵尸战斗按钮_button_down() -> void:
	quickCardPressed(zombieBattleButton)
func _on_僵尸战斗按钮_button_up() -> void:
	quickCardExited(zombieBattleButton,Vector2(0.9,0.9))
func _on_今日挑战_button_down() -> void:
	quickCardPressed(eventBattleButton,Vector2(1.1,1.1))
func _on_今日挑战_button_up() -> void:
	quickCardExited(eventBattleButton,Vector2(1.25,1.25))
func _on_对战_button_down() -> void:
	quickCardPressed(BattleButton,Vector2(0.45,0.45))
func _on_对战_button_up() -> void:
	quickCardExited(BattleButton,Vector2(0.5,0.5))
func _on_对战_pressed() -> void:
	if isEnd == false:
		isEnd = true
		SceneManager.change_scene("res://场景/战斗场景/战斗场景.tscn",{"speed":3,"wait_time":0.5})
