using System.Collections.Generic;

namespace Card.Pack;

/// <summary>
/// 卡包 : 可以用来存储一个系列的卡牌 也可以用于当作卡牌组
/// </summary>
public abstract class CardPack
{
    /// <summary>
    /// 卡包包含的卡牌
    /// </summary>
    public abstract IReadOnlyList<CardPackEntry> Cards { get; }
}
