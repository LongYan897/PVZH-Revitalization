using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

public class FireBuff(int amount) : Buff
{
    private readonly int Amount = amount;
    public override Timing[] Timings => [Timing.BeforeLaneStart];
    public override int Priority => 1000;
    public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
    {
        if (card is FighterCardModel fighter)
        {
            if (!fighter.IsDie)
            {
                await fighter.ApplyDamage(Amount,Variable.VariableReason.Damaged | Variable.VariableReason.Diff);
            }
        }
    }
}
