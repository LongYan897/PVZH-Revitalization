using Battle;
using Logger;
using System.Collections.Generic;
using System.Linq;

namespace Phrases;

public enum Phrase
{
    /// <summary>
    /// 非战斗状态
    /// </summary>
    None,
    /// <summary>
    /// 僵尸主要回合
    /// </summary>
    Zombie,
    /// <summary>
    /// 植物主要回合
    /// </summary>
    Plant,
    /// <summary>
    /// 僵尸锦囊牌回合
    /// </summary>
    Trick,
    /// <summary>
    /// 战斗回合
    /// </summary>
    Fight
}
public static class PhraseManager
{
    /// <summary>
    /// 记录是否在战斗中
    /// </summary>
    public static bool IsInBattle { get; private set; } = false;
    /// <summary>
    /// 如果在战斗中则为正常的阶段 非战斗设置为Phrase.None
    /// </summary>
    public static Phrase Phrase { get; private set; } = Phrase.None;
    /// <summary>
    /// 记录战斗中的道路
    /// </summary>
    private static readonly List<Road> Roads = new()
    {
        new Road() {Index = 1,Type = RoadType.Height},
        new Road() {Index = 2,Type = RoadType.Ground},
        new Road() {Index = 3,Type = RoadType.Ground},
        new Road() {Index = 4,Type = RoadType.Ground},
        new Road() {Index = 5,Type = RoadType.Water},
    };
    /// <summary>
    /// 获取战斗中的道路
    /// </summary>
    /// <param name="Index">道路的编号 1 代表高地 5 代表水路 234 代表平地</param>
    /// <returns></returns>
    public static Road GetRoad(int Index)
    {
        if (!IsInBattle)
        {
            Log.Error("在非战斗场景下获取了道路");
            return null;
        }
        if (Index > 5 || Index <= 0)
        {
            Log.Error("道路数量只有五个 却尝试获取第六个以上或者第零个以下的道路");
            return null;
        }
        return Roads.First(r => r.Index == Index);
    }
}
