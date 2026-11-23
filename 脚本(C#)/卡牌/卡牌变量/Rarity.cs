
namespace Card;

/// <summary>
/// 稀有度
/// </summary>
public enum Rarity
{
    /// <summary>
    /// 无
    /// </summary>
    None,
    /// <summary>
    /// 基础
    /// </summary>
    Basic,
    /// <summary>
    /// 常见
    /// </summary>
    Common,
    /// <summary>
    /// 罕见
    /// </summary>
    Uncommon,
    /// <summary>
    /// 稀有
    /// </summary>
    Rare,
    /// <summary>
    /// 超稀有
    /// </summary>
    SuperRare,
    /// <summary>
    /// 传说
    /// </summary>
    Legend,
    /// <summary>
    /// 活动
    /// </summary>
    Activity,
    /// <summary>
    /// 令牌
    /// </summary>
    Token
}

public static class RarityExtension
{
    public static string ToName(this Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Activity => "活动",
            Rarity.Basic => "基础",
            Rarity.Common => "常见",
            Rarity.Uncommon => "罕见",
            Rarity.Legend => "传奇",
            Rarity.None => "无",
            Rarity.Rare => "稀有",
            Rarity.SuperRare => "超稀有",
            Rarity.Token => "令牌",
            _ => ""
        };
    }
}
