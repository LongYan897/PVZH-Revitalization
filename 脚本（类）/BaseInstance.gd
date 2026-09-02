extends Resource
class_name BaseInstance
func get_attack()->int:
	return 0
func get_health()->int:
	return 0
func get_cost()->int:
	return 0
func get_cost_Ori()->int:
	return 0
func get_card_name()->String:
	return ""
func getBuffVal(type:Buff.Type):
	return null
func getStrPro():
	return
func getTargetPath()->String:
	return ""
func getCardPath()->String:
	return ""
func getCardType()->BaseData.CardType:
	return BaseData.CardType.TARGET
func getData()->BaseData:
	return null
func isEqual(data)->bool:
	return false
func dict():
	pass
