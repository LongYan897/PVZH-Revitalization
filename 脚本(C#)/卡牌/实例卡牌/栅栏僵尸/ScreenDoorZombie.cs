using Battle;
using Phrases;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Card.Hearty;

/// <summary>
/// 栅栏僵尸。<br/>
/// 墓碑：现身时，这名僵尸在本回合不会受到伤害。
/// </summary>
public class ScreenDoorZombie : FighterCardModel
{
    /// <summary>
    /// 监听自己现身，给自己挂上“本回合免伤”。
    /// </summary>
    private class ScreenDoorBuff : Buff
    {
        private bool _granted;

        public override bool IsTransferable => false;

        public override Timing[] Timings => [Timing.WhenRevealed];

        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (_granted) return;
            if (card is not FighterCardModel model) return;
            if (parameters.Length < 2) return;

            // 只有“现身的是自己”时才触发
            if (parameters[0] is not FighterCardModel revealed || revealed != model) return;

            // 死亡等强制现身不算
            if (parameters[1] is bool forced && forced) return;

            _granted = true;
            await model.AddBuff(new DamageImmunityBuff(), Variable.VariableReason.Self);
        }
    }

    /// <summary>
    /// 本回合不会受到任何伤害：把这次结算的最终伤害清零，战斗阶段结束时自行移除。
    /// </summary>
    private class DamageImmunityBuff : Buff
    {
        public override bool IsTransferable => false;

        /// <summary>比其他改伤效果后结算，保证最终为 0。</summary>
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

            // 只在本回合的战斗阶段结束时移除
            if (parameters.Length == 0 || parameters[0] is not Phrase phrase || phrase != Phrase.Fight)
                return;

            await card.RemoveBuff(this, Variable.VariableReason.Self, true);
        }
    }

    protected override List<Buff> InitBuffs => [new ScreenDoorBuff()];
}
