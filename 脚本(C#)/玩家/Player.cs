using Card;
using System.Collections.Generic;

namespace Play;

/// <summary>
/// 玩家
/// 用于记录玩家状态和战斗状态
/// 可以记录局内和局外的数据
/// </summary>
/// <param name="id"></param>
public class Player
{
    private Player() { }
    public Player(string id) { Id = id; }
    /// <summary>
    /// 玩家的名称
    /// </summary>
    public string Id { get; private set; }
    /// <summary>
    /// 玩家获得的卡牌(这里面的卡牌均为克隆模板切勿修改)
    /// </summary>
    private readonly List<CardModel> _cards = new List<CardModel>();
    public IReadOnlyList<CardModel> Cards => _cards;
    /// <summary>
    /// 玩家的战斗状态
    /// </summary>
    public readonly PlayerBox Status = new();
}
