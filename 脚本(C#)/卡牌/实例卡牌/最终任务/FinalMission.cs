using Battle.Entity;
using Card.Cmd;
using Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Target;

namespace Card;

public class FinalMission : CardModel
{
    public override bool TargetFilter(ITarget target)
    {
        return target is Fighter fighter && fighter.Model.Camp == Camp.Zombie;
    }
    public override bool FriendTarget => true;
    public override async Task Play(ITarget target)
    {
        var zomb = target as Fighter;
        if (zomb.Model.Camp == Camp.Zombie)
        {
            Fighter plant = (Fighter)(await CardCmd.PlayerChoiceTarget(null, this, TargetType.Fighters, (itg) => itg is Fighter fighter && fighter.Model.Camp == Camp.Plant));
            await PlayInstantAnimation("intro", zomb);
            await zomb.Model.Die();
            if (plant != null && plant.Model.Camp == Camp.Plant)
            {
                await PlayInstantAnimation("hit", plant);
                await plant.Model.Hp.Lose(4,Variable.VariableReason.Zombie | Variable.VariableReason.Trick);
            }
        }
    }
}
