
using Card;
using Hero.Model;
using Logger;
using Phrases;
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
    /// <summary>
    /// 持有该状态的玩家
    /// </summary>
    public Player Player { get; private set; }
    /// <summary>
    /// 局内卡组内的卡牌
    /// </summary>
    private readonly List<Variable<CardModel>> _deckCards = [];
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
            return [.. _deckCards.Select(v => v.Current)];
        }
    }
    /// <summary>
    /// 局内手牌的卡牌
    /// </summary>
    private readonly List<Variable<CardModel>> _handCards = [];
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
            return [.. _handCards.Select(v => v.Current)];
        }
    }
    /// <summary>
    /// 操纵的英雄
    /// </summary>
    public HeroModel Hero { get; private set; }
    /// <summary>
    /// 往战斗中加入卡牌
    /// </summary>
    /// <param name="card">加入的卡牌</param>
    public void DeckAddCard(CardModel card)
    {
        if (_deckCards.Any(v => v.Current == card)) return;
        _deckCards.Add(new("Card", card));
    }
    /// <summary>
    /// 从战斗中移除卡牌
    /// </summary>
    /// <param name="card">移除的卡牌</param>
    public void DeckRemoveCard(CardModel card)
    {
        _deckCards.RemoveAll(v => v.Current == card);
    }
    /// <summary>
    /// 往手牌中加入卡牌
    /// </summary>
    /// <param name="card">加入的卡牌</param>
    public void HandAddCard(CardModel card)
    {
        if (_handCards.Any(v => v.Current == card)) return;
        _handCards.Add(new("Card", card));
    }
    /// <summary>
    /// 从手牌中移除卡牌
    /// </summary>
    /// <param name="card">移除的卡牌</param>
    public void HandRemoveCard(CardModel card)
    {
        if (_handCards.Any(v => v.Current == card)) return;
        _handCards.Add(new("Card", card));
    }
}
