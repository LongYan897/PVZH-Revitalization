using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pea;

public class PodFather : FighterCardModel
{
    private class PodBuff : Buff
    {
        public override Timing[] Timings => [Timing.OnFighterEnter];
        public override int Priority => 10;
        public override bool IsTransferable => false;
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (card is FighterCardModel fighter)
            {
                if (parameters[1] is FighterCardModel fighter1)
                {
                    if (fighter1.HasLabel("豌豆"))
                    {
                        await Task.WhenAll(
                            fighter1.Atk.Gain(card.GetSignInt("Attack"),Variable.VariableReason.Plant),
                            fighter1.Hp.Gain(card.GetSignInt("Health"),Variable.VariableReason.Plant));
                    }
                }
            }
        }
    }
    protected override List<Buff> InitBuffs => [
        new PodBuff()
        ];
}
