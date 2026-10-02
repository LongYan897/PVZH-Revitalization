using System;

namespace Card;

[Flags]
/// <summary>
/// 目标类型
/// </summary>
public enum TargetType
{
    /// <summary>
    /// 以战斗单位目标
    /// </summary>
    Fighters = 1 << 0,
    /// <summary>
    /// 以可放置行
    /// </summary>
    Lines = 1 << 1,
    /// <summary>
    /// 以可放置格子
    /// </summary>
    Grids = 1 << 2,
    /// <summary>
    /// 以可放置组队格子
    /// </summary>
    CoopGrids = 1 << 3,
    /// <summary>
    /// 以英雄为目标
    /// </summary>
    Hero = 1 << 4,
}
