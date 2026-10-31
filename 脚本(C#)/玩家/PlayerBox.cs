
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
using static Godot.WebSocketPeer;

namespace Play;

/// <summary>
/// 玩家的战斗状态
/// 非战斗调用会报错
/// </summary>
public class PlayerBox
{
	private Random battleRng;
	public void PrefABox(Player player,HeroModel hero,PresetPack cards)
	{
		battleRng = new();
		Player = player;
		if (!hero.IsIdentity)
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
        var host = PlayerManager.HostPlayer;
        if (host?.Status == null) return;
        var box = host.Status;
        var cards = box.HandCards;
        if (cards == null) return;

        var row1 = new List<CardModel>();
        var row2 = new List<CardModel>();

        int count = cards.Count;

        if (count <= 4)
        {
            row1.AddRange(cards);
        }
        else
        {
            int firstRowCount = (count + 1) / 2;

            for (int i = 0; i < count; i++)
            {
                if (i < firstRowCount)
                    row1.Add(cards[i]);
                else
                    row2.Add(cards[i]);
            }
        }

        _stuffCard(row1, 925.0f);
        _stuffCard(row2, 925.0f + 193f * 0.56f);

        var plantSide = host == PlayerManager.GetPlayer(Camp.Plant);
        var player2 = (plantSide ? PlayerManager.GetPlayer(Camp.Zombie) : PlayerManager.GetPlayer(Camp.Plant));
        if (player2?.Status == null) return;

        var cards2 = player2.Status.HandCards;
        if (cards2 == null) return;

        _stuffCardForEnemy(cards2);
    }
    private static void _stuffCard(List<CardModel> cards, float y)
    {
        const float cardW = 256f * 0.56f;
        const float screenW = 720f;

        var host = PlayerManager.HostPlayer;
        if (host?.Status == null) return;

        var layoutCards = new List<CardModel>();
        foreach (var c in cards)
        {
            if (c == null) continue;
            var node = NodeCard.GetNode(c);
            if (node == null)
            {
                host.Status.InputCard(c);
                continue;
            }
            layoutCards.Add(c);
        }

        int n = layoutCards.Count;
        if (n == 0) return;

        float totalW = n * cardW;
        float startX = (screenW - totalW) / 2f;

        for (int i = 0; i < n; i++)
        {
            var node = NodeCard.GetNode(layoutCards[i]);
            if (node == null) continue;

            var pos = new Vector2(startX + i * cardW, y) - new Vector2(node.Scale.X * 100f, 0);
            node.BindOrigin(pos);
            node.BringToFront();

            _quickScaleTo(node, new Vector2(0.56f, 0.56f));
        }
    }

    private static void _stuffCardForEnemy(IReadOnlyList<CardModel> cards)
    {
        const float cardW = 256f * 0.28f;
        const float minGap = 8f;
        const float baseGap = cardW;

        if (cards == null) return;
        var host = PlayerManager.HostPlayer;
        if (host?.Status == null) return;

        var plantSide = host == PlayerManager.GetPlayer(Camp.Plant);
        var enemy = plantSide ? PlayerManager.GetPlayer(Camp.Zombie) : PlayerManager.GetPlayer(Camp.Plant);
        if (enemy?.Status == null) return;

        var layoutCards = new List<CardModel>();
        foreach (var c in cards)
        {
            if (c == null) continue;
            var node = NodeCard.GetNode(c);
            if (node == null)
            {
                enemy.Status.InputCard(c);
                continue;
            }
            layoutCards.Add(c);
        }

        int n = layoutCards.Count;
        if (n == 0) return;

        float gap = n <= 1 ? baseGap : Mathf.Max(minGap, baseGap / n);

        float startX = -40f;

        for (int i = 0; i < n; i++)
        {
            var node = NodeCard.GetNode(layoutCards[i]);
            if (node == null) continue;

            var pos = new Vector2(startX + i * gap, 15f);
            node.BindOrigin(pos);
            node.BringToFront();

            _quickScaleTo(node, new Vector2(0.5f, 0.5f));
        }
    }
    private static void _quickScaleTo(Godot.Node node, Vector2 targetScale, float duration = 0.08f)
    {
        if (node == null) return;

        var tween = node.CreateTween();
        tween.TweenProperty(node, "scale", targetScale, duration)
             .SetTrans(Tween.TransitionType.Cubic)
             .SetEase(Tween.EaseType.Out);
    }

    private float DrawScale => Player == PlayerManager.HostPlayer ? 0.64f : 0.64f * 0.5f / 0.56f;

    /// <summary>
    /// 播放抽卡动画
    /// </summary>
    public async Task PlayCardAndDraw(int amount = 1)
    {
        var handCards = HandCards;
        if (handCards == null) return;
        var maxAmount = 10 - handCards.Count;
        CardCmd.PushStop();
        try
        {
            if (maxAmount <= 0) return;
            else
            {
                if (maxAmount < amount)
                    amount = maxAmount;
            }
            var cards = DrawCard(amount);
            cards.ForEach(c => c.Status = Player == PlayerManager.HostPlayer ? Status.FaceUp : Status.FaceDown);

            int n = cards.Count;
            if (n == 0) return;

            float scale = DrawScale;
            float cardW = 256f * scale;
            float startX = 360f - (n-1) * cardW / 2f - 256f /2f;
            float baseY = Player == PlayerManager.HostPlayer ? 560.0f : 420.0f;

            var tasks = new List<Task>();
            for (int i = 0; i < n; i++)
            {
                var pos = new Vector2(startX + i * cardW, baseY);
                tasks.Add(NodeCard.DrawACard(cards[i], pos, scaleFactor: scale));
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
    /// 将卡牌塞入手牌。
    /// </summary>
    /// <param name="cards">要塞入手牌的卡牌</param>
    public async Task PlayCardAndReturn(params CardModel[] cards)
    {
        if (cards == null) return;
        var validCards = cards.Where(c => c != null).ToArray();
        if (validCards.Length == 0) return;

        CardCmd.PushStop();
        try
        {
            int n = validCards.Length;
            float scale = DrawScale;
            float cardW = 256f * scale;
            float startX = 360f - (n-1) * cardW / 2f - 256f / 2f;
            float baseY = Player == PlayerManager.HostPlayer ? 560.0f : 420.0f;

            var tasks = new List<Task>();
            for (int i = 0; i < n; i++)
            {
                var card = validCards[i];
                if (_deckCards.Cards.Contains(card))
                    DeckRemoveCard(card);
                if (!_handCards.Cards.Contains(card))
                    HandAddCard(card);
                card.Status = Player == PlayerManager.HostPlayer ? Status.FaceUp : Status.FaceDown;

                var pos = new Vector2(startX + i * cardW, baseY);
                tasks.Add(NodeCard.DrawACard(card, pos, scaleFactor: scale));
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
		int index = battleRng.Next(unDrawed.Count);
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
		CardCmd.PushStop();
		try
		{
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
