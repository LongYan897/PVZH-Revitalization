using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 卡牌描述URL的注册区域
/// </summary>
public static class CardRegistryField
{
    [URL]
    public static string 消灭 => "使单位立即死亡。";
    [URL]
    public static string 打出 => "单位被打出到场上时。";
    [URL]
    public static string 额外攻击 => "使单位额外进行攻击。";
    [URL]
    public static string 召唤 => "在一条线上生成单位。";
    [URL]
    public static string 额外战斗 => "使一条线上的所有单位额外进行攻击";
}
