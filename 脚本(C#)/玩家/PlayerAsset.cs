using Card;
using Card.Pack;
using Controller;
using Hero.Model;
using System.Collections.Generic;
using System.Linq;
using Variable;

namespace Play;

/// <summary>
/// 玩家资产
/// </summary>
public class PlayerAsset
{
    /// <summary>
    /// 玩家的幸运值，影响游戏中随机事件的概率
    /// </summary>
    public Variable<double> Lucky { get; private set; } = new Variable<double>("Lucky", 0.0);

    /// <summary>
    /// 玩家的钻石数量
    /// </summary>
    public int Diamond { get; private set; } = 1000;

    /// <summary>
    /// 玩家的火花数量
    /// </summary>
    public int Spark { get; private set; }

    /// <summary>
    /// 玩家的票券数量
    /// </summary>
    public int Ticket { get; private set; }

    private readonly List<HeroModel> _heroes = new();

    /// <summary>
    /// 玩家拥有的英雄
    /// </summary>
    public IReadOnlyList<HeroModel> Heroes => _heroes;

    /// <summary>
    /// 玩家拥有的卡组
    /// </summary>
    public IReadOnlyList<PresetPack> Presets { get; private set; } = new List<PresetPack>
    {
        PresetPack.Load<AllPea>()
    };

    private readonly List<CardModel> _cards = new();

    /// <summary>
    /// 玩家拥有的卡牌（这里面的卡牌均为克隆模板，切勿修改）
    /// </summary>
    public IReadOnlyList<CardModel> Cards => _cards;

    /// <summary>
    /// 获得钻石
    /// </summary>
    /// <param name="amount">钻石数量</param>
    public void GainDiamond(int amount)
    {
        if (amount <= 0) return;
        Diamond += amount;
        GemCenter.GainGem(amount);
    }

    /// <summary>
    /// 消耗钻石
    /// </summary>
    /// <param name="amount">钻石数量</param>
    /// <returns>是否成功消耗</returns>
    public bool LoseDiamond(int amount)
    {
        if (amount <= 0) return false;
        if (Diamond < amount) return false;
        Diamond -= amount;
        GemCenter.LoseGem(amount);
        return true;
    }

    /// <summary>
    /// 获得火花
    /// </summary>
    /// <param name="amount">火花数量</param>
    public void GainSpark(int amount)
    {
        if (amount <= 0) return;
        Spark += amount;
    }

    /// <summary>
    /// 消耗火花
    /// </summary>
    /// <param name="amount">火花数量</param>
    /// <returns>是否成功消耗</returns>
    public bool LoseSpark(int amount)
    {
        if (amount <= 0) return false;
        if (Spark < amount) return false;
        Spark -= amount;
        return true;
    }

    /// <summary>
    /// 获得票券
    /// </summary>
    /// <param name="amount">票券数量</param>
    public void GainTicket(int amount)
    {
        if (amount <= 0) return;
        Ticket += amount;
    }

    /// <summary>
    /// 消耗票券
    /// </summary>
    /// <param name="amount">票券数量</param>
    /// <returns>是否成功消耗</returns>
    public bool LoseTicket(int amount)
    {
        if (amount <= 0) return false;
        if (Ticket < amount) return false;
        Ticket -= amount;
        return true;
    }

    /// <summary>
    /// 是否拥有该英雄
    /// </summary>
    /// <param name="title">英雄标题</param>
    /// <returns>是否拥有</returns>
    public bool HasHero(string title)
        => !string.IsNullOrEmpty(title) && _heroes.Any(h => h.Data.Title == title);

    /// <summary>
    /// 是否拥有该英雄
    /// </summary>
    /// <param name="hero">英雄</param>
    /// <returns>是否拥有</returns>
    public bool HasHero(HeroModel hero)
        => hero != null && _heroes.Any(h => h.Data.Title == hero.Data.Title);

    /// <summary>
    /// 解锁一个英雄
    /// </summary>
    /// <param name="hero">英雄</param>
    public void UnlockHero(HeroModel hero)
    {
        if (hero == null) return;
        if (HasHero(hero)) return;
        _heroes.Add(hero);
    }

    /// <summary>
    /// 添加一张卡牌
    /// </summary>
    /// <param name="card">卡牌</param>
    public void AddCard(CardModel card)
    {
        if (card == null) return;
        _cards.Add(card);
    }

    /// <summary>
    /// 添加多张卡牌
    /// </summary>
    /// <param name="cards">卡牌</param>
    public void AddCards(params CardModel[] cards)
    {
        if (cards == null) return;
        foreach (var card in cards)
            AddCard(card);
    }

    /// <summary>
    /// 添加多张卡牌
    /// </summary>
    /// <param name="cards">卡牌</param>
    public void AddCards(IEnumerable<CardModel> cards)
    {
        if (cards == null) return;
        foreach (var card in cards)
            AddCard(card);
    }

    /// <summary>
    /// 移除一张卡牌
    /// </summary>
    /// <param name="card">卡牌</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveCard(CardModel card)
    {
        return card != null && _cards.Remove(card);
    }

    /// <summary>
    /// 根据卡牌类别获取拥有的数量
    /// </summary>
    /// <param name="card">卡牌</param>
    /// <returns>拥有的数量</returns>
    public int GetAmount(CardModel card)
    {
        if (card == null) return 0;
        return GetAmount(card.Title);
    }

    /// <summary>
    /// 根据卡牌标题获取拥有的数量
    /// </summary>
    /// <param name="title">卡牌标题</param>
    /// <returns>拥有的数量</returns>
    public int GetAmount(string title)
    {
        if (string.IsNullOrEmpty(title)) return 0;
        return _cards.Count(c => c.Title == title);
    }

    /// <summary>
    /// 根据卡牌类别（CardType）获取拥有的数量
    /// </summary>
    /// <param name="cardType">卡牌类别</param>
    /// <returns>拥有的数量</returns>
    public int GetAmount(CardType cardType)
    {
        return _cards.Count(c => c.CardType.HasFlag(cardType));
    }

    /// <summary>
    /// 根据派系（Class）获取拥有的数量
    /// </summary>
    /// <param name="class">派系</param>
    /// <returns>拥有的数量</returns>
    public int GetAmount(Class @class)
    {
        return _cards.Count(c => c.Class.HasFlag(@class));
    }

    /// <summary>
    /// 根据稀有度（Rarity）获取拥有的数量
    /// </summary>
    /// <param name="rarity">稀有度</param>
    /// <returns>拥有的数量</returns>
    public int GetAmount(Rarity rarity)
    {
        return _cards.Count(c => c.Rarity == rarity);
    }

    /// <summary>
    /// 添加一个卡组
    /// </summary>
    /// <param name="preset">卡组</param>
    public void AddPreset(PresetPack preset)
    {
        if (preset == null) return;
        ((List<PresetPack>)Presets).Add(preset);
    }

    /// <summary>
    /// 获得所有卡牌各 4 张
    /// </summary>
    /// <param name="amount">每种卡牌的目标数量</param>
    public void GainAllCards(int amount = 4)
    {
        if (amount <= 0) return;

        var added = new HashSet<string>();

        foreach (var template in CardModel.AllCards)
        {
            if (template == null) continue;
            if (string.IsNullOrEmpty(template.Title)) continue;
            if (!added.Add(template.Title)) continue;

            int owned = GetAmount(template.Title);
            int need = amount - owned;
            if (need <= 0) continue;

            for (int i = 0; i < need; i++)
                AddCard(template.Clone());
        }
    }

    /// <summary>
    /// 应用一条奖励
    /// </summary>
    /// <param name="reward">奖励</param>
    public void ApplyReward(Reward reward)
    {
        reward?.ApplyTo(this);
    }

    /// <summary>
    /// 应用多条奖励
    /// </summary>
    /// <param name="rewards">奖励</param>
    public void ApplyRewards(IEnumerable<Reward> rewards)
    {
        if (rewards == null) return;
        foreach (var reward in rewards)
            ApplyReward(reward);
    }
}