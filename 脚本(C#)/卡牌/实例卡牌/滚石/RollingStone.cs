using Battle.Entity;
using Controller;
using Controller.WavPlay;
using Godot;
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
            await PlayInstantAnimation("intro", target);
            await tg.Kill(Variable.VariableReason.Zombie | Variable.VariableReason.Trick);
        }
    }
}