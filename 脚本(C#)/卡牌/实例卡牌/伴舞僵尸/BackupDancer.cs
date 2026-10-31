using Battle.Entity;
using Card.Cmd;
using Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Target;

namespace Card.Dance;

public class BackupDancer : FighterCardModel
{
    private class BackupDancerBuff : Buff
    {
        public override Timing[] Timings => [Timing.OnFighterExit, Timing.AfterPlay];
        private FighterCardModel target;
        private Func<Task> redo;
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (timing == Timing.AfterPlay)
            {
                if (card is FighterCardModel fighterCard)
                {
                    if (CardCmd.AnyZombie(z => z.HasLabel("跳舞")))
                    {
                        FighterCardModel fighter = await CardCmd.PlayerChoiceZombie(null, card, true, (z) => z.HasLabel("跳舞"));
                        target = fighter;
                        if (target != null)
                        {
                            await fighterCard.PlayFighterAnimationTask("attack");
                            redo = await fighter.Atk.Gain(card.GetSignInt("Strength"), Variable.VariableReason.Zombie | Variable.VariableReason.Self);
                        }

                    }
                }
            }
            if (timing == Timing.OnFighterExit)
            {
                if (card is FighterCardModel fighterCard && fighterCard.IsDie)
                {
                    if (parameters[1] == fighterCard || parameters[1] == target)
                    {
                        if (redo != null) 
                            await redo();
                    }
                }
            }
        }
    }
    public override bool FriendTarget => true;
    protected override List<Buff> InitBuffs => [
        new BackupDancerBuff()
        ];
}
