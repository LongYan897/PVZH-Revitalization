using Battle;
using Battle.Entity;
using Controller;
using Godot;
using Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Target;

namespace Card.Cmd;

/// <summary>
/// 用于卡牌的命令
/// </summary>
public static class CardCmd
{
    private static List<CardModel> CardInstances = new List<CardModel>();

    public static bool IsCardPlaying { get; private set; }
    public static void Clear() => CardInstances.Clear();
    /// <summary>
    /// 向Cmd添加卡牌
    /// </summary>
    /// <param name="cardModel"></param>
    public static void Add(CardModel cardModel)
    {
        CardInstances.Add(cardModel);
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
    public static async Task ChangedCard<T>(CardModel cardModel, string key, T before, T after)
    {
        cardModel.Fresh();
        await cardModel.Changed<T>(key, before, after);
        foreach (CardModel card in CardInstances.ToList())
        {
            await card.FireTiming(Timing.OnVariableChanged, key, before, after);
        }
    }
    /// <summary>
    /// 打出卡牌后执行所有卡牌打出卡牌的逻辑
    /// </summary>
    /// <param name="cardModel">打出的卡牌</param>
    /// <param name="target">打出卡牌的对象</param>
    /// <returns></returns>
    public static async Task PlayedCard(CardModel cardModel, ITarget target)
    {
        IsCardPlaying = true;
        JustFreshAllCards();
        await cardModel.Play(target);
        foreach (CardModel card in CardInstances.ToList())
        {
            await card.FireTiming(Timing.AfterCardPlayed, cardModel, target);
        }
        IsCardPlaying = false;
        JustFreshAllCards();
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
    /// 触发卡牌的时点
    /// </summary>
    /// <param name="cardModel">忽略的卡牌（一般是发起者）</param>
    /// <param name="timing">时点</param>
    /// <param name="paramters">参数</param>
    /// <returns></returns>
    public static async Task TimingOnCards(CardModel cardModel, Timing timing, params object[] paramters)
    {
        JustFreshAllCards();
        foreach (var card in CardInstances.ToList())
        {
            if (card != cardModel)
                await card.FireTiming(timing, paramters);
        }
        JustFreshAllCards();
    }
    private static TaskCompletionSource<ITarget> _tcs;
    /// <summary>
    /// 让玩家选择一个目标
    /// </summary>
    /// <param name="player">选中的玩家</param>
    /// <param name="cardModel">选中的卡牌</param>
    /// <param name="targetType">目标类型</param>
    /// <returns></returns>
    public static async Task<ITarget> PlayerChoiceTarget(Player player, CardModel cardModel, TargetType targetType)
    {
        TargetRegistry.CallTargeted(cardModel, targetType);

        _tcs = new TaskCompletionSource<ITarget>();
        var result = await _tcs.Task;

        TargetRegistry.DeleteTargeted(targetType);

        _tcs = null;
        return result;
    }
    /// <summary>
    /// 让玩家选择一个目标(自定义过滤器)
    /// </summary>
    /// <param name="player">选中的玩家</param>
    /// <param name="cardModel">是哪个卡牌调用的</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static async Task<ITarget> PlayerChoiceTarget(Player player, CardModel cardModel, TargetType targetType, Func<ITarget, bool> filter = null)
    {
        TargetRegistry.CallTargeted(cardModel, targetType, filter);

        _tcs = new TaskCompletionSource<ITarget>();
        var result = await _tcs.Task;

        TargetRegistry.DeleteTargeted(targetType);

        _tcs = null;
        return result;
    }
    /// <summary>
    /// 让玩家选择一个单位(自定义过滤器)
    /// </summary>
    /// <param name="player">选中的玩家</param>
    /// <param name="cardModel">是哪个卡牌调用的</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static async Task<FighterCardModel> PlayerChoiceFighter(Player player, CardModel cardModel, Func<FighterCardModel, bool> filter = null)
    {
        filter ??= (f) => true;
        Func<ITarget, bool> itgFilter = (itg) =>
        {
            return itg.CanBeFighter(out var c) && filter(c);
        };
        var itg = await PlayerChoiceTarget(player, cardModel, TargetType.Fighters, itgFilter);
        return itg.CanBeFighter(out var fighter) ? fighter : null;
    }
    /// <summary>
    /// 让玩家选择一个僵尸(自定义过滤器)
    /// </summary>
    /// <param name="player">选中的玩家</param>
    /// <param name="cardModel">是哪个卡牌调用的</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static async Task<FighterCardModel> PlayerChoiceZombie(Player player, CardModel cardModel, Func<FighterCardModel, bool> filter = null)
    {
        filter ??= (f) => true;
        return await PlayerChoiceFighter(player, cardModel, (f) => f.Camp == Camp.Zombie && filter(f));
    }
    /// <summary>
    /// 让玩家选择一个植物(自定义过滤器)
    /// </summary>
    /// <param name="player">选中的玩家</param>
    /// <param name="cardModel">是哪个卡牌调用的</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static async Task<FighterCardModel> PlayerChoicePlant(Player player, CardModel cardModel, Func<FighterCardModel, bool> filter = null)
    {
        filter ??= (f) => true;
        return await PlayerChoiceFighter(player, cardModel, (f) => f.Camp == Camp.Plant && filter(f));
    }
    /// <summary>
    /// 让玩家选择一条线(自定义过滤器)
    /// </summary>
    /// <param name="player">选中的玩家</param>
    /// <param name="cardModel">是哪个卡牌调用的</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static async Task<Road> PlayerChoiceRoad(Player player, CardModel cardModel, Func<Road, bool> filter = null)
    {
        filter ??= (f) => true;
        Func<ITarget, bool> itgFilter = (itg) =>
        {
            return itg.CanBeRoad(out var r) && filter(r);
        };
        var itg = await PlayerChoiceTarget(player, cardModel, TargetType.Lines, itgFilter);
        return itg.CanBeRoad(out var road) ? road : null;
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
    /// 生成战斗单位
    /// </summary>
    /// <param name="fighter">战斗单位卡牌</param>
    /// <param name="road">打出的道路</param>
    /// <param name="location">打出的位置</param>
    /// <returns></returns>
    public static async Task FighterGenerate(FighterCardModel fighter, Road road, Location location)
    {
        var fight = Fighter.Generate(fighter, road, location);
        await fighter.FireTiming(Timing.OnFighterEnter, road, fighter);
        await TimingOnCards(fighter, Timing.OnFighterEnter, road, fighter);
        await fighter.IntroPlayed();
        await fighter.AnimationWhenPlayed(road);
        await fighter.FireTiming(Timing.AfterPlay, NodeRoad.GetNode(road));
        await fighter.FireTiming(Timing.AfterRoadChanged, road);
        await TimingOnCards(fighter, Timing.AfterRoadChanged, road);
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
    => TargetRegistry.All.Any(t => t is Fighter f && predicate(f.Model));

    /// <summary>
    /// 搜寻有没有符合目标的僵尸单位
    /// </summary>
    /// <param name="predicate">过滤器</param>
    /// <returns></returns>
    public static bool AnyZombie(Func<FighterCardModel, bool> predicate = null)
        => AnyFighter(f => f != null
            && f.Camp == Camp.Zombie
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
    /// 播放场景动画
    /// </summary>
    /// <param name="kind">场景动画类型</param>
    /// <param name="startAt">起始位置（全局坐标）</param>
    /// <param name="endAt">结束位置（全局坐标）</param>
    /// <param name="duration">位移时长（秒）</param>
    /// <param name="soundPath">可选：音效路径，为 null 不播</param>
    /// <param name="soundVolumeDb">音效音量</param>
    /// <param name="soundPitch">音效音调</param>
    public static async Task PlaySceneAnim(
        SceneAnimaActor.SceneAnimKind kind,
        Vector2 startAt,
        Vector2 endAt,
        float duration,
        string soundPath = null,
        float soundVolumeDb = 0f,
        float soundPitch = 1f)
    {
        await SceneAnimaActor.PlayCardAsync(kind, startAt, endAt, duration, soundPath, soundVolumeDb, soundPitch);
    }

    /// <summary>
    /// 战斗单位倒下动画
    /// </summary>
    public static async Task FighterDown(Vector2 startAt, Vector2 endAt, float duration, string soundPath = null)
    {
        await SceneAnimaActor.FighterDownAsync(startAt, endAt, duration, soundPath);
    }

    /// <summary>
    /// 卡牌落下动画
    /// </summary>
    public static async Task CardDown(Vector2 startAt, Vector2 endAt, float duration, string soundPath = null)
    {
        await SceneAnimaActor.CardDownAsync(startAt, endAt, duration, soundPath);
    }

    /// <summary>
    /// 卡牌到动画
    /// </summary>
    public static async Task CardToAnim(Vector2 startAt, Vector2 endAt, float duration, string soundPath = null)
    {
        await SceneAnimaActor.CardToAnimAsync(startAt, endAt, duration, soundPath);
    }

    /// <summary>
    /// 动画到描述
    /// </summary>
    public static async Task AnimToDes(Vector2 startAt, Vector2 endAt, float duration, string soundPath = null)
    {
        await SceneAnimaActor.AnimToDesAsync(startAt, endAt, duration, soundPath);
    }
}