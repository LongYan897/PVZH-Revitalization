extends Node2D
class_name BaseTarget
enum Type{
	Display_only = 0,
	Normal,
}
var line:int = -1
var col:int = -1
var type:Type = Type.Normal
var isOver:bool = false
var isDrag:bool = false
var dragTime:float = 0
var faction:PVZ.Type = PVZ.Type.PLANT_AND_ZOMBIE
@onready var is_alive: bool = true
@onready var attackEffect = load("res://场景/攻击特效/基础攻击特效.tscn")
var stone
var planClear:bool = false
func introEvent():
	pass
func extraReady():
	pass
func setInstance(instance):
	pass
func planIntro():
	pass
func waitAnimationComP(track:int = 0,precent:float = 1):
	pass
func afterIntro():
	pass
