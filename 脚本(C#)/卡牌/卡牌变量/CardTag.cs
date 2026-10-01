using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 卡牌关键词
/// </summary>
[Flags]
public enum CardTag
{
    None = 1 << 0,
    Coop = 1 << 1,
    Amphibious = 1 << 2
}
