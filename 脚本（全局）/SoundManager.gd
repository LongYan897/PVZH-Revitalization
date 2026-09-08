extends Node
var soundArr:Array
var soundTrack:Array[SoundTrack]
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
func _ready() -> void:
	for i in range(10):
		var sound := SoundTrack.new()
		soundTrack.append(sound)
func _process(delta: float) -> void:
	soundArr.clear()
func createSound(path:String,bus:Bus = Bus.MASTER,loop:bool = false,adb:int = 0) -> AudioNode:
	if soundArr.has(path):return
	soundArr.append(path)
	var stream = load(path)
	if stream == null:
		push_error("音频加载失败：" + path)
		return null
	var instance := AudioNode.new()
	add_child(instance)
	instance.stream = load(path)
	instance.bus = BusName[bus]
	instance.volume_db = adb
	instance.adb = adb
	if loop:
		if path.contains(".mp3"):instance.stream.loop = true
		elif path.contains(".wav"):instance.stream.loop_mode = AudioStreamWAV.LOOP_FORWARD
	else:instance.finished.connect(soundFinished.bind(instance))
	instance.play()
	return instance
func soundFinished(node):
	node.stop()
	node.queue_free()
func setAudioOnST(trackNum:int,path:String,time:float = 1,loop:bool = false,adb:int = 0):
	print("SoundManager set AudioTrack\t trackNum:" + str(trackNum) + "\tpath:" + path)
	var audio := createSound(path,Bus.MUSIC,loop,adb)
	trackNum = clamp(trackNum,0,9)
	if soundTrack[trackNum].soundNode:
		soundTrack[trackNum].soundNode.VolumeClear(time)
	soundTrack[trackNum].soundNode = audio
	var target_volume := db_to_linear(adb)
	audio.volume_linear = 0
	audio.setVolume(target_volume,time)
func clearAudioST(trackNum:int,time:float = 1):
	print("SoundManager clear AudioTrack\t trackNum:" + str(trackNum))
	trackNum = clamp(trackNum,0,9)
	if soundTrack[trackNum].soundNode:
		soundTrack[trackNum].soundNode.VolumeClear(time)
	soundTrack[trackNum].soundNode = null
func setAudioVolOnST(trackNum:int,percent:float,time:float = 1):
	trackNum = clamp(trackNum,0,9)
	if soundTrack[trackNum].soundNode:
		soundTrack[trackNum].soundNode.setVolume(percent,time)
