
using Battle;
using Card;
using Card.Pack;
using Hero;
using Hero.Model;
using Logger;
using Phrases;
using System;
using System.Collections.Generic;
using System.Linq;
using Variable;

namespace Play;

/// <summary>
/// 玩家的战斗状态
/// 非战斗调用会报错
/// </summary>
public class PlayerBox
{
    public void PrefABox(Player player,HeroModel hero,PresetPack cards)
    {
        Player = player;
        Hero = hero;
        _deckCards = cards;
    }
    /// <summary>
    /// 持有该状态的玩家
    /// </summary>
    public Player Player { get; private set; }
    /// <summary>
    /// 局内卡组内的卡牌
    /// </summary>
    private PresetPack _deckCards = new();
    /// <summary>
    /// 局内卡牌
    /// </summary>
    public IReadOnlyList<CardModel> DeckCards
    {
        get
        {
            if (!PhraseManager.IsInBattle)
            {
                Log.Error("在非战斗场景尝试获取战斗卡组的卡牌");
                return null;
            }
            return _deckCards.Cards;
        }
    }
    /// <summary>
    /// 局内手牌的卡牌
    /// </summary>
    private PresetPack _handCards = new();
    /// <summary>
    /// 局内手牌
    /// </summary>
    public IReadOnlyList<CardModel> HandCards
    {
        get
        {
            if (!PhraseManager.IsInBattle)
            {
                Log.Error("在非战斗场景尝试获取战斗卡组的卡牌");
                return null;
            }
            return _handCards.Cards;
        }
    }
    /// <summary>
    /// 未抽出的卡牌或者已经抽出的卡牌
    /// </summary>
    public IEnumerable<CardModel> UnDrawedCards => DeckCards.Where(c => !HandCards.Contains(c));
    /// <summary>
    /// 抽出一张卡牌
    /// </summary>
    /// <returns></returns>
    public CardModel DrawCard()
    {
        Random random = new();
        int index = random.Next(UnDrawedCards.Count());
        var card = UnDrawedCards.ToList()[index];
        DeckRemoveCard(card);
        return card;
    }
    /// <summary>
    /// 抽出卡牌
    /// </summary>
    /// <param name="amount">抽出的数量</param>
    /// <returns></returns>
    public List<CardModel> DrawCard(int amount)
    {
        var cards = new List<CardModel>();
        for (int i = 0;i < amount;i++)
        {
            cards.Add(DrawCard());
        }
        return cards;
    }
    /// <summary>
    /// 把卡牌塞回卡组
    /// </summary>
    /// <param name="card"></param>
    public void InputCard(CardModel card)
    {
        DeckAddCard(card);
    }
    /// <summary>
    /// 把卡牌塞回卡组
    /// </summary>
    /// <param name="card"></param>
    public void InputCards(params CardModel[] cards)
    {
        foreach (var card in cards)
            DeckAddCard(card);
    }
    /// <summary>
    /// 把卡牌塞回卡组
    /// </summary>
    /// <param name="card"></param>
    public void InputCards(params List<CardModel>[] cardses)
    {
        foreach (var cards in cardses)
            InputCards(cards.ToArray());
    }
    /// <summary>
    /// 操纵的英雄
    /// </summary>
    public HeroModel Hero { get; private set; }
    public IAttackable AtkMe() => HeroInstance.GetNode(Hero);
    /// <summary>
    /// 往卡组中加入卡牌
    /// </summary>
    /// <param name="card">加入的卡牌</param>
    public void DeckAddCard(CardModel card)
    {
        _deckCards.AddCard(card);
    }
    /// <summary>
    /// 从卡组中移除卡牌
    /// </summary>
    /// <param name="card">移除的卡牌</param>
    public void DeckRemoveCard(CardModel card)
    {
        _deckCards.RemoveCard(card);
    }
    /// <summary>
    /// 往手牌中加入卡牌
    /// </summary>
    /// <param name="card">加入的卡牌</param>
    public void HandAddCard(CardModel card)
    {
        _handCards.AddCard(card);
    }
    /// <summary>
    /// 从手牌中移除卡牌
    /// </summary>
    /// <param name="card">移除的卡牌</param>
    public void HandRemoveCard(CardModel card)
    {
        _handCards.RemoveCard(card);
    }
}
