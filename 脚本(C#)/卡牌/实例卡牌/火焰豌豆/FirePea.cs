using Battle;
using Logger;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Card.Pea;

public class FirePea : FighterCardModel
{
    private class FirePeaBuff : Buff
    {
        public override Timing[] Timings => [Timing.OnDamaged];
        public override bool IsTransferable => false;
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (card is FighterCardModel fighter)
            {
                if (parameters[0] is AtkStack stack)
                {
                    if (stack.Attacker == fighter)
                    {
                        var tasks = new List<Task>();
                        foreach (var tg in stack.Targets)
                        {
                            if (tg.InferAsCard() != null)
                                tasks.Add(tg.InferAsCard().AddBuff(new FireBuff(1),Variable.VariableReason.Plant | Variable.VariableReason.Diff));
                        }
                        await Task.WhenAll(tasks);
                    }
                }
            }
        }
        public override int Priority => -1000;
    }

    protected override List<Buff> InitBuffs =>
        [
        new FirePeaBuff()
        ];
}
