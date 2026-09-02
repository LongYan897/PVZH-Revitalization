extends Node
signal AQSizeChange
@onready var queue:Array[AQData] = []
var threadCount:int = 0
var count:int = 0
func _ready() -> void:
	handleAction()
func handleAction():
	while 1:
		if !queue.is_empty() && queue[0].visit == false:
			await excuteQueue(0)
			continue
		await get_tree().process_frame
func addAction(action:Callable):
	count += 1
	var data = AQData.new()
	data.action = action
	var object = action.get_object()
	var objectName
	if object is Node:
		data.node = object
		objectName = object.name
	else:
		data.node = null
		objectName = "null"
	data.visit = false
	data.number = count
	var node = data.node
	queue.append(data)
	AQSizeChange.emit()
	print("AQ add ",action,"\tnumber:",data.number,"\tsize:",queue.size(),"\tname:",objectName)
func waitQueueEmpty():
	while !queue.is_empty():
		await get_tree().create_timer(0.05).timeout
func getlen():
	return queue.size()
func addThread(mlen):
	threadCount += 1
	print("AQ addThread \tsize:",threadCount)
	while AQ.getlen() > mlen:
		await excuteQueue(mlen)
	threadCount -= 1
	print("AQ delateThread \tsize:",threadCount)
	await get_tree().process_frame
func excuteQueue(len):
	var queueNode = queue[len]
	var action:Callable = queue[len].action
	var node = queue[len].node
	queue[len].visit = true
	if is_instance_valid(node) && node!= null && action != null:
		await action.call()
	print("AQ delete ",action,"\tnumber:",queueNode.number,"\tsize:",queue.size()-1)
	queue.erase(queueNode)
	AQSizeChange.emit()
