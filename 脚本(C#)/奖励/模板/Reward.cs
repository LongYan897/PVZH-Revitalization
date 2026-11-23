using Card;
using Hero.Model;

namespace Play;

/// <summary>
/// 奖励类型
/// </summary>
public enum RewardType
{
    /// <summary>
    /// 宝石
    /// </summary>
    Diamond,

    /// <summary>
    /// 火花
    /// </summary>
    Spark,

    /// <summary>
    /// 票券
    /// </summary>
    Ticket,

    /// <summary>
    /// 卡牌
    /// </summary>
    Card,

    /// <summary>
    /// 英雄
    /// </summary>
    Hero,
}

/// <summary>
/// 奖励
/// </summary>
public class Reward
{
    /// <summary>
    /// 奖励类型
    /// </summary>
    public RewardType Type { get; private init; }

    /// <summary>
    /// 数量
    /// </summary>
    public int Amount { get; private init; } = 1;

    /// <summary>
    /// 卡牌标题
    /// </summary>
    public string CardTitle { get; private init; }

    /// <summary>
    /// 英雄标题
    /// </summary>
    public string HeroTitle { get; private init; }

    /// <summary>
    /// 创建宝石奖励
    /// </summary>
    /// <param name="amount">宝石数量</param>
    /// <returns>奖励</returns>
    public static Reward Diamond(int amount)
        => new() { Type = RewardType.Diamond, Amount = amount };

    /// <summary>
    /// 创建火花奖励
    /// </summary>
    /// <param name="amount">火花数量</param>
    /// <returns>奖励</returns>
    public static Reward Spark(int amount)
        => new() { Type = RewardType.Spark, Amount = amount };

    /// <summary>
    /// 创建票券奖励
    /// </summary>
    /// <param name="amount">票券数量</param>
    /// <returns>奖励</returns>
    public static Reward Ticket(int amount)
        => new() { Type = RewardType.Ticket, Amount = amount };

    /// <summary>
    /// 创建卡牌奖励
    /// </summary>
    /// <param name="title">卡牌名称</param>
    /// <param name="amount">张数</param>
    /// <returns>奖励</returns>
    public static Reward Card(string title, int amount = 1)
        => new() { Type = RewardType.Card, CardTitle = title, Amount = amount };

    /// <summary>
    /// 创建英雄奖励
    /// </summary>
    /// <param name="title">英雄名称</param>
    /// <returns>奖励</returns>
    public static Reward Hero(string title)
        => new() { Type = RewardType.Hero, HeroTitle = title };

    /// <summary>
    /// 应用到玩家资产
    /// </summary>
    /// <param name="asset">目标资产</param>
    public void ApplyTo(PlayerAsset asset)
    {
        if (asset == null) return;

        switch (Type)
        {
            case RewardType.Diamond:
                asset.GainDiamond(Amount);
                break;
            case RewardType.Spark:
                asset.GainSpark(Amount);
                break;
            case RewardType.Ticket:
                asset.GainTicket(Amount);
                break;
            case RewardType.Card:
                ApplyCard(asset);
                break;
            case RewardType.Hero:
                ApplyHero(asset);
                break;
        }
    }

    /// <summary>
    /// 应用卡牌奖励
    /// </summary>
    /// <param name="asset">目标资产</param>
    private void ApplyCard(PlayerAsset asset)
    {
        if (string.IsNullOrEmpty(CardTitle)) return;
        var template = CardModel.GetTemplate(CardTitle);
        if (template == null) return;
        for (int i = 0; i < Amount; i++)
            asset.AddCard(template);
    }

    /// <summary>
    /// 应用英雄奖励
    /// </summary>
    /// <param name="asset">目标资产</param>
    private void ApplyHero(PlayerAsset asset)
    {
        if (string.IsNullOrEmpty(HeroTitle)) return;
        var hero = HeroModel.Identity(HeroTitle);
        if (hero == null) return;
        asset.UnlockHero(hero);
    }
}