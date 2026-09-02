extends Resource
class_name BaseData
enum Type{
	Ground = 0,
	Amphibious,
}
enum CardType{
	TARGET = 0,
	PLAN,
	ENVIRONMENT,
}
@export var cardType:CardType = CardType.TARGET
func getCardType()->CardType:
	return cardType
