using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Nuts;

public class Nut : FighterCardModel
{
    private class NutBuff : Buff
    {
        public override bool IsTransferable => false;

        public override int Priority => 1000;

        public override Timing[] Timings => [Timing.WhenAttacked];

        private int atkedA;

        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (atkedA <= 2)
            {
                atkedA++;
                if (card is FighterCardModel fighter)
                {
                    if (atkedA == 1)
                        fighter.SwitchAttachment("mirrornut_body", "wallnut_body_base_02");
                    else
                        fighter.SwitchAttachment("mirrornut_body", "wallnut_body_base_03");
                }
            }
        }
    }

    protected override List<Buff> InitBuffs => [new NutBuff()];
}
