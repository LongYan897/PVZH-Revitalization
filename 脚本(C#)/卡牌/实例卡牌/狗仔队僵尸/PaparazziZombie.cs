using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Identity;

public class PaparazziZombie : FighterCardModel
{
    private class PaparazziBuff : Buff
    {
        public override bool IsTransferable => false;
        public override Timing[] Timings => [Timing.AfterCardPlayed];
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (card is FighterCardModel fighter)
            {
                var car = (CardModel)parameters[0];
                if (car.Camp.Zombie() && car.CardType.HasFlag(CardType.Trick))
                {
                    await Task.WhenAll(
                        fighter.Atk.Gain(fighter.GetSignInt("Strength"),Variable.VariableReason.Self | Variable.VariableReason.Zombie | Variable.VariableReason.Fighter),
                        fighter.GainHp(fighter.GetSignInt("Health"), Variable.VariableReason.Self | Variable.VariableReason.Zombie | Variable.VariableReason.Fighter)
                        );
                }
            }
        }
    }
    protected override List<Buff> InitBuffs => [];
}
