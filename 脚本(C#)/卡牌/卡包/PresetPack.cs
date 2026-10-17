using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pack;

public class PresetPack : CardPack
{
    /// <summary>
    /// 卡组名称
    /// </summary>
    public virtual string PackName { get; set; }
    protected List<CardModel> cards = new();
    public override IReadOnlyList<CardModel> Cards => cards;
    public void AddCard(CardModel card)
    {
        if (cards.Contains(card)) return;
        cards.Add(card);
    }
    public void RemoveCard(CardModel card)
    {
        cards.Remove(card);
    }
    public void AddCards(params CardModel[] cards)
    {
        foreach (var card in cards)
            AddCard(card);
    }
}
