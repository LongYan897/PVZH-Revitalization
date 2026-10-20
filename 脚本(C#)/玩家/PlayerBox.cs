
using Battle;
using Card;
using Card.Cmd;
using Card.Pack;
using Godot;
using Hero;
using Hero.Model;
using Logger;
using Phrases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        _handCards = new();
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
    public IEnumerable<CardModel> UnDrawedCards => DeckCards?.Where(c => HandCards != null && !HandCards.Contains(c)) ?? new List<CardModel>();
    /// <summary>
    /// 整理手牌
    /// </summary>
    public static void StuffCard()
    {
        var box = PlayerManager.HostPlayer.Status;
        var cards = box.HandCards;
        var row1 = new List<CardModel>();
        var row2 = new List<CardModel>();

        if (cards.Count <= 10)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (i < 5) row1.Add(cards[i]);
                else row2.Add(cards[i]);
            }
        }
        else
        {
            for (int i = 0; i < cards.Count; i++)
            {
                int order = i + 1;
                if (order % 2 == 1) row1.Add(cards[i]);
                else row2.Add(cards[i]);
            }
        }

        _stuffCard(row1, 925.0f);
        _stuffCard(row2, 925.0f + 193f * 0.56f);

        var plantSide = PlayerManager.HostPlayer == PlayerManager.GetPlayer(Camp.Plant);
        var player2 = (plantSide ? PlayerManager.GetPlayer(Camp.Zombie) : PlayerManager.GetPlayer(Camp.Plant));
        var box2 = player2.Status;
        var cards2 = box2.HandCards;

        _stuffCardForEnemy(cards2);
    }
    private static void _stuffCard(List<CardModel> cards, float y)
    {
        const float cardW = 256f * 0.56f;
        const float screenW = 720f;

        int n = cards.Count;
        if (n == 0) return;

        float totalW = n * cardW;
        float startX = (screenW - totalW) / 2f;

        for (int i = 0; i < n; i++)
        {
            var node = NodeCard.GetNode(cards[i]);
            node.Scale = new Vector2(0.56f, 0.56f);
            var pos = new Vector2(startX + i * cardW, y) - new Vector2(56f,0);
            node.BindOrigin(pos);
            node.BringToFront();
        }
    }
    private static void _stuffCardForEnemy(IReadOnlyList<CardModel> cards)
    {
        const float cardW = 256f * 0.28f;
        const float minGap = 8f;
        const float baseGap = cardW;

        int n = cards.Count;
        if (n == 0) return;

        float gap = n <= 1 ? baseGap : Mathf.Max(minGap, baseGap / n);

        float startX = -40f;

        for (int i = 0; i < n; i++)
        {
            var node = NodeCard.GetNode(cards[i]);
            node.Scale = new Vector2(0.5f, 0.5f);
            var pos = new Vector2(startX + i * gap, 15f);
            node.BindOrigin(pos);
            node.BringToFront();
        }
    }
    /// <summary>
    /// 播放抽卡动画
    /// </summary>
    public async Task PlayCardAndDraw(int amount = 1)
    {
        try
        {
            var maxAmount = 10 - HandCards.Count;
            CardCmd.PushStop();
            if (maxAmount <= 0) return;
            else
            {
                if (maxAmount < amount)
                    amount = maxAmount;
            }
            var cards = DrawCard(amount);
            cards.ForEach(c => c.Status = Player == PlayerManager.HostPlayer ? Status.FaceUp : Status.FaceDown);
            const float cardW = 256f * 0.56f;
            const float screenW = 720f;

            int n = cards.Count;
            if (n == 0) return;

            float totalW = n * cardW;
            float startX = (screenW - totalW) / 2f - 256f * 0.28f;
            float baseY = Player == PlayerManager.HostPlayer ? 560.0f : 420.0f;

            var tasks = new List<Task>();
            for (int i = 0; i < n; i++)
            {
                var pos = new Vector2(startX + i * cardW, baseY);
                tasks.Add(NodeCard.DrawACard(cards[i], pos));
            }
            await Task.WhenAll(tasks);
            StuffCard();
        }
        finally
        {
            CardCmd.PopStop();
        }
    }
    /// <summary>
    /// 抽出一张卡牌
    /// </summary>
    /// <returns></returns>
    public CardModel DrawCard()
    {
        var unDrawed = UnDrawedCards.ToList();
        if (unDrawed.Count == 0)
        {
            return null;
        }
        Random random = new();
        int index = random.Next(unDrawed.Count);
        var card = unDrawed[index];
        DeckRemoveCard(card);
        HandAddCard(card);
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
        cards.RemoveAll(c => c == null);
        return cards;
    }
    /// <summary>
    /// 返回手牌
    /// </summary>
    public async Task ReturnHand(CardModel card,Vector2 posStart)
    {
        try
        {
            CardCmd.PushStop();
            HandAddCard(card);
            card.Status = Player == PlayerManager.HostPlayer ? Status.FaceUp : Status.FaceDown;
            await NodeCard.DisplayCard(card, posStart);
            StuffCard();
        }
        finally
        {
            CardCmd.PopStop();
        }
    }
    /// <summary>
    /// 把卡牌塞回卡组
    /// </summary>
    /// <param name="card"></param>
    public void InputCard(CardModel card)
    {
        DeckAddCard(card);
        HandRemoveCard(card);
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
