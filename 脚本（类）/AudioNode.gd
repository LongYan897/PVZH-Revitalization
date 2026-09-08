extends AudioStreamPlayer
class_name AudioNode
var adb:int = 0
func setVolume(percent:float,time:float = 1):
	var tarVol = db_to_linear(adb) * (percent / 1.0)
	var tween = create_tween()
	tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tween.tween_property(self,"volume_linear",tarVol,time)
	await tween.finished
func VolumeClear(time:float = 1):
	var tween = create_tween()
	tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tween.tween_property(self,"volume_linear",0,time)
	await tween.finished
	queue_free()
