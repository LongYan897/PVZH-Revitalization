using System;

namespace Card;

[Flags]
/// <summary>
/// 目标类型
/// </summary>
public enum TargetType
{
    /// <summary>
    /// 以可放置格子
    /// </summary>
    Fighter = 0 << 1,
    /// <summary>
    /// 以战斗单位目标
    /// </summary>
    Fighters = 1 << 1,
    /// <summary>
    /// 以可放置行
    /// </summary>
    Lines = 1 << 2,
    /// <summary>
    /// 以可放置格子
    /// </summary>
    Grids = 1 << 3,
    /// <summary>
    /// 以可放置组队格子
    /// </summary>
    CoopGrids = 1 << 4,
    /// <summary>
    /// 以战斗单位目标和可放置格子
    /// </summary>
    FighterAndGrids = 1 << 5,
    /// <summary>
    /// 以战斗单位目标和可放置组队格子
    /// </summary>
    FighterAndCoopGrids = 1 << 6,
}
