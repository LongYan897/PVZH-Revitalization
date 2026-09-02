extends Node
var soundArr:Array
enum Bus {
	MASTER,
	EFFECT,
	MUSIC,
	UI
}
const BusName = {
	Bus.MASTER: "Master",
	Bus.EFFECT: "Effect",
	Bus.MUSIC: "Music",
	Bus.UI: "UI"
}
func _process(delta: float) -> void:
	soundArr.clear()
func createSound(path:String,bus:Bus = Bus.MASTER):
	if soundArr.has(path):return
	soundArr.append(path)
	var instance = AudioStreamPlayer.new()
	add_child(instance)
	instance.stream = load(path)
	instance.bus = BusName[bus]
	instance.finished.connect(soundFinished.bind(instance))
	instance.play()
func soundFinished(node):
	node.stop()
	node.queue_free()
