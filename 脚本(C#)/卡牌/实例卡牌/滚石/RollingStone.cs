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
            return fighter.Model.Atk.Current <= 2;
        return false;
    }

    public override async Task Play(ITarget target)
    {
        var tg = (Fighter)target;
        if (tg.Model.Camp == Camp.Plant)
        {
            WavPlayer.Play("res://素材(C#)/卡牌/滚石/intro_1.wav");
            await PlayInstantAnimation("intro", target);

            await tg.Model.Kill(Variable.VariableReason.Zombie | Variable.VariableReason.Trick);
        }
    }
}