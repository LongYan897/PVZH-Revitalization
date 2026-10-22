using System;
using System.Collections.Generic;

namespace Card.Pack;

public class PresetPack : CardPack
{
    /// <summary>
    /// 卡组名称
    /// </summary>
    public virtual string PackName { get; set; }

    /// <summary>
    /// 卡牌
    /// </summary>
    protected readonly List<CardModel> cards = new();

    private bool _filled;

    /// <summary>
    /// 子类覆写此属性提供初始填充内容。
    /// 不要在这里返回共享实例；每次访问应返回新的模板列表。
    /// </summary>
    protected virtual IReadOnlyList<CardModel> DeckCards => Array.Empty<CardModel>();

    /// <summary>
    /// 包含的卡牌。
    /// </summary>
    public sealed override IReadOnlyList<CardModel> Cards
    {
        get
        {
            EnsureFilled();
            return cards;
        }
    }

    private void EnsureFilled()
    {
        if (_filled) return;
        _filled = true;

        var template = DeckCards;
        if (template == null) return;

        foreach (var card in template)
        {
            if (card == null) continue;
            if (cards.Contains(card)) continue;
            cards.Add(card);
        }
    }

    public void AddCard(CardModel card)
    {
        EnsureFilled();
        if (cards.Contains(card)) return;
        cards.Add(card);
    }

    public void RemoveCard(CardModel card)
    {
        EnsureFilled();
        cards.Remove(card);
    }

    public void AddCards(params CardModel[] cards)
    {
        foreach (var card in cards)
            AddCard(card);
    }
}