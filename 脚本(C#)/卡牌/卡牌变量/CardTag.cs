using System;

namespace Card;

/// <summary>
/// 卡牌关键词
/// </summary>
[Flags]
public enum CardTag
{
    None = 1 << 0,
    /// <summary>
    /// 组队
    /// </summary>
    Coop = 1 << 1,
    /// <summary>
    /// 两栖
    /// </summary>
    Amphibious = 1 << 2,
    /// <summary>
    /// 墓碑
    /// </summary>
    GraveStone = 1 << 3
}
