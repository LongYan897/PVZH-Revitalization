extends Node2D
class_name BaseCard
enum AddChoiceType{
	ROAD = 0,
	TARGET,
}
var offset:Vector2 = Vector2(0,0)
var isDrag:bool = false
var isOver:bool = false
var isPlace:bool = false
var c_Line:int = 0
var c_Col:int = 0
var index
var tween:Tween
var type:CardManager.CardType = CardManager.CardType.NULL
var displayY:float = 0
var displayNode
var tarScale:Vector2 = Vector2(0.55,0.55)
var buttonPressed:bool = false
var informationBox:InformationBox
var information
var dragTime:float = 0
var touchPos:Vector2 = Vector2.ZERO
var showLock:bool = false
var dragInformation
var chooseTarget
func intro():
	scale = Vector2(0,0)
	modulate = Color(0,0,0,0)
	tween = create_tween()
	tween.tween_property(self,"modulate",Color(1,1,1,1),0.2)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_OUT)
	var tween1 = create_tween()
	tween1.tween_property(self,"scale",tarScale,0.4)\
	.set_trans(Tween.TRANS_BACK)\
	.set_ease(Tween.EASE_OUT)
func clear():
	tween = create_tween()
	var tween1 = create_tween()
	scale = Vector2(0.6,0.6)
	modulate = Color(1,1,1,1)
	tween.tween_property(self,"scale",Vector2(0,0),0.2)\
	.set_trans(Tween.TRANS_BACK)\
	.set_ease(Tween.EASE_IN)
	tween1.tween_property(self,"modulate",Color(0,0,0,0),0.5)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN)
	await tween.finished
	await get_tree().create_timer(1).timeout
	queue_free()
func setType(mtype:String):
	if mtype == "display":
		type = CardManager.CardType.LIBRARY
		tarScale = Vector2(0.6,0.6)
		intro()
func setDisplayY(y):
	displayY = y
func setDisplayNode(node):
	displayNode = node
func setCardNumber():
	c_Line = CardManager.getLine(self)
	c_Col = CardManager.getCol(self)
func addChoice(path:Vector2,type:AddChoiceType = AddChoiceType.ROAD)->Node2D:
	var choiceText = load("res://场景/UI对象/选择高亮.tscn").instantiate()
	add_child(choiceText)
	choiceText.intro()
	choiceText.setRoad(path)
	choiceText.name = "选择"+ str(int(path.x))+"-"+ str(int(path.y))
	match type:
		AddChoiceType.ROAD:
			pass
		AddChoiceType.TARGET:
			choiceText.loadSprite("res://素材/对象高亮框/单位1.png")
			choiceText.position = path
			choiceText.setExSpriteSca(2)
			choiceText.introAnimation1()
	return choiceText
func updatePosition():
	if type != CardManager.CardType.NULL:return
	if isPlace == true:return
	if isDrag == true:return
	var pos:Vector2 = Vector2(0,0)
	var CardCount = CardManager.getCardCountAll()
	if CardCount <=4:
		pos = Vector2(365-145*((CardCount/2.0)-0.5-(c_Col)),1135)
	elif c_Line == 1:
		var currentCount = CardManager.getCardCount1()
		pos = Vector2(365-145*((currentCount/2.0)-0.5-(c_Col)),1070)
	elif c_Line == 2:
		var currentCount = CardManager.getCardCount2()
		pos = Vector2(365-145*((currentCount/2.0)-0.5-(c_Col)),1200)
	if tween : tween.kill()
	tween = create_tween()
	tween.tween_property(self,"position",pos,0.5)\
	.set_trans(Tween.TRANS_QUAD)\
	.set_ease(Tween.EASE_IN_OUT)
func getDataInstance():
	pass
func getData():
	pass
func showLabelClear(quickMode:bool = false):
	if quickMode == true:
		if dragInformation != null && is_instance_valid(dragInformation):
			dragInformation.quickClear()
	else:
		if dragInformation != null && is_instance_valid(dragInformation):
			dragInformation.clear()
