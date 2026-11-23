using System.Threading.Tasks;

namespace Card;

/// <summary>
/// Fighter卡牌被打出额外攻击的Buff
/// </summary>
public class PlayedExtraAttackBuff(params Timing[] timing) : Buff
{
    public override Timing[] Timings => timing;

    public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
    {
        if (card is FighterCardModel fighter)
        {
            await fighter.Battle();
        }
    }
}
