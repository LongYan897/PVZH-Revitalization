using Battle;
using Pack;
using Phrases;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 伤害免疫（无敌）：受到伤害时，把本次结算的伤害直接归零。<br/>
/// 默认一直有效，由调用方 <see cref="CardModel.RemoveBuff"/> 手动移除；<br/>
/// 也可以在构造时指定阶段，在该阶段结束时自动移除，<br/>
/// 例如 <c>new DamageImmunityBuff(Phrase.Fight)</c> = 本回合不会受到伤害。<br/>
/// 只挡伤害，不拦「消灭」「弹回」这类不走伤害结算的效果。
/// </summary>
public class DamageImmunityBuff : Buff
{
    private readonly Phrase? _expirePhrase;
    private readonly Timing[] _timings;

    public DamageImmunityBuff(Phrase? expirePhrase = null)
    {
        _expirePhrase = expirePhrase;
        _timings = expirePhrase is null
            ? [Timing.ModifyCardDamage]
            : [Timing.ModifyCardDamage, Timing.AfterPhraseEnd];
    }

    public override int Priority => 1000;

    public override bool IsTransferable => false;

    public override Timing[] Timings => _timings;

    public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
    {
        if (timing == Timing.AfterPhraseEnd)
        {
            if (_expirePhrase is not Phrase expire) return;
            if (parameters.Length == 0 || parameters[0] is not Phrase phrase || phrase != expire) return;

            await card.RemoveBuff(this, Variable.VariableReason.Self, true);
            return;
        }

        if (parameters.Length == 0 || parameters[0] is not AtkStack stack) return;

        if (!stack.TargetsCard(card)) return;

        stack.FinalDamage = 0;
    }

    [URL]
    public static string 无敌 => "不会受到伤害。";
}
