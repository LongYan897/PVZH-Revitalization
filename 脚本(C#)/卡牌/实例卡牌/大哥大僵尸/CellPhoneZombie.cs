using Card.Cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Identity;

public class CellPhoneZombie : FighterCardModel
{
    private class CellBuff : Buff
    {
        public override bool IsTransferable => false;
        public override int Priority => 200;
        public override Timing[] Timings => [Timing.AfterPlay];
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            await CardCmd.Draw(null, card);
        }
    }
    protected override List<Buff> InitBuffs => [];
}
