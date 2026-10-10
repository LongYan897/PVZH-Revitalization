using Battle;
using Battle.Entity;
using Card.Cmd;
using Card.String;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Target;
using Variable;
using Variable.Special;
using static Godot.HttpRequest;
using static Godot.OpenXRCompositionLayer;

namespace Card;

/// <summary>
/// 单位类型的卡牌
/// </summary>
public class FighterCardModel : CardModel
{
    private readonly List<FighterCardModel> additiveCards = new();
    /// <summary>
    /// 隐藏单位的数值,并销毁单位
    /// </summary>
    /// <param name="Icon">融合的特殊图像</param>
    public static void Overlay(FighterCardModel fighter,Texture2D Icon)
    {
        Fighter.Overlay(fighter,Icon);
    }
    /// <summary>
    /// 把一个单位归类到自己的子类
    /// </summary>
    public void AddGroup(FighterCardModel fighter)
    {
        Fighter.GetNode(this).GroupWith(fighter);
    }
    protected override CardModel LoadData(CardString cardString)
    {
        Atk = new(this, "Attack", cardString.Attack);
        BornWithAtk = Atk.Current > 0;
        AtkType = new(this, "AttackType", cardString.AtkType ?? new NormalAtkType());
        Hp = new(this, "Health", cardString.Health);
        MaxHp = new(this, "HealthMax", cardString.Health);
        HpType = new(this, "HealthType", cardString.HpType ?? new NormalHpType());
        CardTag = cardString.CardTag;
        StarType = cardString.StarType;
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
        Variable<bool> stop = new("Stop", false);
        await FireTiming(Timing.TryStopAttack, stop,this);
        await CardCmd.TimingOnCards(this,Timing.TryStopAttack, stop, this);
        if (stop.Current)
        {
            await FireTiming(Timing.AfterAttack, atkStack);
            await CardCmd.TimingOnCards(this, Timing.AfterAttack, atkStack);
            if (atkStack.ExtraAttacks > 0)
            {
                atkStack.ExtraAttacks -= 1;
                await Battle(atkStack);
                return;
            }
            else
                return;
        }
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
            if (itg is Fighter fighter)
            {
                atkStack.FinalDamage = atkStack.BaseDamage;

                await FireTiming(Timing.WhenAttacked, atkStack);
                await CardCmd.TimingOnCards(fighter.Model, Timing.WhenAttacked, atkStack);

                await fighter.Model.ApplyDamage(atkStack.FinalDamage, Variable.VariableReason.Fighter, atkStack, true, false);

                await fighter.Model.FireTiming(Timing.AfterAttacked, atkStack);
                await CardCmd.TimingOnCards(fighter.Model, Timing.AfterAttacked, atkStack);
                await FireTiming(Timing.ApplyTarget, atkStack);
                await CardCmd.TimingOnCards(this, Timing.ApplyTarget, atkStack);
            }
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
        stack ??= new(null, [Targeted()], null , amount);
        int before = Hp.Current;
        await FireTiming(Timing.ModifyCardDamage, stack);
        await CardCmd.TimingOnCards(this, Timing.ModifyCardDamage, stack);
        await Hp.Lose(stack.FinalDamage, reason);
        await FireTiming(Timing.OnDamaged, stack);
        await CardCmd.TimingOnCards(this,Timing.OnDamaged, stack);
        int actual = before - Hp.Current;

        if (playHurtAnim && actual > 0)
            await Fighter.GetNode(this).Hurt(stack.FinalDamage);

        if (Hp.Current <= 0)
        {
            bool isDamage = reason.HasFlag(Variable.VariableReason.Damaged);

            if (isDamage && stack.Attacker != null)
            {
                await FireTiming(Timing.OnKilled, stack);
                await CardCmd.TimingOnCards(this, Timing.OnKilled, stack);
            }
            else
            {
                await FireTiming(Timing.OnDeath, stack);
                await CardCmd.TimingOnCards(this, Timing.OnDeath, stack);
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
    /// <summary>
    /// 是否有初始血量
    /// </summary>
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
        bool fusOrEvo = true;
        if (cardModel is FighterCardModel fighter && (IsFusion || fighter.IsEvolution))
        {
            fusOrEvo = CanFusion(fighter,out var _) || fighter.CanEvolution(this);
            fusOrEvo &= fighter.Camp == Camp;
        }
        return fusOrEvo && hpbol && CanTargetedBy(cardModel);
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

    private CardTag _tag;
    /// <summary>
    /// 卡牌标签(两栖，组队)
    /// </summary>
    public CardTag CardTag
    {
        get => _tag;
        set
        {
            Fresh();
            _tag = value;
        }
    }
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
    /// 获得最大血上限
    /// </summary>
    /// <param name="amount">获得血上限的数量</param>
    /// <param name="reason">原因</param>
    /// <returns></returns>
    public async Task GainHp(int amount, Variable.VariableReason reason)
    {
        Variable<bool> stop = new("Stop",false);
        await CardCmd.TimingOnCards(null, Timing.TryStopGainMaxHp, stop,this);
        if (stop.Current) return;
        Variable<int> maxHp = new("MaxHp",amount);
        Variable<int> hp = new("Hp", amount);
        await CardCmd.TimingOnCards(null,Timing.ModifyGainMaxHp,maxHp,this);
        await MaxHp.Gain(maxHp.Current,reason);
        Variable<bool> stop2 = new("Stop2", false);
        await CardCmd.TimingOnCards(null, Timing.TryStopHeal, stop2, this);
        if (stop2.Current) return;
        await CardCmd.TimingOnCards(null, Timing.ModifyHeal, hp, this);
        await Hp.Gain(hp.Current, reason);
    }
    /// <summary>
    /// 恢复血量
    /// </summary>
    /// <param name="amount">恢复数量</param>
    /// <param name="reason">原因</param>
    /// <returns></returns>
    public async Task Heal(int amount, Variable.VariableReason reason)
    {
        Variable<bool> stop = new("Stop", false);
        await CardCmd.TimingOnCards(null, Timing.TryStopHeal, stop, this);
        if (stop.Current) return;
        int cap = MaxHp.Current;
        if (Hp.Current + amount > cap)
            amount = cap - Hp.Current;
        if (amount <= 0) return;
        Variable<int> hp = new("Hp", amount);
        await CardCmd.TimingOnCards(null, Timing.ModifyHeal, hp,this);
        await Hp.Gain(hp.Current, reason);
        await FireTiming(Timing.OnHealed, hp.Current,this);
        await CardCmd.TimingOnCards(this, Timing.OnHealed, hp.Current,this);
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
                return TargetType.CoopGrids | TargetType.Fighters;
            return TargetType.Grids | TargetType.Fighters;
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
        else if (target.CanBeFighter(out FighterCardModel fighter))
        {
            if (fighter.CanFusion(this,out var icon))
            {
                await CardCmd.Fuse(this, fighter);
                Overlay(fighter, icon);
            }
            if (CanEvolution(fighter))
            {
                await CardCmd.Evolve(this, fighter);
                Overlay(fighter, null);
            }
            await CardCmd.FighterGenerate(this, fighter.Road, fighter.Location);
            AddGroup(fighter);
            return;
        }
    }
    /// <summary>
    /// 融合逻辑。
    /// </summary>
    public virtual Task Fuse(FighterCardModel fighter) => Task.CompletedTask;

    /// <summary>
    /// 进化逻辑。
    /// </summary>
    public virtual Task Evolve(FighterCardModel fighter) => Task.CompletedTask;

    /// <summary>
    /// 是否能作为其他卡牌的融合对象
    /// </summary>
    protected virtual bool CanFusion(FighterCardModel cardModel,out Texture2D fuseIcon) { fuseIcon = null; return false; }
    /// <summary>
    /// 是否能把其他卡牌当作进化的对象
    /// </summary>
    protected virtual bool CanEvolution(FighterCardModel cardModel) { return false; }
    /// <summary>
    /// 是否是含融合效果的卡牌
    /// </summary>
    public virtual bool IsFusion { get; } = false;
    /// <summary>
    /// 是否是含进化效果的卡牌
    /// </summary>
    public virtual bool IsEvolution { get; set; } = false;
    private IAttackable Targeted()
    {
        return Fighter.GetNode(this);
    }
}