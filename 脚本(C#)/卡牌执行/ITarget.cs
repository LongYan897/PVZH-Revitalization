using Battle;
using Battle.Entity;
using Card;
using System;

namespace Target;

public interface ITarget
{
    /// <summary>本目标支持目标类型（Fighters / Lines / Grids / CoopGrids）</summary>
    TargetType TargetType { get; }

    /// <summary>当前是否处于“可被选中”的高亮状态</summary>
    bool Calling { get; }

    /// <summary>卡牌能否选中本目标</summary>
    Func<CardModel, Func<ITarget, bool>, bool> CanBeTarget { get; }

    /// <summary>开始高亮（等待玩家选择）</summary>
    void OnCallTargeted(TargetType type);

    /// <summary>结束高亮</summary>
    void OnDeleteTargeted(TargetType type);

    /// <summary>鼠标悬停/进入时的高亮</summary>
    void OnTargeted(TargetContext ctx);

    /// <summary>鼠标离开时的恢复</summary>
    void OnDistargeted(TargetContext ctx);
}

public static class TargetExtension
{
    public static bool IsFighter(this ITarget target, Func<FighterCardModel, bool> filter)
        => target is Fighter fighter && filter(fighter.Model);

    public static bool IsRoad(this ITarget target, Func<Road, bool> filter)
        => target is NodeRoad road && filter(road.Model);

    public static bool CanBeFighter(this ITarget target,out FighterCardModel fighterCard)
    {
        if (target is Fighter fighter)
        {
            fighterCard = fighter.Model;
            return true;
        }
        fighterCard = null;
        return false;
    }
    public static bool CanBeRoad(this ITarget target, out Road road)
    {
        if (target is NodeRoad nodeRoad)
        {
            road = nodeRoad.Model;
            return true;
        }
        road = null;
        return false;
    }
}