using Battle.Entity;
using Controller;
using Controller.WavPlay;
using System.Threading.Tasks;
using Target;

namespace Card;

public class RollingStone : CardModel
{
	public override bool TargetFilter(ITarget target)
	{
		if (target is Fighter fighter)
			return fighter.Model.Atk.Current <= GetSignInt("Strength");
		return false;
	}

	public override async Task Play(ITarget target)
	{
		if (target.CanBeFighter(out FighterCardModel tg))
		{
			WavPlayer.Play<RollingStoneWav>();
			await PlayLocationalAnimation("intro","intro1", target);
			await tg.Die(Variable.VariableReason.Zombie | Variable.VariableReason.Trick);
		}
	}
}
