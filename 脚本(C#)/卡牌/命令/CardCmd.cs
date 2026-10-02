using Battle;
using Battle.Entity;
using Godot;
using Play;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Target;

namespace Card.Cmd;

/// <summary>
/// 用于卡牌的命令
/// </summary>
public static class CardCmd
{
    private static List<CardModel> CardInstances = new List<CardModel>();
    private static ITarget _cachedTarget;

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
        foreach (CardModel card in CardInstances)
        {
            await card.FireTiming(Timing.OnVariableChanged,key,before,after);
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
        await cardModel.Play(target);
        foreach (CardModel card in CardInstances)
        {
            await card.FireTiming(Timing.AfterCardPlayed,cardModel,target);
        }
    }
    /// <summary>
    /// 触发卡牌的时点
    /// </summary>
    /// <param name="cardModel">忽略的卡牌（一般是发起者）</param>
    /// <param name="timing">时点</param>
    /// <param name="paramters">参数</param>
    /// <returns></returns>
    public static async Task TimingOnCards(CardModel cardModel,Timing timing,params object[] paramters)
    {
        foreach (var card in CardInstances)
        {
            if (card != cardModel)
                await card.FireTiming(timing,paramters);
        }
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
        Fighter.CallTargeted(cardModel, targetType);
        NodeRoad.CallTargeted(cardModel, targetType);

        _tcs = new TaskCompletionSource<ITarget>();
        var result = await _tcs.Task;

        Fighter.DeleteTargeted(targetType);
        NodeRoad.DeleteTargeted(targetType);

        _tcs = null;
        return result;
    }
    /// <summary>
    /// 让玩家选择一个目标(自定义过滤器)
    /// </summary>
    /// <param name="player">选中的玩家</param>
    /// <param name="cardModel">选中的卡牌</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="filter">过滤器</param>
    /// <returns></returns>
    public static async Task<ITarget> PlayerChoiceTarget(Player player, CardModel cardModel, TargetType targetType, Func<ITarget, bool> filter)
    {
        Fighter.CallTargeted(cardModel, targetType, filter);
        NodeRoad.CallTargeted(cardModel, targetType, filter);

        _tcs = new TaskCompletionSource<ITarget>();
        var result = await _tcs.Task;

        Fighter.DeleteTargeted(targetType);
        NodeRoad.DeleteTargeted(targetType);

        _tcs = null;
        return result;
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
    public static async Task FighterGenerate(FighterCardModel fighter,Road road,Location location)
    {
        var fight = Fighter.Generate(fighter,road,location);
        await fighter.IntroPlayed();
        await fighter.AnimationWhenPlayed(road);
        await fighter.FireTiming(Timing.AfterPlay,NodeRoad.GetNode(road));
    }
    /// <summary>
    /// 从卡组中抽出卡牌
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public static async Task Draw(int amount = 1)
    {
    }
}
