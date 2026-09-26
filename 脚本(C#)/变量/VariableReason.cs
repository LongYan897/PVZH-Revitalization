
using System;

namespace Variable;

[Flags]
public enum VariableReason
{
    None = 1 << 0,
    /// <summary>
    /// 植物效果
    /// </summary>
    Plant = 1 << 1,
    /// <summary>
    /// 僵尸效果
    /// </summary>
    Zombie = 1 << 2,
    /// <summary>
    /// 英雄效果
    /// </summary>
    Hero = 1 << 3,
    /// <summary>
    /// 单位效果
    /// </summary>
    Fighter = 1 << 4,
    /// <summary>
    /// 锦囊牌效果 如果是超能力建议使用 Hero + Trick
    /// </summary>
    Trick = 1 << 5,
    /// <summary>
    /// 环境效果,
    /// </summary>
    Environment = 1 << 6,
    /// <summary>
    /// 来自其他卡牌的效果
    /// </summary>
    Diff = 1 << 7,
    /// <summary>
    /// 来自本卡牌的效果,
    /// </summary>
    Self = 1 << 8,
    /// <summary>
    /// 重置效果
    /// </summary>
    Reset = 1 << 9
}
