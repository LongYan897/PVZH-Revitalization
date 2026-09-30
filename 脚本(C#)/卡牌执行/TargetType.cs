using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

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
    Lines
}
