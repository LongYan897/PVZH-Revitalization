using Card;
using Card.Pack;
using System.Collections.Generic;
using System.Linq;
using Variable;

namespace Play;

public class PlayerAsset
{
    /// <summary>
    /// 玩家的幸运值，影响游戏中随机事件的概率
    /// </summary>
    public Variable<double> Lucky { get; private set; } = new Variable<double>("Lucky", 0.0);

    /// <summary>
    /// 玩家的钻石数量
    /// </summary>
    public int Diamond { get; private set; } = 0;

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
    /// 添加一张卡牌
    /// </summary>
    public void AddCard(CardModel card)
    {
        if (card == null) return;
        _cards.Add(card);
    }

    /// <summary>
    /// 添加多张卡牌
    /// </summary>
    public void AddCards(params CardModel[] cards)
    {
        if (cards == null) return;
        foreach (var card in cards)
            AddCard(card);
    }

    /// <summary>
    /// 添加多张卡牌
    /// </summary>
    public void AddCards(IEnumerable<CardModel> cards)
    {
        if (cards == null) return;
        foreach (var card in cards)
            AddCard(card);
    }

    /// <summary>
    /// 移除一张卡牌
    /// </summary>
    public bool RemoveCard(CardModel card)
    {
        return card != null && _cards.Remove(card);
    }

    /// <summary>
    /// 根据卡牌类别获取拥有的数量
    /// </summary>
    public int GetAmount(CardModel card)
    {
        if (card == null) return 0;
        return _cards.Count(c => c == card);
    }

    /// <summary>
    /// 根据卡牌标题获取拥有的数量
    /// </summary>
    public int GetAmount(string title)
    {
        if (string.IsNullOrEmpty(title)) return 0;
        return _cards.Count(c => c.Title == title);
    }

    /// <summary>
    /// 根据卡牌类别（CardType）获取拥有的数量
    /// </summary>
    public int GetAmount(CardType cardType)
    {
        return _cards.Count(c => c.CardType.HasFlag(cardType));
    }

    /// <summary>
    /// 根据派系（Class）获取拥有的数量
    /// </summary>
    public int GetAmount(Class @class)
    {
        return _cards.Count(c => c.Class.HasFlag(@class));
    }

    /// <summary>
    /// 根据稀有度（Rarity）获取拥有的数量
    /// </summary>
    public int GetAmount(Rarity rarity)
    {
        return _cards.Count(c => c.Rarity == rarity);
    }

    /// <summary>
    /// 添加一个卡组
    /// </summary>
    public void AddPreset(PresetPack preset)
    {
        if (preset == null) return;
        ((List<PresetPack>)Presets).Add(preset);
    }
    /// <summary>
    /// 获得所有卡牌各 4 张
    /// </summary>
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
}