using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// Fighter卡牌被打出额外攻击的Buff
/// </summary>
public class PlayedExtraAttackBuff : Buff
{
    public override Timing[] Timings => [Timing.AfterPlay];

    public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
    {
        if (card is FighterCardModel fighter)
        {
            if (timing == Timing.AfterPlay)
            {
                await fighter.Battle();
            }
        }
    }
}
