using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 卡牌类别
/// </summary>
[Flags]
public enum CardType
{
    /// <summary>
    /// 无
    /// </summary>
    None = 1 << 0,
    /// <summary>
    /// 英雄相关卡牌
    /// </summary>
    Hero = 1 << 1,
    /// <summary>
    /// 锦囊牌
    /// </summary>
    Trick = 1 << 2,
    /// <summary>
    /// 单位
    /// </summary>
    Fighter = 1 << 3,
    /// <summary>
    /// 环境牌
    /// </summary>
    Environment = 1 << 4,
}
