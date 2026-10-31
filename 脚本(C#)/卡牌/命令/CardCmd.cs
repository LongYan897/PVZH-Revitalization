using Battle;
using Battle.Entity;
using Logger;
using Phrases;
using Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Target;
using Variable;

namespace Card.Cmd;

/// <summary>
/// 用于卡牌的命令
/// </summary>
public static class CardCmd
{
    private static HashSet<CardModel> CardInstances = new();

    private static int _stopCount;
    /// <summary>
    /// 是否禁止出牌
    /// </summary>
    public static bool StopCardPlay => _stopCount > 0;
    private static object stopLock = new();
    private static object stepLock = new();
    public static void PushStop()
    {
        lock(stopLock)
        {
            _stopCount++;
            PhraseManager.PushProcessing();
            if (_stopCount == 1)
            {
                JustFreshAllCards();
            } 
        }
    }

    /// <summary>
    /// 退出一次禁止出牌状态
    /// </summary>
    public static void PopStop()
    {
        lock (stepLock)
        {
            _stopCount = Math.Max(0, _stopCount - 1);
            PhraseManager.PopProcessing();
            if (_stopCount == 0)
            {
                JustFreshAllCards();
            }
        }
    }

    public static void Clear()
    {
        Buffs.Clear();
        CardInstances.Clear();
        _cardHooks.Clear();
    }
    /// <summary>
    /// 向Cmd添加卡牌
    /// </summary>
    /// <param name="cardModel"></param>
    public static void Add(CardModel cardModel)
    {
        if (cardModel == null) return;
        if (!CardInstances.Add(cardModel)) return;

        foreach (var (filter, action, applied) in _cardHooks.ToList())
        {
            if (applied.Contains(cardModel) || !filter(cardModel)) continue;
            applied.Add(cardModel);
            var func = (async () =>
            {
                try { await action(cardModel); }
                catch (Exception ex) { Log.Error($"[ForCards] {cardModel.Title}: {ex}"); }
            });
            func();
        }
    }


    /// <summary>
    /// 从Cmd删除卡牌
    /// </summary>
    /// <param name="cardModel"></param>
    public static void Remove(CardModel cardModel)
    {
        CardInstances.Remove(cardModel);
    }

    /// <summary>
    /// 修改卡牌后执行所有卡牌修改卡牌的逻辑
    /// </summary>
    /// <typeparam name="T">修改卡牌变量的类型</typeparam>
    /// <param name="cardModel">被修改的卡牌</param>
    /// <param name="before">变量修改前的值</param>
    /// <param name="after">变量修改后的值</param>
    /// <returns></returns>
    public static async Task ChangedCard<T>(CardModel cardModel, string key, T before, T after, VariableReason variableReason)
    {
        cardModel.Fresh();
        await cardModel.Changed(key, after, before,variableReason);
        await TimingOnCards(null, Timing.OnVariableChanged, key,  after, before, variableReason);
    }

    /// <summary>
    /// 获取相邻线
    /// </summary>
    /// <param name="road">所处的线</param>
    /// <returns></returns>
    public static IEnumerable<Road> GetBehindRoad(Road road)
    {
        var Index = road.Index;
        if (Index == 1)
            return [PhraseManager.GetRoad(Index + 1)];
        else if (Index == 5)
            return [PhraseManager.GetRoad(Index - 1)];
        else if (Index > 1 && Index < 5)
            return [PhraseManager.GetRoad(Index + 1),PhraseManager.GetRoad(Index-1)];
        else
            return null;
    }

    /// <summary>
    /// 打出卡牌后执行所有卡牌打出卡牌的逻辑
    /// </summary>
    /// <param name="cardModel">打出的卡牌</param>
    /// <param name="target">打出卡牌的对象</param>
    /// <returns></returns>
    public static async Task PlayedCard(CardModel cardModel, ITarget target)
    {
        PushStop();
        try
        {
            await cardModel.OnPlayWrap(target);
            await TimingOnCards(null, Timing.AfterCardPlayed, cardModel, target);
        }
        finally
        {
            PopStop();
            if (!CardInstances.Any(c => PhraseManager.CurrentTurnPlayer != null && PhraseManager.CurrentTurnPlayer.Status.Hero.Camp == c.Camp && c.CanPlay()))
            {
                await PhraseManager.HintCost();
            }
        }
    }

    /// <summary>
    /// 让一位玩家抽牌
    /// </summary>
    /// <param name="player">要抽牌的玩家</param>
    /// <param name="cardModel">索引的卡牌</param>
    /// <param name="amount">抽牌的数量</param>
    /// <returns></returns>
    public static async Task Draw(Player player, CardModel cardModel,int amount = 1)
    {
        player ??= PlayerManager.GetPlayer(cardModel.Camp);
        await player.Status.PlayCardAndDraw(amount);
    }

    /// <summary>
    /// 创建指定类型的卡牌并加入玩家手牌。
    /// </summary>
    /// <typeparam name="T">要创建的卡牌类型</typeparam>
    /// <param name="player">目标玩家</param>
    /// <param name="amount">创建数量</param>
    /// <returns>创建并加入手牌的卡牌</returns>
    public static async Task<List<CardModel>> Make<T>(Player player = null, int amount = 1) where T : CardModel
    {
        var cards = new List<CardModel>();
        if (amount <= 0) return cards;

        for (int i = 0; i < amount; i++)
            cards.Add(CardModel.Load<T>());

        player ??= PlayerManager.GetPlayer(cards[0].Camp);
        await player.Status.PlayCardAndReturn(cards.ToArray());
        return cards;
    }

    private static readonly Random _makeRng = new();

    /// <summary>
    /// 创建指定类型的卡牌并加入玩家手牌。
    /// </summary>
    /// <param name="player">目标玩家</param>
    /// <param name="selectors">筛选器和数量</param>
    /// <returns>创建并加入手牌的卡牌</returns>
    public static async Task<List<CardModel>> MakeRandom(Player player, params (Func<CardModel, bool> Filter, int Amount)[] selectors)
    {
        var cards = new List<CardModel>();
        if (selectors == null || selectors.Length == 0) return cards;

        foreach (var (filter, amount) in selectors)
        {
            if (filter == null || amount <= 0) continue;

            var matched = CardModel.AllCards.Where(filter).ToList();
            if (matched.Count == 0) continue;

            for (int i = 0; i < amount; i++)
            {
                var template = matched[_makeRng.Next(matched.Count)];
                cards.Add(template.Clone());
            }
        }

        if (cards.Count == 0) return cards;

        player ??= PlayerManager.GetPlayer(cards[0].Camp);
        await player.Status.PlayCardAndReturn(cards.ToArray());
        return cards;
    }

    /// <summary>
    /// 将指定卡牌加入玩家手牌。
    /// </summary>
    /// <param name="player">目标玩家</param>
    /// <param name="cards">要加入手牌的卡牌</param>
    public static async Task Make(Player player = null, params CardModel[] cards)
    {
        if (cards == null || cards.Length == 0) return;

        player ??= PlayerManager.GetPlayer(cards[0].Camp);
        await player.Status.PlayCardAndReturn(cards);
    }

    private static void JustFreshAllCards()
    {
        foreach (var card in CardInstances.ToList())
        {
            var node = NodeCard.GetNode(card);
            node?.JustFresh();
        }
    }
    /// <summary>
    /// 改变玩家的阳光/脑子数
    /// </summary>
    /// <param name="player">要改变的玩家(null时默认为卡牌的操控玩家)</param>
    /// <param name="amount">要改变的数值,负数为失去</param>
    /// <param name="cardModel">执行该效果的卡牌</param>
    /// <returns></returns>
    public static async Task ChangeCost(Player player, int amount, CardModel cardModel)
    {
        player ??= PlayerManager.GetPlayer(cardModel.Camp);

        if (amount > 0)
            await player.Status.Hero.Cost.Gain(amount, VariableReason.Hero, cardModel);
        else if (amount < 0)
            await player.Status.Hero.Cost.Lose(-amount, VariableReason.Hero, cardModel);
    }

    /// <summary>
    /// 触发卡牌的时点
    /// </summary>
    /// <param name="cardModel">忽略的卡牌（一般是发起者）</param>
    /// <param name="timing">时点</param>
    /// <param name="paramters">参数</param>
    /// <returns></returns>
    public static async Task TimingOnCards(CardModel cardModel, Timing timing, params object[] paramters)
    {
        foreach (var card in CardInstances.Where(c=> PlayerManager.GetPlayer(c.Camp).Status.HandCards.Contains(c)).ToList())
        {
            if (card != cardModel)
                await card.FireTiming(timing, paramters);
        }

        var fighters = TargetRegistry.All.OfType<Fighter>().ToList();
        fighters.Sort((a, b) =>
        {
            int byRoad = a.Road.Index.CompareTo(b.Road.Index);
            if (byRoad != 0) return byRoad;

            return LocationPriority(a.Model.Location).CompareTo(LocationPriority(b.Model.Location));
        });

        foreach (Fighter fighter in fighters)
        {
            if (fighter.Model != cardModel && !CardInstances.Contains(fighter.Model))
                await fighter.Model.FireTiming(timing, paramters);
        }

        var fired = Buffs.Where(b => b.Timings.Contains(timing));
        foreach (Buff buff in fired.OrderByDescending(b => b.Priority))
        {
            if (buff.Timings.Contains(timing))
            {
                await buff.OnTiming(timing,null,paramters);
            }
        }
    }

    private static int LocationPriority(Location location) => location switch
    {
        Location.Zombie => 0,
        Location.PlantFront => 1,
        Location.Plant => 2,
        _ => 3
    };

    private static TaskCompletionSource<ITarget> _tcs;

    /// <summary>
    /// 让玩家选择一个目标(自定义过滤器)
    /// </summary>
    /// <param name="player">选中的玩家</param>
    /// <param name="cardModel">是哪个卡牌调用的</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static async Task<ITarget> PlayerChoiceTarget(Player player, CardModel cardModel, TargetType targetType, bool fromBuff, Func<ITarget, bool> filter = null,Camp? camp = null)
    {
        PushStop();
        player ??= PlayerManager.GetPlayer(cardModel.Camp);
        try
        {
            if (player == PlayerManager.HostPlayer)
            {
                var candidates = TargetRegistry.GetAllCanBeTarget(cardModel, targetType, fromBuff, filter).ToList();

                if (candidates.Count == 0) return null;

                if (candidates.Count == 1) return candidates[0];

                TargetRegistry.CallTargeted(cardModel, targetType, fromBuff, filter,camp);

                _tcs = new TaskCompletionSource<ITarget>();
                var result = await _tcs.Task;

                TargetRegistry.DeleteTargeted(targetType,camp);
                return result;
            }
            else
            {
                if (player is AiPlayer ai)
                {
                    return await ai.ChoiceTarget(cardModel,targetType,fromBuff,filter);
                }
                else
                {
                    return null;
                }
            }
        }
        finally
        {
            _tcs = null;
            PopStop();
        }
    }

    /// <summary>
    /// 让玩家选择一个单位(自定义过滤器)
    /// </summary>
    /// <param name="player">选中的玩家</param>
    /// <param name="cardModel">是哪个卡牌调用的</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static async Task<FighterCardModel> PlayerChoiceFighter(Player player, CardModel cardModel, bool fromBuff, Func<FighterCardModel, bool> filter = null)
    {
        filter ??= (f) => true;
        Func<ITarget, bool> itgFilter = (itg) =>
        {
            return itg.CanBeFighter(out var c) && filter(c);
        };
        var itg = await PlayerChoiceTarget(player, cardModel, TargetType.Fighters, fromBuff, itgFilter);
        return itg.CanBeFighter(out var fighter) ? fighter : null;
    }

    /// <summary>
    /// 让玩家选择一个僵尸(自定义过滤器)
    /// </summary>
    /// <param name="player">选中的玩家</param>
    /// <param name="cardModel">是哪个卡牌调用的</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static async Task<FighterCardModel> PlayerChoiceZombie(Player player, CardModel cardModel, bool fromBuff, Func<FighterCardModel, bool> filter = null)
    {
        filter ??= (f) => true;
        return await PlayerChoiceFighter(player, cardModel, fromBuff, (f) => f.Camp == Camp.Zombie && filter(f));
    }

    /// <summary>
    /// 让玩家选择一个植物(自定义过滤器)
    /// </summary>
    /// <param name="player">选中的玩家</param>
    /// <param name="cardModel">是哪个卡牌调用的</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static async Task<FighterCardModel> PlayerChoicePlant(Player player, CardModel cardModel, bool fromBuff, Func<FighterCardModel, bool> filter = null)
    {
        filter ??= (f) => true;
        return await PlayerChoiceFighter(player, cardModel, fromBuff, (f) => f.Camp == Camp.Plant && filter(f));
    }

    /// <summary>
    /// 让玩家选择一条线(自定义过滤器)
    /// </summary>
    /// <param name="player">选中的玩家</param>
    /// <param name="cardModel">是哪个卡牌调用的</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static async Task<Road> PlayerChoiceRoad(Player player, CardModel cardModel, bool fromBuff, Func<Road, bool> filter = null)
    {
        filter ??= (f) => true;
        Func<ITarget, bool> itgFilter = (itg) =>
        {
            return itg.CanBeRoad(out var r) && filter(r);
        };
        var itg = await PlayerChoiceTarget(player, cardModel, TargetType.Lines, fromBuff, itgFilter);
        return itg.CanBeRoad(out var road) ? road : null;
    }

    /// <summary>
    /// 让玩家选择一个格子(自定义过滤器)
    /// </summary>
    /// <param name="player">选中的玩家</param>
    /// <param name="cardModel">是哪个卡牌调用的</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static async Task<(Road road, Location? location)> PlayerChoiceGrid(Player player, CardModel cardModel, bool fromBuff, Func<Road, bool> filter = null, Camp? camp = null)
    {
        filter ??= (f) => true;
        player ??= PlayerManager.GetPlayer(cardModel.Camp);

        if (player is AiPlayer ai)
            return await ai.ChoiceGrid(cardModel, fromBuff, filter);

        Func<ITarget, bool> itgFilter = (itg) =>
        {
            return itg.CanBeRoad(out var r) && filter(r);
        };
        var itg = await PlayerChoiceTarget(player, cardModel, TargetType.Grids, fromBuff, itgFilter, camp) as NodeRoad;
        if (itg == null || !itg.CanBeRoad(out var road)) return (null, null);
        return (road, road.LastTargetKind == RoadTargetKind.None ? null : road.LastTargetKind switch
        {
            RoadTargetKind.Grid => itg.ClickedCamp == null ? null : itg.ClickedCamp is Camp camp1 && camp1.Plant() ? Location.Plant : Location.Zombie,
            RoadTargetKind.CoopGrid => Location.PlantFront,
            _ => null
        });
    }
    /// <summary>
    /// 选中目标后调用此方法
    /// </summary>
    /// <param name="target"></param>
    public static void SelectTarget(ITarget target)
    {
        _tcs?.TrySetResult(target);
    }

    /// <summary>
    /// 直接获取目标 而不需要玩家选取(自定义过滤器)
    /// </summary>
    /// <param name="cardModel">调用的卡牌</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="fromBuff">是不是由Buff而来</param>
    /// <param name="filter">过滤器</param>
    public static IEnumerable<ITarget> GetTargets(CardModel cardModel, TargetType targetType, bool fromBuff, Func<ITarget, bool> filter = null)
    {
        return TargetRegistry.GetAllCanBeTarget(cardModel,targetType,fromBuff,filter);
    }
    /// <summary>
    /// 获得按照先后顺序排列的单位
    /// </summary>
    /// <returns></returns>
    public static List<Fighter> GetOrderedFighters()
    {
        var fighters = TargetRegistry.All.OfType<Fighter>().ToList();
        fighters.Sort((a, b) =>
        {
            int byRoad = a.Road.Index.CompareTo(b.Road.Index);
            if (byRoad != 0) return byRoad;
            return LocationPriority(a.Model.Location)
                 .CompareTo(LocationPriority(b.Model.Location));
        });
        return fighters;
    }
    /// <summary>
    /// 获取最左侧（第一个）的单位（按 TimingOnCards 顺序：Road.Index 升序，再按 Location 优先级）
    /// </summary>
    /// <param name="predicate">过滤器（可选）</param>
    /// <param name="camp">阵营过滤（null 表示不区分）</param>
    /// <returns>第一个 FighterCardModel，没有则返回 null</returns>
    public static FighterCardModel GetFirstFighter(Func<FighterCardModel, bool> predicate = null, Camp? camp = null)
    {
        var query = TargetRegistry.All
            .OfType<Fighter>()
            .Where(f => f.Model != null && !f.Model.IsDie);

        if (camp != null)
            query = query.Where(f => f.Model.Camp == camp);

        if (predicate != null)
            query = query.Where(f => predicate(f.Model));

        return query
            .OrderBy(f => f.Road.Index)
            .ThenBy(f => LocationPriority(f.Model.Location))
            .Select(f => f.Model)
            .FirstOrDefault();
    }
    private static readonly List<(
        Func<CardModel, bool> Filter,
        Func<CardModel, Task> Action,
        HashSet<CardModel> Applied)> _cardHooks = new();
    /// <summary>
    /// 对满足filter的卡牌进行操作
    /// </summary>
    public static async Task ForCards(Func<CardModel, bool> filter, Func<CardModel, Task> action)
    {
        ArgumentNullException.ThrowIfNull(filter);

        ArgumentNullException.ThrowIfNull(action);

        var applied = new HashSet<CardModel>();
        _cardHooks.Add((filter, action, applied));

        var cards = CardInstances
            .Concat(TargetRegistry.All.OfType<Fighter>().Select(f => f.Model))
            .Where(c => c != null)
            .Distinct()
            .ToList();

        foreach (var card in cards)
        {
            if (applied.Contains(card) || !filter(card)) continue;
            applied.Add(card);
            await action(card);
        }
    }
    private static readonly List<Buff> Buffs = new();
    /// <summary>
    /// 向全局注册一个Buff，会持续监听时点
    /// </summary>
    /// <param name="buff">要添加的效果</param>
    /// <returns>收回该Buff的函数</returns>
    public static Action EnableBuff(Buff buff)
    {
        if (Buffs.Contains(buff)) return () => UnableBuff(buff);
        Buffs.Add(buff);
        return () => UnableBuff(buff);
    }

    /// <summary>
    /// 把全局注册的Buff收回
    /// </summary>
    /// <param name="buff"></param>
    public static void UnableBuff(Buff buff)
    {
        if (!Buffs.Contains(buff)) return;
        Buffs.Remove(buff);
    }

    /// <summary>
    /// 获取最左侧（第一个）的僵尸单位
    /// </summary>
    public static FighterCardModel GetFirstZombie(Func<FighterCardModel, bool> predicate = null)
        => GetFirstFighter(predicate, Camp.Zombie);

    /// <summary>
    /// 获取最左侧（第一个）的植物单位
    /// </summary>
    public static FighterCardModel GetFirstPlant(Func<FighterCardModel, bool> predicate = null)
        => GetFirstFighter(predicate, Camp.Plant);
    /// <summary>
    /// 生成战斗单位
    /// </summary>
    /// <param name="fighter">战斗单位卡牌</param>
    /// <param name="road">打出的道路</param>
    /// <param name="location">打出的位置</param>
    /// <returns></returns>
    public static async Task FighterGenerate(FighterCardModel fighter, Road road, Location location)
    {
        PushStop();
        await Fighter.Generate(fighter, road, location);
        if (!fighter.CardTag.HasFlag(CardTag.GraveStone))
            await fighter.IntroPlayed();
        await fighter.AnimationWhenPlayed(road);
        await fighter.FireTiming(Timing.AfterPlay, NodeRoad.GetNode(road));
        PopStop();
    }
    /// <summary>
    /// 搜寻有没有可以作为Target的对象
    /// </summary>
    /// <param name="predicate">过滤器</param>
    /// <returns></returns>
    public static bool AnyTarget(Func<ITarget, bool> predicate)
    {
        return TargetRegistry.All.Any(predicate);
    }

    /// <summary>
    /// 搜寻有没有符合目标的单位
    /// </summary>
    /// <param name="predicate">过滤器</param>
    /// <returns></returns>
    public static bool AnyFighter(Func<FighterCardModel, bool> predicate)
    => TargetRegistry.All.Any(t => t is Fighter f && predicate(f.Model) && !f.Model.IsDie);

    /// <summary>
    /// 搜寻有没有符合目标的僵尸单位
    /// </summary>
    /// <param name="predicate">过滤器</param>
    /// <returns></returns>
    public static bool AnyZombie(Func<FighterCardModel, bool> predicate = null,bool checkHiding = true)
        => AnyFighter(f => f != null
            && f.Camp == Camp.Zombie && (!f.Hiding || !checkHiding)
            && (predicate == null || predicate(f)));

    /// <summary>
    /// 搜寻有没有符合目标的植物单位
    /// </summary>
    /// <param name="predicate">过滤器</param>
    /// <returns></returns>
    public static bool AnyPlant(Func<FighterCardModel, bool> predicate = null)
        => AnyFighter(f => f != null
            && f.Camp == Camp.Plant
            && (predicate == null || predicate(f)));

    /// <summary>
    /// 将融合卡牌和目标卡牌融合
    /// </summary>
    /// <param name="fusioned">被融合的卡牌</param>
    /// <param name="fusioner">要融合目标的卡牌</param>
    /// <returns></returns>
    public static async Task Fuse(FighterCardModel fusioned, FighterCardModel fusioner)
    {
        PushStop();
        await fusioner.Fuse(fusioned);
        await fusioner.FireTiming(Timing.WhenFuse, fusioned, fusioner);
        await TimingOnCards(fusioner, Timing.WhenFuse, fusioned, fusioner);
        PopStop();
    }

    /// <summary>
    /// 检测有没有能被进化卡牌进化的单位
    /// </summary>
    /// <param name="fusioner">要进化的卡牌</param>
    /// <returns></returns>
    public static bool IsAnyOneCanEvolved(FighterCardModel fusioner)
    {
        if (!fusioner.IsEvolution) return false;
        return AnyFighter(f => fusioner.CanEvolution(f));
    }

    /// <summary>
    /// 进化卡牌到一个卡牌上
    /// </summary>
    /// <param name="evolved">被进化的卡牌</param>
    /// <param name="evolver">要进化的卡牌</param>
    /// <returns></returns>
    public static async Task Evolve(FighterCardModel evolver, FighterCardModel evolved)
    {
        PushStop();
        await evolver.Evolve(evolved);
        await evolver.FireTiming(Timing.WhenFuse, evolved, evolver);
        await TimingOnCards(evolver, Timing.WhenFuse, evolved, evolver);
        PopStop();
    }
    /// <summary>
    /// 检查是否能生成某个单位
    /// </summary>
    /// <typeparam name="T">要生成单位的类</typeparam>
    /// <returns></returns>
    public static bool CanGenerate<T>() where T : FighterCardModel
    {
        return AnyTarget(t => t.CanBeRoad(out var road) && Fighter.CanGenerate(CardModel.GetTemplate<T>() as T, road));
    }
    /// <summary>
    /// 检查某条线是否能生成某个单位
    /// </summary>
    /// <typeparam name="T">要生成单位的类</typeparam>
    /// <returns></returns>
    public static bool CanGenerate<T>(Road road) where T : FighterCardModel
    {
        return Fighter.CanGenerate(CardModel.GetTemplate<T>() as T, road);
    }
}