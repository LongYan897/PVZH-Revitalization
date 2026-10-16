
using Card;
using System.Collections.Generic;
using System.Linq;

namespace Pack;

/// <summary>
/// 列表的扩展
/// </summary>
public static class ListExtension
{
    /// <summary>
    /// 为列表添加N张卡牌
    /// </summary>
    /// <typeparam name="T">卡牌类</typeparam>
    /// <param name="list">列表</param>
    /// <param name="amount">数量</param>
    public static void Repeat<T>(this List<T> list, int amount) where T : CardModel
    {
        List<T> values = new();
        for (int i = 0;i < amount; i++)
        {
            values.Add((T)CardModel.Load<T>());
        }
        list.AddRange(values);
    }
    /// <summary>
    /// 为列表添加N张卡牌
    /// </summary>
    /// <typeparam name="T">卡牌类</typeparam>
    /// <param name="list">列表</param>
    /// <param name="amount">数量</param>
    public static void Repeat<T>(this List<CardModel> list, int amount) where T : CardModel
    {
        List<T> values = new();
        for (int i = 0; i < amount; i++)
        {
            values.Add((T)CardModel.Load<T>());
        }
        list.AddRange(values);
    }
    /// <summary>
    /// 为列表添加N张卡牌
    /// </summary>
    /// <typeparam name="T">卡牌类</typeparam>
    /// <param name="list">列表</param>
    /// <param name="amount">数量</param>
    public static List<T> RepeatLoad<T>(this List<T> list, int amount) where T : CardModel
    {
        List<T> values = new();
        for (int i = 0; i < amount; i++)
        {
            values.Add((T)CardModel.Load<T>());
        }
        list.AddRange(values);
        return list;
    }
    /// <summary>
    /// 为列表添加N张卡牌
    /// </summary>
    /// <typeparam name="T">卡牌类</typeparam>
    /// <param name="list">列表</param>
    /// <param name="amount">数量</param>
    public static List<CardModel> RepeatLoad<T>(this List<CardModel> list, int amount) where T : CardModel
    {
        List<T> values = new();
        for (int i = 0; i < amount; i++)
        {
            values.Add((T)CardModel.Load<T>());
        }
        list.AddRange(values);
        return list;
    }
}
