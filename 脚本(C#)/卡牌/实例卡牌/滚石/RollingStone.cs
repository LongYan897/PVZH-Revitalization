using Battle.Entity;
using Godot;
using System.Threading.Tasks;
using Target;

namespace Card;

public class RollingStone : CardModel
{
	public override bool TargetFilter(FighterCardModel cardModel)
	{
		return cardModel.Atk.Current <= 2;
	}
	public override async Task Play(ITarget target)
	{
		var tg = (Fighter)target;
		if (tg.Model.Camp == Camp.Plant)
		{
			await PlayInstantAnimation("intro", target);
			await tg.Model.Die();
		}
	}
}
