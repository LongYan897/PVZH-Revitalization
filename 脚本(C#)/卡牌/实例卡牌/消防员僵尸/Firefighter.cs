using Card.Cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Metier;

public class Firefighter : FighterCardModel
{
    public override bool FriendTarget => true;
    private class FirefighterBuff : Buff
    {
        public override Timing[] Timings => [Timing.WhenRevealed];
        public override bool IsTransferable => false;
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if ((bool)parameters[1]) return;
            if (card is FighterCardModel model)
            {
                if ((FighterCardModel)parameters[0] == model)
                {
                    if (CardCmd.AnyZombie(z=>z != model))
                    {
                        var target = await CardCmd.PlayerChoiceZombie(null,card,true,z => z != model);
                        if (target != null)
                            await target.Bounced();
                    }
                }
            }
        }
    }
    protected override List<Buff> InitBuffs => [
        new FirefighterBuff()
        ];
}
