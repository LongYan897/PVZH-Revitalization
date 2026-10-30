using Battle;
using Battle.Entity;
using Card.Cmd;
using Card.String;
using Godot;
using Hero;
using Logger;
using Play;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Target;
using Variable;

namespace Card;

/// <summary>
/// 单位类型的卡牌
/// </summary>
public class FighterCardModel : CardModel
{
    /// <summary>
    /// 僵尸是否隐藏在墓碑之中
    /// </summary>
    public bool Hiding => Camp == Camp.Zombie && (Fighter.GetNode(this) ?? null) != null && Fighter.GetNode(this).IsHiding;
    private readonly List<FighterCardModel> additiveCards = new();
    /// <summary>
    /// 隐藏单位的数值,并销毁单位
    /// </summary>
    /// <param name="Icon">融合的特殊图像</param>
    public static void Overlay(FighterCardModel fighter, Texture2D Icon)
    {
        Fighter.Overlay(fighter, Icon);
    }
    /// <summary>
    /// 修改单位的动画节点的属性 (比如缩小,移动...)
    /// </summary>
    /// <param name="func">修改节点的函数</param>
    /// <returns></returns>
    public async Task ShapeMe(Func<Node2D,Task> func)
    {
        if (IsPlaced)
            await func(Fighter.GetNode(this).AnimationNode);
    }
    /// <summary>
    /// 移动到一条线上的位置
    /// </summary>
    /// <param name="road">移到的线路</param>
    /// <param name="location">移动的位置</param>
    /// <returns></returns>
    public async Task MoveTo(Road road,Location location)
    {
        if (!IsPlaced) return;
        await Fighter.MoveTo(this,road,location);
        await FireTiming(Timing.WhenMoved, road, this);
        await CardCmd.TimingOnCards(this, Timing.WhenMoved, road, this);
    }

    protected sealed override void AfterClone()
    {
        base.AfterClone();
        Atk = new(this, "Attack", Atk.Current);
        Hp = new(this, "Health", Hp.Current);
        MaxHp = new(this, "HealthMax", MaxHp.Current);
        AtkType = new(this, "AttackType", AtkType.Current);
        HpType = new(this, "HealthType", HpType.Current);
    }
    /// <summary>
    /// 修改单位的动画节点,改变单位颜色
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public async Task ColorMe(Color color, float duration = 0.3f)
    {
        if (!IsPlaced) return;

        var fighter = Fighter.GetNode(this);
        var anim = fighter.AnimationNode;

        Tween tween = fighter.CreateTween();
        tween.TweenProperty(anim, "modulate", color, duration);
        await fighter.ToSignal(tween, Tween.SignalName.Finished);
    }
    /// <summary>
    /// 修改单位的动画节点,缩小该单位
    /// </summary>
    /// <param name="scale">缩小的倍率</param>
    /// <returns></returns>
    public async Task ScaleMe(Vector2 scale, float duration = 0.3f)
    {
        if (!IsPlaced) return;

        var fighter = Fighter.GetNode(this);
        var anim = fighter.AnimationNode;
        var pit = fighter.GetNode<SpineHandler>("%土坑");

        Vector2 offset = anim.Position - pit.Position;
        Vector2 targetPos = pit.Position + offset * scale;

        Tween tween = fighter.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(anim, "scale", scale, duration);
        tween.TweenProperty(anim, "position", targetPos, duration);
        await fighter.ToSignal(tween, Tween.SignalName.Finished);
    }
    /// <summary>
    /// 对一名敌人发射子弹(无伤害)
    /// </summary>
    /// <param name="texture">子弹的图标</param>
    /// <param name="from">从哪里开始</param>
    /// <param name="to">到哪里去</param>
    /// <param name="duration">飞行的时间</param>
    /// <returns></returns>
    public async Task SpawnAmmo(Texture2D texture, Vector2 from, Vector2 to, float duration)
    {
        if (!IsPlaced) return;
        await Fighter.GetNode(this).CreateAmmon(texture, from, to, duration);
    }
    /// <summary>
    /// 对一名敌人发射子弹并播放攻击动画(无伤害)
    /// </summary>
    /// <param name="texture">子弹的图标</param>
    /// <param name="from">从哪里开始</param>
    /// <param name="to">到哪里去</param>
    /// <param name="duration">飞行的时间</param>
    /// <returns></returns>
    public async Task Attack(IAttackable target)
    {
        if (!IsPlaced) return;
        if (target == null) return;
        var stack = new AtkStack(this, [target], Road, Atk.Current, false);
        stack.AddShot(target);
        await Fighter.GetNode(this).Battle(stack.Shots, InstantAmmo);
    }
    /// <summary>
    /// 把一个单位归类到自己的子类
    /// </summary>
    private void AddGroup(FighterCardModel fighter)
    {
        Fighter.GetNode(this).GroupWith(fighter);
    }
    /// <summary>
    /// 检测该单位是否在场上
    /// </summary>
    public bool IsPlaced => Fighter.GetNode(this) != null;
    /// <summary>
    /// 获取这个单位的坐标 如果没有返回(0,0)
    /// </summary>
    /// <returns></returns>
    public Vector2 Locating() => IsPlaced ? GlobalPosition : new();
    protected Vector2 GlobalPosition => Fighter.GetNode(this).GlobalPosition;
    /// <summary>
    /// 把这张卡牌弹回手牌
    /// </summary>
    /// <returns></returns>
    public async Task Bounced()
    {
        if (IsPlaced)
        {
            await Fighter.GetNode(this).ReturnCard();
            await Reset();
        }
    }
    protected sealed override CardModel LoadData(CardString cardString)
    {
        Atk = new(this, "Attack", cardString.Attack);
        BornWithAtk = Atk.Current > 0;
        AtkType = new(this, "AttackType", cardString.AtkType ?? new NormalAtkType());
        Hp = new(this, "Health", cardString.Health);
        MaxHp = new(this, "HealthMax", cardString.Health);
        HpType = new(this, "HealthType", cardString.HpType ?? new NormalHpType());
        CardTag = cardString.CardTag;
        StarType = cardString.StarType;
        RegistBuff(new HpBuff());
        RegistBuff(AtkType.Current.Buff);
        RegistBuff(HpType.Current.Buff);
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
    public IAttackable Attacking() => Fighter.GetNode(this);
    /// <summary>
    /// 所处的道路
    /// </summary>
    public Road Road { get; internal set; }
    /// <summary>
    /// 所在的位置
    /// </summary>
    public Location Location { get; internal set; }
    /// <summary>
    /// 创建一个攻击堆栈(攻击者是这个单位)
    /// </summary>
    /// <returns></returns>
    private AtkStack CreateOwnedStack()
    => new(this, [], Road, Atk.Current,false);
    /// <summary>
    /// 额外攻击一次
    /// </summary>
    /// <returns></returns>
    public async Task Battle(bool kill = true) => await Battle(CreateOwnedStack(), kill);

    /// <summary>
    /// 获取该线上可行的单位
    /// </summary>
    /// <param name="atkStack"></param>
    /// <returns></returns>
    public async Task GetTarget(AtkStack atkStack)
    {
        if (atkStack.Road == null) return;

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
        target ??= PlayerManager.GetPlayer(Camp switch { Camp.Zombie => Camp.Plant, Camp.Plant => Camp.Zombie, _ => 0 })?.Status?.AtkMe();
        if (target != null)
            atkStack.Targeting(target);

        await FireTiming(Timing.ModifyTarget, atkStack);
        await CardCmd.TimingOnCards(this, Timing.ModifyTarget, atkStack);

        if (atkStack.Pierce && !atkStack.TargetsRedirected)
            ExpandPierceTargets(atkStack);
    }

    private void ExpandPierceTargets(AtkStack atkStack)
    {
        var expanded = new List<IAttackable>();
        var seen = new HashSet<IAttackable>();

        foreach (var t in atkStack.Targets)
        {
            if (t is Fighter f && f.Road != null)
            {
                foreach (var enemy in f.Road.GetFighters())
                {
                    if (enemy?.Model == null) continue;
                    if (enemy.Model.IsDie) continue;
                    if (enemy.Model.Camp == Camp) continue;
                    if (seen.Add(enemy))
                        expanded.Add(enemy);
                }
            }
            else
            {
                if (seen.Add(t))
                    expanded.Add(t);
            }
        }

        if (expanded.Count > 0)
            atkStack.Targeting([.. expanded]);
    }
    /// <summary>
    /// 获取一条线上的可选择目标
    /// </summary>
    /// <param name="road">线</param>
    /// <returns></returns>
    public IEnumerable<IAttackable> GetTarget(Road road)
    {
        if (road == null) return Array.Empty<IAttackable>();

        var fighters = road.GetFighters();
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
        target ??= PlayerManager.GetPlayer(Camp switch { Camp.Zombie => Camp.Plant, Camp.Plant => Camp.Zombie, _ => 0 })?.Status?.AtkMe();

        return target != null ? new[] { target } : Array.Empty<IAttackable>();
    }
    /// <summary>
    /// 开始战斗
    /// </summary>
    public async Task Battle(AtkStack atkStack, bool kill = false)
    {
        if (Atk.Current <= 0) return;
        Variable<bool> stop = new("Stop", false);
        await FireTiming(Timing.TryStopAttack, stop, this);
        await CardCmd.TimingOnCards(this, Timing.TryStopAttack, stop, this);
        if (stop.Current)
        {
            await FireTiming(Timing.AfterAttack, atkStack);
            await CardCmd.TimingOnCards(this, Timing.AfterAttack, atkStack);
            return;
        }
        await FireTiming(Timing.BeforeAttack,atkStack);
        await CardCmd.TimingOnCards(this,Timing.BeforeAttack, atkStack);
        await GetTarget(atkStack);
        await FireTiming(Timing.ModifyDamage, atkStack);
        await CardCmd.TimingOnCards(this, Timing.ModifyDamage, atkStack);

        if (Fighter.GetNode(this) is { } self)
        {
            foreach (var t in atkStack.Targets)
            {
                if (t == null) continue;
                if (!atkStack.Shots.ContainsKey(t))
                    atkStack.AddShot(t);
            }
            await self.Battle(atkStack.Shots, InstantAmmo);
        }

        await FireTiming(Timing.OnAttack, atkStack);
        await CardCmd.TimingOnCards(this, Timing.OnAttack, atkStack);

        var finalTgs = atkStack.Targets.ToList();
        var tasks = new List<Task<IAttackable>>();
        foreach (var itg in finalTgs)
            tasks.Add(HitOne(itg, atkStack));
        var hitTargets = (await Task.WhenAll(tasks))
            .Where(t => t != null)
            .ToList();

        await FireTiming(Timing.AfterAttack, atkStack);
        await CardCmd.TimingOnCards(this, Timing.AfterAttack, atkStack);
        if (kill)
        {
            foreach (var itg in hitTargets)
            {
                if (itg is Fighter fighter)
                {
                    if (fighter.Model.IsDie)
                        await fighter.Model.Die(VariableReason.Fighter | VariableReason.Damaged, atkStack);
                }
            }
        }
    }
    private async Task<IAttackable> HitOne(IAttackable itg, AtkStack parent)
    {
        var stack = parent.ForTarget(itg);

        if (itg is Fighter fighter)
        {
            if (fighter.IsHiding) return null;

            await FireTiming(Timing.WhenAttacked, stack);
            await CardCmd.TimingOnCards(fighter.Model, Timing.WhenAttacked, stack);

            await fighter.Model.ApplyDamage(stack.FinalDamage, Variable.VariableReason.Fighter | VariableReason.Damaged, stack, true, false);

            await fighter.Model.FireTiming(Timing.AfterAttacked, stack);
            await CardCmd.TimingOnCards(fighter.Model, Timing.AfterAttacked, stack);
            await FireTiming(Timing.ApplyTarget, stack);
            await CardCmd.TimingOnCards(this, Timing.ApplyTarget, stack);
            return itg;
        }
        else if (itg is HeroInstance hero)
        {
            await CardCmd.TimingOnCards(null, Timing.WhenAttacked, stack);

            await hero.Hero.ApplyDamage(stack.FinalDamage, Variable.VariableReason.Fighter | VariableReason.Damaged, this, stack, !stack.IgnoreShield);

            await CardCmd.TimingOnCards(null, Timing.AfterAttacked, stack);

            await FireTiming(Timing.ApplyTarget, stack);
            await CardCmd.TimingOnCards(this, Timing.ApplyTarget, stack);
            return itg;
        }

        return null;
    }

    /// <summary>
    /// 是否正在处理死亡
    /// </summary>
    public bool IsDying { get; private set; }

    /// <summary>
    /// 触发死亡时点并播放死亡动画。
    /// </summary>
    /// <param name="reason">死亡原因</param>
    /// <param name="stack">攻击上下文，非攻击致死可传 null</param>
    public async Task Die(Variable.VariableReason reason, AtkStack stack = null)
    {
        if (IsDying) return;
        IsDying = true;

        bool isDamage = reason.HasFlag(Variable.VariableReason.Fighter) || reason.HasFlag(Variable.VariableReason.Damaged);

        if (isDamage && stack?.Attacker != null)
        {
            await FireTiming(Timing.OnKilled, stack);
            await CardCmd.TimingOnCards(this, Timing.OnKilled, stack);
        }
        else
        {
            stack ??= new AtkStack(null, [Attacking()], Road, 0,false);
            await FireTiming(Timing.OnDeath, stack);
            await CardCmd.TimingOnCards(this, Timing.OnDeath, stack);
        }

        var fighter = Fighter.GetNode(this);
        if (fighter != null)
            await fighter.Die();
    }

    /// <summary>
    /// 伤害入口
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
        stack ??= new(null, [Targeted()], null, amount,false);
        int before = Hp.Current;
        await FireTiming(Timing.ModifyCardDamage, stack);
        await CardCmd.TimingOnCards(this, Timing.ModifyCardDamage, stack);
        await Hp.Lose(stack.FinalDamage, reason);
        await FireTiming(Timing.OnDamaged, stack);
        await CardCmd.TimingOnCards(this, Timing.OnDamaged, stack);
        int actual = before - Hp.Current;

        if (playHurtAnim && actual > 0)
            await Fighter.GetNode(this).Hurt(stack.FinalDamage);

        if (IsDie && immediateDeath)
        {
            await Die(VariableReason.Damaged | reason,stack);
        }
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
    public bool CanbeTarget(CardModel cardModel, bool fromBuff)
    {
        bool hpbol = HpType.Current.ExtraFilter(cardModel);
        bool fusOrEvo = true;
        if (cardModel is FighterCardModel fighter && (IsFusion || fighter.IsEvolution))
        {
            fusOrEvo = CanFusion(fighter, out var _) || fighter.CanEvolution(this);
            fusOrEvo &= fighter.Camp == Camp;
        }
        else if (cardModel is FighterCardModel fighter2)
        {
            fusOrEvo = false || fromBuff;
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
        Variable<bool> stop = new("Stop", false);
        await CardCmd.TimingOnCards(null, Timing.TryStopGainMaxHp, stop, this);
        if (stop.Current) return;
        Variable<int> maxHp = new("MaxHp", amount);
        Variable<int> hp = new("Hp", amount);
        await CardCmd.TimingOnCards(null, Timing.ModifyGainMaxHp, maxHp, this);
        await MaxHp.Gain(maxHp.Current, reason);
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
        await CardCmd.TimingOnCards(null, Timing.ModifyHeal, hp, this);
        await Hp.Gain(hp.Current, reason);
        await FireTiming(Timing.OnHealed, hp.Current, this);
        await CardCmd.TimingOnCards(this, Timing.OnHealed, hp.Current, this);
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
    /// <summary>
    /// 子弹是否带方向(会指向目标,需要子弹图标/动画的正方向向下)
    /// </summary>
    public virtual bool ArrowAmmo {  get; } = false;

    public async Task IntroPlayed()
    {
        await PlayFighterAnimationTask("intro");
    }
    public sealed override Task Changed<T>(string key, T valueAfter, T valueBefore,VariableReason reason)
    {
        if (reason.HasFlag(VariableReason.Reset)) return Task.CompletedTask;
        if (IsPlaced)
        {
            if (key == "Health" || key == "MaxHealth")
            {
                if (valueAfter is int t && valueBefore is int tb)
                {
                    if (t - tb > 0)
                    {
                        Fighter.GetNode(this).Boost();
                    }
                }
            }
            if (key == "Attack")
            {
                if (valueAfter is int t && valueBefore is int tb)
                {
                    if (t - tb > 0)
                    {
                        Fighter.GetNode(this).BoostAtk();
                    }
                }
            }
        }
        return Task.CompletedTask;
    }
    public sealed override TargetType TargetType
    {
        get
        {
            if (CardCmd.IsAnyOneCanEvolved(this))
            {
                if (CardTag.HasFlag(CardTag.Coop))
                    return TargetType.CoopGrids | TargetType.Fighters;
                return TargetType.Grids | TargetType.Fighters;
            }
            else
            {
                if (CardTag.HasFlag(CardTag.Coop))
                    return TargetType.CoopGrids;
                return TargetType.Grids;
            }
        }
    }

    public sealed override async Task Play(ITarget target)
    {
        if (target.CanBeRoad(out Road road))
        {
            if (Camp == Camp.Zombie)
            {
                await CardCmd.FighterGenerate(this, road, Location.Zombie);
                return;
            }
            if (road.LastTargetKind == RoadTargetKind.Grid)
                await CardCmd.FighterGenerate(this, road, Location.Plant);
            else if (road.LastTargetKind == RoadTargetKind.CoopGrid)
                await CardCmd.FighterGenerate(this, road, Location.PlantFront);
        }
        else if (target.CanBeFighter(out FighterCardModel fighter))
        {
            if (fighter.CanFusion(this, out var icon))
            {
                await CardCmd.Fuse(this, fighter);
                Overlay(fighter, icon);
            }
            if (CanEvolution(fighter))
            {
                Overlay(fighter, null);
            }
            await CardCmd.FighterGenerate(this, fighter.Road, fighter.Location);
            AddGroup(fighter);
            if (CanEvolution(fighter))
            {
                await CardCmd.Evolve(this, fighter);
            }
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
    public virtual bool CanFusion(FighterCardModel cardModel, out Texture2D fuseIcon) { fuseIcon = null; return false; }
    /// <summary>
    /// 是否能把其他卡牌当作进化的对象
    /// </summary>
    public virtual bool CanEvolution(FighterCardModel cardModel) { return false; }
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
    /// <summary>
    /// 触发死亡时点。
    /// 调用前确保 Hp ≤ 0；方法内部会校验。
    /// </summary>
    /// <param name="reason">死亡原因</param>
    /// <param name="stack">攻击上下文，非攻击致死可传 null</param>
    public async Task BeginDie(Variable.VariableReason reason, AtkStack stack = null)
    {
        if (!IsDie) return;
        if (IsDying) return;
        IsDying = true;

        bool isDamage = reason.HasFlag(Variable.VariableReason.Fighter) || reason.HasFlag(Variable.VariableReason.Damaged);

        if (isDamage && stack?.Attacker != null)
        {
            await FireTiming(Timing.OnKilled, stack);
            await CardCmd.TimingOnCards(this, Timing.OnKilled, stack);
        }
        else
        {
            stack ??= new AtkStack(null, [Attacking()], Road, 0,false);
            await FireTiming(Timing.OnDeath, stack);
            await CardCmd.TimingOnCards(this, Timing.OnDeath, stack);
        }
    }

    /// <summary>
    /// 触发死亡动画。
    /// 调用前确保 Hp ≤ 0；方法内部会校验。
    /// </summary>
    /// <param name="reason">死亡原因</param>
    /// <param name="stack">攻击上下文，非攻击致死可传 null</param>
    public async Task EndDie()
    {
        if (!IsDie) return;
        if (IsPlaced)
        {
            await Fighter.GetNode(this).Die();
        }
    }

    protected override async Task ResetData()
    {
        await Hp.Reset();
        await Atk.Reset();
    }
}