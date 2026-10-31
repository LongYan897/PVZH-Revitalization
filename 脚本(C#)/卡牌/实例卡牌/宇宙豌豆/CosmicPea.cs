using Card.Cmd;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pea;

public class CosmicPea : FighterCardModel
{
    private class CosmicPeaBuff : Buff
    {
        public override Timing[] Timings => [Timing.AfterPlay];
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (card is FighterCardModel)
            {
                var pea = await CardCmd.MakeRandom(null,(c=>c.HasLabel("豌豆") ,1)).First() as FighterCardModel;
                await pea.MergeAtk(new DoubleAtkType());
            }
        }
        public override bool IsTransferable => false;
    }
    protected override List<Buff> InitBuffs =>
        [new CosmicPeaBuff()];
}
