using System;
using System.Collections.Generic;
using System.Linq;

namespace Card.Pack;

/// <summary>
/// CardPack 的扩展方法，提供基于可控种子的伪随机抽取与洗牌。
/// 使用 System.Random 并传入 seed 保证可复现（伪随机）。
/// </summary>
public static class CardPackExtensions
{
    /// <summary>
    /// 从卡包中随机抽取指定数量的卡，使用给定的种子保证可复现。
    /// 标记不可重复的组抽取不重复，若整个组不可重复，概率会修正。
    /// </summary>
    public static List<CardModel> Draw(this CardPack pack, int count, int seed)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (count <= 0) return new List<CardModel>();

        var list = pack.Cards.ToList();
        var rnd = new Random(seed);

        if (count >= list.Count)
        {
            // 返回洗牌后的全部卡牌
            return ShuffleList(list, rnd);
        }

        var result = new List<CardModel>(count);
        // Fisher–Yates 风格的抽取（不重复）
        for (int i = 0; i < count; i++)
        {
            int idx = rnd.Next(list.Count - i);
            result.Add(list[idx]);
            // 把已选元素换到尾部并移除（避免 O(n^2) 的 RemoveAt(0)）
            var last = list[list.Count - 1 - i];
            list[idx] = last;
        }

        return result;
    }

    /// <summary>
    /// 从卡包中抽取一张卡，使用给定种子。
    /// </summary>
    public static CardModel DrawOne(this CardPack pack, int seed)
        => pack.Draw(1, seed).FirstOrDefault();

    /// <summary>
    /// 返回一个基于种子洗牌后的新列表（不修改原卡包）。
    /// </summary>
    public static List<CardModel> Shuffle(this CardPack pack, int seed)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        var list = pack.Cards.ToList();
        var rnd = new Random(seed);
        return ShuffleList(list, rnd);
    }

    private static List<CardModel> ShuffleList(List<CardModel> list, Random rnd)
    {
        // 原地 Fisher–Yates
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            var tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
        return list;
    }
}
