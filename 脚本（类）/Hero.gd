extends Node
class_name Hero
var health:int
var shield:int
enum Type{
	PLANT_HERO = 0,
	ZOMBIE_HERO,
	DISPLAY_ONLY,
}
func _init() -> void:
	health = 20
	shield = 0
