extends AudioStreamPlayer
class_name AudioNode
func setVolume(percent:float,time:float = 1):
	var tween = create_tween()
	tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tween.tween_property(self,"volume_linear",percent,time)
	await tween.finished
func VolumeClear(time:float = 1):
	var tween = create_tween()
	tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tween.tween_property(self,"volume_linear",0,time)
	await tween.finished
	queue_free()
