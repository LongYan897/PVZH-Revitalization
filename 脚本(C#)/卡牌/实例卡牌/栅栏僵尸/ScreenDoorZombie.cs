using Phrases;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Card.Hearty;

/// <summary>
/// 栅栏僵尸<br/>
/// 墓碑：现身时，这名僵尸在本回合不会受到伤害。
/// </summary>
public class ScreenDoorZombie : FighterCardModel
{
    private class ScreenDoorBuff : Buff
    {
        public override bool IsTransferable => false;

        public override Timing[] Timings => [Timing.WhenRevealed];

        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (card is not FighterCardModel model) return;
            if (parameters.Length < 2) return;

            if (parameters[0] is not FighterCardModel revealed || revealed != model) return;

            if (parameters[1] is bool forced && forced) return;

            await model.AddBuff(new DamageImmunityBuff(Phrase.Fight), Variable.VariableReason.Self);
        }
    }

    protected override List<Buff> InitBuffs => [new ScreenDoorBuff()];
}
