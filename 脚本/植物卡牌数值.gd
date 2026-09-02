extends Node2D
class_name PlantCardValue
@onready var attack:int = int($"伤害/伤害数值".text)
@onready var health:int = int($"血量/血量数值".text)
@onready var cost:int = int($"阳光/阳光数值".text)
@onready var picture = $"阳光"
@onready var attackNode = $"伤害"
@onready var healthNode = $"血量"
func set_attack(val:int):
	$"伤害/伤害数值".text = str(val)
	if val == 0:
		attackNode.hide()
	else:
		attackNode.show()
func set_health(val:int):
	$"血量/血量数值".text = str(val)
	if val == 0:
		healthNode.hide()
	else:
		healthNode.show()
func set_cost(val:int):
	$"阳光/阳光数值".text = str(val)
func setPicture(type:String):
	match type:
		"SUN":
			picture.texture = load("res://素材/卡牌属性图片/frameSun.png")
		"BRAIN":
			picture.texture = load("res://素材/卡牌属性图片/inhnd_brains.png")
