using Battle;
using Phrases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

public class DamageImmunityBuff : Buff
{
    public override bool IsTransferable => false;

    public override int Priority => 1000;

    public override Timing[] Timings => [Timing.ModifyCardDamage, Timing.AfterPhraseEnd];

    public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
    {
        if (timing == Timing.ModifyCardDamage)
        {
            if (parameters[0] is AtkStack stack)
                stack.FinalDamage = 0;
            return;
        }

        if (parameters.Length == 0 || parameters[0] is not Phrase phrase || phrase != Phrase.Fight)
            return;

        await card.RemoveBuff(this, Variable.VariableReason.Self, true);
    }
}