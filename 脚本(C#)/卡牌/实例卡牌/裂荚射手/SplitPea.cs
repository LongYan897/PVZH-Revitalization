using Battle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pea;

public class SplitPea : FighterCardModel
{
    public override string AmmoPath => "res://素材(C#)/卡牌/豌豆射手/PeaAmmo.png";
    private class SplitBuff : Buff
    {
        public override Timing[] Timings => [Timing.BeforeLaneStart];
        public override int Priority => 100;
        public override bool IsTransferable => false;
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (card is FighterCardModel fighter)
            {
                if (parameters[0] is Road road)
                {
                    if (road == fighter.Road)
                    {
                        await fighter.Attack(card.Player.Status.AtkMe());
                        await card.Player.Status.Hero.ApplyDamage(card.GetSignInt("Health"),Variable.VariableReason.Plant | Variable.VariableReason.Damaged);
                    }
                }
            }
        }
    }
    protected override List<Buff> InitBuffs =>
        [new SplitBuff()];
}
