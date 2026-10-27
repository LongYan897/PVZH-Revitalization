using Card;
using Godot;
using System.Collections.Generic;
using Target;

namespace Battle;

/// <summary>
/// 一次攻击的结算上下文
/// 从攻击宣告到结算结束，贯穿各个时点
/// </summary>
public class AtkStack
{
    /// <summary>
    /// 根攻击上下文
    /// </summary>
    public AtkStack Root { get; private init; }
    /// <summary>
    /// 一条子弹路径
    /// 子弹从 FirePointBone 指定的骨骼出发（为空时用默认骨骼），沿 Curve 飞行
    /// </summary>
    public sealed class ShotCurve
    {
        /// <summary>
        /// 子弹经过的路径点（不含起点）
        /// </summary>
        public Curve2D Curve { get; } = new();

        /// <summary>
        /// 发射骨骼名，为空时使用默认骨骼
        /// </summary>
        public string FirePointBone { get; set; }

        public ShotCurve(string firePointBone = null) { FirePointBone = firePointBone; }
    }

    private readonly Dictionary<IAttackable, List<ShotCurve>> _shots = new();

    /// <summary>
    /// 本次攻击的子弹路径表
    /// </summary>
    public IReadOnlyDictionary<IAttackable, List<ShotCurve>> Shots => _shots;

    /// <summary>
    /// 注册一个直线子弹目标（无路径，从默认骨骼发射）
    /// </summary>
    public void AddShot(IAttackable target)
    {
        if (target == null) return;
        if (!_shots.ContainsKey(target))
            _shots[target] = new List<ShotCurve>();
    }

    /// <summary>
    /// 为某个目标追加一条子弹路径
    /// </summary>
    /// <param name="target">子弹的目标</param>
    /// <param name="firePointBone">发射骨骼名，为空时使用默认骨骼</param>
    /// <returns>创建的 ShotCurve，可继续往 Curve 里加路径点</returns>
    public ShotCurve AddCurve(IAttackable target, string firePointBone = null)
    {
        if (target == null) return null;
        if (!_shots.TryGetValue(target, out var list))
        {
            list = new List<ShotCurve>();
            _shots[target] = list;
        }
        var sc = new ShotCurve(firePointBone);
        list.Add(sc);
        return sc;
    }

    /// <summary>
    /// 清空所有子弹路径
    /// </summary>
    public void ClearShots() => _shots.Clear();

    /// <summary>
    /// 攻击堆栈
    /// </summary>
    /// <param name="attacker">攻击者</param>
    /// <param name="targets">初始目标集合</param>
    /// <param name="road">道路</param>
    /// <param name="baseDamage">基础伤害</param>
    public AtkStack(FighterCardModel attacker, IEnumerable<IAttackable> targets, Road road, int baseDamage, bool isRoadBattle)
    {
        Attacker = attacker;
        Road = road;
        BaseDamage = baseDamage;
        FinalDamage = baseDamage;
        Targets = new List<IAttackable>(targets);
        RoadBattle = isRoadBattle;
    }

    /// <summary>
    /// 为单个目标生成独立的攻击上下文副本
    /// </summary>
    public AtkStack ForTarget(IAttackable target)
    {
        return new AtkStack(Attacker, [target], Road, BaseDamage, RoadBattle)
        {
            Root = Root ?? this,
            FinalDamage = FinalDamage,
            HitHero = HitHero,
            IgnoreShield = IgnoreShield,
            Pierce = Pierce,
            TargetsRedirected = TargetsRedirected
        };
    }

    /// <summary>
    /// 重新选中目标
    /// </summary>
    /// <param name="targets"></param>
    public void Targeting(params IAttackable[] targets)
    {
        Targets = [.. targets];
    }

    /// <summary>
    /// 替换目标
    /// </summary>
    /// <param name="targets"></param>
    public void ReplaceTarget(IAttackable target)
    {
        var ct = Targets.Count;
        Targets.Clear();
        for (int i = 0; i < ct; i++)
            Targets.Add(target);
    }

    /// <summary>
    /// 增加目标
    /// </summary>
    /// <param name="targets"></param>
    public void AddTarget(params IAttackable[] targets)
    {
        Targets.AddRange(targets);
    }

    /// <summary>
    /// 攻击方
    /// </summary>
    public FighterCardModel Attacker { get; init; }

    /// <summary>
    /// 是否是道路战斗
    /// </summary>
    public bool RoadBattle { get; init; }

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
    private int _extraAttacks;

    /// <summary>
    /// 额外攻击次数（狂热/双重打击累加）
    /// </summary>
    public int ExtraAttacks
    {
        get => Root != null ? Root._extraAttacks : _extraAttacks;
        set => (Root ?? this)._extraAttacks = value;
    }

    /// <summary>
    /// 不增加护盾值
    /// </summary>
    public bool IgnoreShield { get; set; }

    /// <summary>
    /// 本次攻击是否穿透
    /// </summary>
    public bool Pierce { get; set; }

    /// <summary>
    /// 目标是否已被重定向为单一目标
    /// </summary>
    public bool TargetsRedirected { get; set; }
}