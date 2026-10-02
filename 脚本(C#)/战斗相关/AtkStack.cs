using Card;
using System.Collections.Generic;

namespace Battle;

/// <summary>
/// 一次攻击的结算上下文
/// 从攻击宣告到结算结束，贯穿各个时点
/// </summary>
public class AtkStack
{
    /// <summary>
    /// 攻击堆栈
    /// </summary>
    /// <param name="attacker">攻击者</param>
    /// <param name="targets">初始目标集合</param>
    /// <param name="road">道路</param>
    /// <param name="baseDamage">基础伤害</param>
    public AtkStack(FighterCardModel attacker, IEnumerable<IAttackable> targets, Road road, int baseDamage)
    {
        Attacker = attacker;
        Road = road;
        BaseDamage = baseDamage;
        FinalDamage = baseDamage;
        Targets = new List<IAttackable>(targets);
    }
    /// <summary>
    /// 重新选中目标
    /// </summary>
    /// <param name="targets"></param>
    public void Targeting(params  IAttackable[] targets)
    {
        Targets = [.. targets];
    }
    /// <summary>
    /// 增加目标
    /// </summary>
    /// <param name="targets"></param>
    public void AddTarget(params  IAttackable[] targets)
    {
        Targets.AddRange(targets);
    }
    /// <summary>
    /// 攻击方
    /// </summary>
    public FighterCardModel Attacker { get; init; }

    /// <summary>
    /// 本次攻击的目标集合
    /// </summary>
    public List<IAttackable> Targets { get; private set; }

    /// <summary>
    /// 所处的道路
    /// </summary>
    public Road Road { get; init; }

    /// <summary>
    /// 基础伤害（攻击力）
    /// </summary>
    public int BaseDamage { get; set; }

    /// <summary>
    /// 结算后的最终伤害
    /// </summary>
    public int FinalDamage { get; set; }

    /// <summary>
    /// 是否命中英雄
    /// </summary>
    public bool HitHero { get; set; }

    /// <summary>
    /// 额外攻击次数（狂热/双重打击累加）
    /// </summary>
    public int ExtraAttacks { get; set; }
}