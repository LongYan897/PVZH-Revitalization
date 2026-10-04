using Battle.Entity;
using Controller;
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
            WavPlayer.Play("res://素材(C#)/卡牌/滚石/intro_1.wav");
            await PlayInstantAnimation("intro", target);
            await tg.Kill(Variable.VariableReason.Zombie | Variable.VariableReason.Trick);
        }
    }
}