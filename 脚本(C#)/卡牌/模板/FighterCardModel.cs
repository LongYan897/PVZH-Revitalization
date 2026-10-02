using Battle;
using Battle.Entity;
using Card.Cmd;
using Card.String;
using System.Linq;
using System.Threading.Tasks;
using Target;

namespace Card;

/// <summary>
/// 单位类型的卡牌
/// </summary>
public class FighterCardModel : CardModel
{
    protected override CardModel LoadData(CardString cardString)
    {
        Atk = new(this, "Attack", cardString.Attack);
        AtkType = new(this, "AttackType", cardString.AtkType);
        Hp = new(this, "Health", cardString.Health);
        HpType = new(this, "HealthType", cardString.HpType);
        CardTag = cardString.CardTag;
        return LoadCustomData(cardString);
    }

    /// <summary>
    /// 开始战斗
    /// </summary>
    public async Task Battle(AtkStack atkStack)
    {
        var fighters = atkStack.Road.GetFighters();
        var target = (IAttackable)fighters.First(
            f =>
            {
                if (Camp == Camp.Zombie)
                {
                    if (fighters.Count > 1)
                        return f.Plant1 && f.Model.Camp == Camp.Plant;
                    else
                        return f.Model.Camp == Camp.Plant;
                }
                else
                    return f.Model.Camp == Camp.Zombie;
            }
            );
        atkStack.Targeting(target);

        await FireTiming(Timing.ModifyTarget, atkStack);
        await CardCmd.TimingOnCards(this, Timing.ModifyTarget, atkStack);
        await FireTiming(Timing.ModifyDamage, atkStack);
        await CardCmd.TimingOnCards(this, Timing.ModifyDamage, atkStack);

        await Fighter.GetNode(this).Battle(atkStack.Targets,InstantAmmo);

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

            await fighter.Model.ApplyDamage(atkStack.FinalDamage, Variable.VariableReason.Fighter, atkStack);

            await fighter.Model.FireTiming(Timing.AfterAttacked, atkStack);
            await CardCmd.TimingOnCards(fighter.Model, Timing.AfterAttacked, atkStack);
            await FireTiming(Timing.ApplyTarget, atkStack);
            await CardCmd.TimingOnCards(this, Timing.ApplyTarget, atkStack);
        }

        await FireTiming(Timing.AfterAttack, atkStack);
        await CardCmd.TimingOnCards(this, Timing.AfterAttack, atkStack);

        foreach (var itg in finalTgs)
        {
            if (itg is Fighter fighter && fighter.Model.IsDie)
                await fighter.Die();
        }

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
    public async Task ApplyDamage(int amount, Variable.VariableReason reason, AtkStack stack = null)
    {
        await Hp.Lose(amount, reason);

        if (Hp.Current <= 0)
        {
            if (stack != null)
            {
                await FireTiming(Timing.OnKilled, stack);
                await CardCmd.TimingOnCards(this, Timing.OnKilled, stack);
            }
            else
            {
                await FireTiming(Timing.OnDeath, reason);
                await CardCmd.TimingOnCards(this, Timing.OnDeath, reason);
            }
        }
    }

    /// <summary>
    /// 直接消灭（扣光血）
    /// </summary>
    public async Task Kill(Variable.VariableReason reason, AtkStack stack = null)
    {
        await ApplyDamage(Hp.Current, reason, stack);
    }

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
    protected async Task PlayFighterAnimationTask(string name, bool returnToIdle = true, int track = 0)
    {
        var fighter = Fighter.GetNode(this);
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
    /// 血量类型
    /// </summary>
    public CardVariable<HpType> HpType { get; private set; }
    /// <summary>
    /// 等级类型(提示特殊能力)
    /// </summary>
    public StarType StarType { get; private set; }

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

    public override async Task Changed<T>(string key, T valueAfter, T valueBefore)
    {
        if (key == "Health")
        {
            if (valueAfter is int i && valueBefore is int i2 && i < i2)
                await Fighter.GetNode(this).Hurt(i2 - i);
        }
        await base.Changed(key, valueAfter, valueBefore);
    }

    public override TargetType TargetType
    {
        get
        {
            if (CardTag.HasFlag(CardTag.Coop))
                return TargetType.CoopGrids;
            return TargetType.Grids;
        }
    }

    public override async Task Play(ITarget target)
    {
        if (target is NodeRoad road)
        {
            if (road.LastTargetKind == NodeRoad.RoadTargetKind.Grid)
                await CardCmd.FighterGenerate(this, road.Model, Location.Plant);
            else if (road.LastTargetKind == NodeRoad.RoadTargetKind.CoopGrid)
                await CardCmd.FighterGenerate(this, road.Model, Location.PlantFront);
        }
    }
}