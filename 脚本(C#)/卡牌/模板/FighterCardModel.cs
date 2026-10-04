using Battle;
using Battle.Entity;
using Card.Cmd;
using Card.String;
using System;
using System.Linq;
using System.Threading.Tasks;
using Target;
using Variable;
using Variable.Special;
using static Godot.HttpRequest;

namespace Card;

/// <summary>
/// 单位类型的卡牌
/// </summary>
public class FighterCardModel : CardModel
{
    protected override CardModel LoadData(CardString cardString)
    {
        Atk = new(this, "Attack", cardString.Attack);
        BornWithAtk = Atk.Current > 0;
        AtkType = new(this, "AttackType", cardString.AtkType);
        Hp = new(this, "Health", cardString.Health);
        MaxHp = new(this, "HealthMax", cardString.Health);
        HpType = new(this, "HealthType", cardString.HpType);
        CardTag = cardString.CardTag;
        _ = AddBuff(new HpBuff(), VariableReason.Self);
        return LoadCustomData(cardString);
    }
    /// <summary>
    /// 控制单位血量不超过最大血量
    /// </summary>
    private class HpBuff : Buff
    {
        public override bool IsTransferable => false;
        public override Timing[] Timings => [Timing.OnVariableChanged];

        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (card is not FighterCardModel fighter) return;

            if (timing == Timing.OnVariableChanged)
            {
                if (parameters.Length < 1) return;
                var key = parameters[0] as string;
                if (key != "Health" && key != "HealthMax") return;
            }

            if (fighter.Hp.Current > fighter.MaxHp.Current)
                await fighter.Hp.Set(fighter.MaxHp.Current, VariableReason.Reset);
        }
    }
    public ITarget Targeting() => Fighter.GetNode(this);
    /// <summary>
    /// 所处的道路
    /// </summary>
    public Road Road { get; internal set; }
    /// <summary>
    /// 所在的位置
    /// </summary>
    public Location Location { get; internal set; }
    /// <summary>
    /// 创建一个攻击堆栈
    /// </summary>
    /// <returns></returns>
    private AtkStack CreateOwnedStack()
    => new(this, [], Road, Atk.Current);
    /// <summary>
    /// 额外攻击一次
    /// </summary>
    /// <returns></returns>
    public async Task Battle() => await Battle(CreateOwnedStack());
    /// <summary>
    /// 开始战斗
    /// </summary>
    public async Task Battle(AtkStack atkStack)
    {
        if (Atk.Current <= 0) return;
        var fighters = atkStack.Road.GetFighters();
        var target = (IAttackable)fighters.FirstOrDefault(
            f =>
            {
                if (Camp == Camp.Zombie)
                {
                    if (fighters.Count > 2)
                        return f.Plant1 && f.Model.Camp == Camp.Plant;
                    else
                        return f.Model.Camp == Camp.Plant;
                }
                else
                    return f.Model.Camp == Camp.Zombie;
            }
            );
        if (target == null) return;
        atkStack.Targeting(target);

        await FireTiming(Timing.ModifyTarget, atkStack);
        await CardCmd.TimingOnCards(this, Timing.ModifyTarget, atkStack);
        await FireTiming(Timing.ModifyDamage, atkStack);
        await CardCmd.TimingOnCards(this, Timing.ModifyDamage, atkStack);

        await Fighter.GetNode(this).Battle(atkStack.Targets, InstantAmmo);

        await FireTiming(Timing.OnAttack, atkStack);
        await CardCmd.TimingOnCards(this, Timing.OnAttack, atkStack);

        var finalTgs = atkStack.Targets.ToList();
        foreach (var itg in finalTgs)
        {
            if (itg is not Fighter fighter) continue;

            atkStack.FinalDamage = atkStack.BaseDamage;

            await fighter.Model.FireTiming(Timing.ModifyCardDamage, atkStack);
            await CardCmd.TimingOnCards(fighter.Model, Timing.ModifyCardDamage, atkStack);
            await fighter.Model.FireTiming(Timing.WhenAttacked, atkStack);
            await CardCmd.TimingOnCards(fighter.Model, Timing.WhenAttacked, atkStack);

            await fighter.Model.ApplyDamage(atkStack.FinalDamage, Variable.VariableReason.Fighter, atkStack, true, false);

            await fighter.Model.FireTiming(Timing.AfterAttacked, atkStack);
            await CardCmd.TimingOnCards(fighter.Model, Timing.AfterAttacked, atkStack);
            await FireTiming(Timing.ApplyTarget, atkStack);
            await CardCmd.TimingOnCards(this, Timing.ApplyTarget, atkStack);
        }

        await FireTiming(Timing.AfterAttack, atkStack);
        await CardCmd.TimingOnCards(this, Timing.AfterAttack, atkStack);

        if (atkStack.ExtraAttacks > 0)
        {
            atkStack.ExtraAttacks -= 1;
            await Battle(atkStack);
        }
    }

    /// <summary>
    /// 统一伤害入口
    /// </summary>
    /// <param name="amount">伤害量</param>
    /// <param name="reason">原因</param>
    /// <param name="stack">攻击上下文，非攻击伤害传 null</param>
    public async Task ApplyDamage(
    int amount,
    Variable.VariableReason reason,
    AtkStack stack = null,
    bool playHurtAnim = true,
    bool immediateDeath = true)
    {
        int before = Hp.Current;
        await Hp.Lose(amount, reason);
        int actual = before - Hp.Current;

        if (playHurtAnim && actual > 0)
            await Fighter.GetNode(this).Hurt(amount);

        if (Hp.Current <= 0)
        {
            bool isDamage = reason.HasFlag(Variable.VariableReason.Damaged);

            if (isDamage && stack != null)
            {
                await FireTiming(Timing.OnKilled, stack);
                await CardCmd.TimingOnCards(this, Timing.OnKilled, stack);
            }
            else
            {
                await FireTiming(Timing.OnDeath, reason);
                await CardCmd.TimingOnCards(this, Timing.OnDeath, reason);
            }

            if (immediateDeath)
            {
                var fighter = Fighter.GetNode(this);
                if (fighter != null)
                    await fighter.Die();
            }
        }
    }


    /// <summary>
    /// 直接消灭（扣光血）
    /// </summary>
    public async Task Kill(Variable.VariableReason reason, AtkStack stack = null, bool immediateDeath = true)
    {
        await ApplyDamage(Hp.Current, reason, stack, false, immediateDeath);
    }
    public bool BornWithAtk { get; private set; }
    /// <summary>
    /// 是否已死亡（Hp ≤ 0）
    /// </summary>
    public bool IsDie => Hp.Current <= 0;

    /// <summary>
    /// 是否能被当作目标
    /// </summary>
    /// <param name="cardModel">要把这张卡当作目标的卡牌</param>
    /// <returns></returns>
    protected virtual bool CanTargetedBy(CardModel cardModel) { return true; }

    /// <summary>
    /// 是否能被当作目标
    /// </summary>
    /// <param name="cardModel">要把这张卡当作目标的卡牌</param>
    /// <returns></returns>
    public bool CanbeTarget(CardModel cardModel)
    {
        bool hpbol = HpType.Current.ExtraFilter(cardModel);
        return hpbol && CanTargetedBy(cardModel);
    }

    /// <summary>
    /// 播放Fighter动画
    /// </summary>
    /// <param name="name"></param>
    /// <param name="returnToIdle"></param>
    /// <param name="track"></param>
    /// <returns></returns>
    public async Task PlayFighterAnimationTask(string name, bool returnToIdle = true, int track = 0)
    {
        var fighter = Fighter.GetNode(this);
        if (fighter != null)
            await fighter.PlayAnimation(name, returnToIdle, track);
    }

    protected virtual CardModel LoadCustomData(CardString cardString) { return this; }

    /// <summary>
    /// 卡牌标签(两栖，组队)
    /// </summary>
    public CardTag CardTag { get; private set; }
    /// <summary>
    /// 攻击力
    /// </summary>
    public CardIntVariable Atk { get; private set; }
    /// <summary>
    /// 攻击类型
    /// </summary>
    public CardVariable<AtkType> AtkType { get; private set; }
    /// <summary>
    /// 血量
    /// </summary>
    public CardIntVariable Hp { get; private set; }
    /// <summary>
    /// 最大血量
    /// </summary>
    public CardIntVariable MaxHp { get; private set; }
    /// <summary>
    /// 血量类型
    /// </summary>
    public CardVariable<HpType> HpType { get; private set; }
    /// <summary>
    /// 等级类型(提示特殊能力)
    /// </summary>
    public StarType StarType { get; private set; }
    /// <summary>
    /// 恢复血量
    /// </summary>
    /// <param name="amount">恢复数量</param>
    /// <param name="reason">原因</param>
    /// <returns></returns>
    public async Task Heal(int amount, Variable.VariableReason reason)
    {
        int cap = MaxHp.Current;
        if (Hp.Current + amount > cap)
            amount = cap - Hp.Current;
        if (amount <= 0) return;
        await Hp.Gain(amount, reason);
        await FireTiming(Timing.OnHealed, amount);
        await CardCmd.TimingOnCards(this, Timing.OnHealed, amount);
    }

    public virtual Task AnimationWhenPlayed(Road road) { return Task.CompletedTask; }
    /// <summary>
    /// 子弹的路径 (可以是.tres的动画 比如爆炸这种需要播放动画 , 也可以是纹理) 
    /// </summary>
    public virtual string AmmoPath { get; } = null;
    /// <summary>
    /// 子弹是否是瞬间到达目标的
    /// </summary>
    public virtual bool InstantAmmo { get; } = false;

    public async Task IntroPlayed()
    {
        await PlayFighterAnimationTask("intro");
    }

    public sealed override TargetType TargetType
    {
        get
        {
            if (CardTag.HasFlag(CardTag.Coop))
                return TargetType.CoopGrids;
            return TargetType.Grids;
        }
    }

    public sealed override async Task Play(ITarget target)
    {
        if (target.CanBeRoad(out Road road))
        {
            if (road.LastTargetKind == RoadTargetKind.Grid)
                await CardCmd.FighterGenerate(this, road, Location.Plant);
            else if (road.LastTargetKind == RoadTargetKind.CoopGrid)
                await CardCmd.FighterGenerate(this, road, Location.PlantFront);
        }
    }
}