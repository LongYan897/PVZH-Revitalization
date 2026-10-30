using Battle.Entity;
using Card.Cmd;
using Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pea;

public class SweetPea : FighterCardModel
{
    private class SweetBuff : Buff
    {
        public override Timing[] Timings => [Timing.AfterPlay,Timing.WhenMoved];
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (timing == Timing.AfterPlay)
            {
                if (card is FighterCardModel fighter)
                {
                    if (CardCmd.AnyZombie())
                    {
                        var target = await CardCmd.PlayerChoiceZombie(card.Player, card, true);
                        if (target is null) return;
                        await target.MoveTo(fighter.Road, Location.Zombie);
                    }
                }
            }

            if (timing == Timing.WhenMoved)
            {
                if (card is FighterCardModel fighter)
                {
                    if (parameters[1] is FighterCardModel fighter1)
                    {
                        if (fighter1.Camp.Zombie())
                        {
                            await fighter.Atk.Gain(card.GetSignInt("Strength"),Variable.VariableReason.Self | Variable.VariableReason.Plant);
                        }
                    }
                }
            }
        }
        public override bool IsTransferable => false;
    }
    protected override List<Buff> InitBuffs =>
        [new SweetBuff()];
}
