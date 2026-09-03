@tool
extends Sprite2D
class_name CardBox
enum CardBoxType{
	Target,
	Plan,
	Env,
	Hero_,
}
enum CardBoxTarType{
	TarCommon = 0,
	TarUncommon,
	TarRare,
	TarSuperRare,
	TarLegendary,
	TarEvent,
}
enum CardBoxEnvType{
	EnvCommon = 0,
	EnvUncommon,
	EnvRare,
	EnvSuperRare,
	EnvLegendary,
	EnvEvent,
}
enum CardBoxPlanType{
	PlanCommon = 0,
	PlanUncommon,
	PlanRare,
	PlanSuperRare,
	PlanLegendary,
	PlanEvent,
}
enum CardBoxHeroType{
	HeroCommon = 0,
}
@export var cardType: CardBoxType = CardBoxType.Target:
	set(value):
		cardType = value
		cardRare = clampi(cardRare, 0, _rarity_count() - 1)
		notify_property_list_changed()
		_update_card_frame()

# 根据卡牌类型动态显示对应的稀有度枚举。
# 例如 Target -> TarCommon/TarRare，Plan -> PlanCommon/PlanRare。
@export var cardRare: int = 0:
	set(value):
		cardRare = clampi(value, 0, _rarity_count() - 1)
		_update_card_frame()

func _ready() -> void:
	_update_card_frame()
	if Engine.is_editor_hint():
		notify_property_list_changed()

func _update_card_frame() -> void:
	if not is_inside_tree():
		return
	var frame_path := _get_card_frame_path()
	if frame_path.is_empty():
		return
	var frame := load(frame_path) as Texture2D
	if frame:
		texture = frame

func _get_card_frame_path() -> String:
	var base := "res://素材/牌背、卡面/"
	match cardType:
		CardBoxType.Target:
			if cardRare == 0:
				return base + "SEEDPACKET_FRONT.png"
			if cardRare <= 4:
				return base + "SEEDPACKET_R" + str(cardRare + 1) + ".png"
			return base + "SEEDPACKET_Event.png"
		CardBoxType.Plan:
			if cardRare <= 4:
				return base + "SEEDPACKET_onetime_R" + str(cardRare + 1) + ".png"
			return base + "SEEDPACKET_onetime_Event.png"
		CardBoxType.Env:
			if cardRare <= 4:
				return base + "Environment_R" + str(cardRare + 1) + ".png"
			return base + "Environment_Back.png"
		CardBoxType.Hero_:
			return base + "SEEDPACKET_HeroPower_zombie.png"
	return ""

func _rarity_names() -> PackedStringArray:
	match cardType:
		CardBoxType.Target:
			return PackedStringArray(["TarCommon", "TarUncommon", "TarRare", "TarSuperRare", "TarLegendary", "TarEvent"])
		CardBoxType.Plan:
			return PackedStringArray(["PlanCommon", "PlanUncommon", "PlanRare", "PlanSuperRare", "PlanLegendary", "PlanEvent"])
		CardBoxType.Env:
			return PackedStringArray(["EnvCommon", "EnvUncommon", "EnvRare", "EnvSuperRare", "EnvLegendary", "EnvEvent"])
		CardBoxType.Hero_:
			return PackedStringArray(["HeroCommon"])
	return PackedStringArray(["TarCommon"])

func _rarity_count() -> int:
	return _rarity_names().size()

func _validate_property(property: Dictionary) -> void:
	if property.name == "cardRare":
		property.hint = PROPERTY_HINT_ENUM
		property.hint_string = ",".join(_rarity_names())
