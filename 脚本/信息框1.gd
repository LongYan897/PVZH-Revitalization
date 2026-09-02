extends Panel
class_name InformationBox
var _tween: Tween
func _ready() -> void:
	size = Vector2.ZERO
func setSize(pSize: Vector2, center_ratio: Vector2 = Vector2(0.5, 0.5),time = 0.2) -> void:
	if _tween and _tween.is_running():
		_tween.kill()
	center_ratio = center_ratio.clamp(Vector2.ZERO, Vector2.ONE)
	var fixed_point := position + size * center_ratio
	var target_position := fixed_point - pSize * center_ratio
	_tween = create_tween()
	_tween.set_trans(Tween.TRANS_QUAD)
	_tween.set_ease(Tween.EASE_IN_OUT)
	_tween.parallel().tween_property(self, "size", pSize, time)
	_tween.parallel().tween_property(self, "position", target_position, time)
	await get_tree().create_timer(time).timeout
	size = pSize
	position = target_position
