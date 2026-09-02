extends Resource
class_name Buff
enum Type{
	STONE = 0,
}
@export var type:Type
@export var val:int = 1
func _init(buff:Buff = null) -> void:
	if buff == null:
		return
	if buff is Buff:
		type = buff.type
		val = buff.val
func isEqual(data:Buff)->bool:
	if type != data.type:return false
	if val != data.val:return false
	return true
func dict():
	return{
		"type":type,
		"val":val
	}
static func from_dict(data):
	var buff = Buff.new()
	buff.type = data.type
	buff.val = data.val
	return buff
