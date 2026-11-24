using Card.Cmd;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Card.Metier;

public class Firefighter : FighterCardModel
{
    public override bool FriendTarget => true;
    private class FirefighterBuff : Buff
    {
        public override Timing[] Timings => [Timing.WhenRevealed,Timing.BuffApply];
        public override bool IsTransferable => false;
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (timing == Timing.WhenRevealed)
            {
                if ((bool)parameters[1]) return;
                if (card is FighterCardModel model)
                {
                    if ((FighterCardModel)parameters[0] == model)
                    {
                        if (CardCmd.AnyZombie(z => z != model))
                        {
                            var target = await CardCmd.PlayerChoiceZombie(null, card, true, z => z != model);
                            if (target != null)
                            {
                                await model.PlayFighterAnimationTask("special1");
                                await target.Bounced();
                            }
                        }
                    }
                }
            }
            else if (timing == Timing.BuffApply)
            {
                if (card is FighterCardModel model)
                {
                    if ((Buff)parameters[0] is not FireBuff) return;
                    if ((CardModel)parameters[1] is FighterCardModel fighter && fighter.Camp.Zombie() && (CardCmd.GetBehindRoad(model.Road).Contains(fighter.Road) || fighter == model))
                    {
                        await model.PlayFighterAnimationTask("special1");
                        await model.RemoveBuff((Buff)parameters[0],Variable.VariableReason.Zombie | Variable.VariableReason.Fighter,true);
                    }
                }
            }
        }
    }
    protected override List<Buff> InitBuffs => [
        new FirefighterBuff()
        ];
}
