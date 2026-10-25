using Godot;
using Hero.Model;
using Pack;
using System;
using System.Collections.Generic;

namespace Card.Pack;

public class PresetPack : CardPack
{
    protected enum DeckRarity
    {
        /// <summary>
        /// 正常卡包
        /// </summary>
        None,
        /// <summary>
        /// 初学者卡包
        /// </summary>
        Init,
        /// <summary>
        /// 熟练卡包
        /// </summary>
        Adept,
        /// <summary>
        /// 精通卡包
        /// </summary>
        Expert,
        /// <summary>
        /// 宇宙卡包
        /// </summary>
        Universe,
        /// <summary>
        /// 原始卡包
        /// </summary>
        Primordial
    }
    /// <summary>
    /// 卡组名称
    /// </summary>
    public virtual string PackName { get; set; }

    /// <summary>
    /// 卡组绑定的英雄
    /// </summary>
    public virtual HeroModel Hero { get; set; }

    /// <summary>
    /// 是否已经获取
    /// </summary>
    public virtual bool Gained {  get; set; }

    /// <summary>
    /// 特殊的卡面
    /// </summary>
    public virtual string CustomFrame {  get; set; }
    /// <summary>
    /// 卡组的稀有度
    /// </summary>
    protected virtual DeckRarity Rarity { get; } = PresetPack.DeckRarity.None;
    /// <summary>
    /// 卡牌
    /// </summary>
    protected readonly List<CardModel> cards = new();

    private bool _filled;

    /// <summary>
    /// 子类覆写此属性提供初始填充内容。
    /// 不要在这里返回共享实例；每次访问应返回新的模板列表。
    /// </summary>
    protected virtual IReadOnlyList<CardModel> DeckCards { get;}

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

    public Texture2D GetBg()
    {
        return (Rarity switch
        {
            DeckRarity.Init => "res://素材(C#)/卡包/卡套/deckRecipeR1Background.png",
            DeckRarity.Adept => "res://素材(C#)/卡包/卡套/deckRecipeR2Background.png",
            DeckRarity.Expert => "res://素材(C#)/卡包/卡套/deckRecipeR3Background.png",
            DeckRarity.Universe => "res://素材(C#)/卡包/卡套/deckRecipeRSpaceBackground.png",
            DeckRarity.Primordial => "res://素材(C#)/卡包/卡套/deckRecipeSet3Background.png",
            _ => "res://素材(C#)/卡包/卡套/deckCustomBackground.png"
        }).Load<Texture2D>();
    }
    public Texture2D GetFrame()
    {
        return (Rarity switch
        {
            DeckRarity.Init => "res://素材(C#)/卡包/卡套/deckRecipeR1Sash.png",
            DeckRarity.Adept => "res://素材(C#)/卡包/卡套/deckRecipeR2Sash.png",
            DeckRarity.Expert => "res://素材(C#)/卡包/卡套/deckRecipeR3Sash.png",
            DeckRarity.Universe => "res://素材(C#)/卡包/卡套/deckRecipeRSpaceSash.png",
            DeckRarity.Primordial => "res://素材(C#)/卡包/卡套/deckRecipeSet3Sash.png",
            _ => "res://素材(C#)/卡包/卡套/deckCustomSashGrayscale.png"
        }).Load<Texture2D>();
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