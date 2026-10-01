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
    Fighter,
    /// <summary>
    /// 以战斗单位目标
    /// </summary>
    Fighters,
    /// <summary>
    /// 以可放置行
    /// </summary>
    Lines,
    /// <summary>
    /// 以可放置格子
    /// </summary>
    Grids,
    /// <summary>
    /// 以战斗单位目标和可放置格子
    /// </summary>
    FighterAndGrids,
}
