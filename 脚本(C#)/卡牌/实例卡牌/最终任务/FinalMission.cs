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
        if (Fighter.HasFighter(f => f.Model.Camp == Camp.Plant))
            return target is Fighter fighter && fighter.Model.Camp == Camp.Zombie;
        else
            return false;
    }
    public override bool FriendTarget => true;
    public override async Task Play(ITarget target)
    {
        if (target.CanBeFighter(out var zomb) && zomb.Camp == Camp.Zombie)
        {
            FighterCardModel plant = await CardCmd.PlayerChoiceFighter(null, this, (itg) => itg.Camp == Camp.Plant);
            
            await PlayInstantAnimation("intro", target);

            await zomb.Kill(Variable.VariableReason.Trick);

            if (plant != null)
            {
                await PlayInstantAnimation("hit", plant.Targeting());
                
                await plant.ApplyDamage(4,Variable.VariableReason.Zombie | Variable.VariableReason.Trick | Variable.VariableReason.Damaged);
            }
        }
    }
}
