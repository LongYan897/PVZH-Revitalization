extends Node
var messageArray:Array
func addMessageBox(node):
	print("addMessageBox add node:",node)
	messageArray.append(node)
func deleteMessageBox(node):
	for i in range(messageArray.size()-1,-1,-1):
		var curNode = messageArray[i]
		if curNode == node || !is_instance_valid(curNode):
			messageArray.remove_at(i)
func isEmpty():
	return messageArray.is_empty()
