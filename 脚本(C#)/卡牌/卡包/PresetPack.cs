using Card.Pack.String;
using Godot;
using Hero.Model;
using Pack;
using Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Card.Pack;

public class PresetPack : CardPack
{
    public enum DeckRarity
    {
        /// <summary>正常卡包</summary>
        None,
        /// <summary>初学者卡包</summary>
        Init,
        /// <summary>熟练卡包</summary>
        Adept,
        /// <summary>精通卡包</summary>
        Expert,
        /// <summary>宇宙卡包</summary>
        Universe,
        /// <summary>原始卡包</summary>
        Primordial
    }

    /// <summary>
    /// 按卡组名称（decks.json 的 Name）索引的模板
    /// </summary>
    private static readonly Dictionary<string, PresetPack> TemplateEntries = new();
    /// <summary>
    /// 按卡组类名（decks.json 键前缀，如 AllPea）索引的模板
    /// </summary>
    private static readonly Dictionary<string, PresetPack> ClassEntries = new();

    /// <summary>
    /// 卡组名称
    /// </summary>
    public virtual string PackName { get; set; }

    /// <summary>
    /// 卡组描述
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// 卡组绑定的英雄
    /// </summary>
    public virtual HeroModel Hero { get; set; }

    /// <summary>
    /// 已经拥有的卡牌数量
    /// </summary>
    public int LockedCard
    {
        get
        {
            if (this is DIYPack)
                return 0;
            else
            {
                var hash = Cards.Select(c => c.Title);
                var haSet = hash.ToHashSet();
                return haSet.Sum(c=>Math.Min(PlayerManager.HostPlayer.Asset.GetAmount(c),hash.Count(c1=>c1 == c)));
            }
        }
    }
    /// <summary>
    /// 是否已经获取
    /// </summary>
    public virtual bool Gained => LockedCard == 40;

    /// <summary>
    /// 特殊的卡面
    /// </summary>
    public virtual string CustomFrame { get; set; }

    /// <summary>
    /// 卡组的稀有度
    /// </summary>
    protected DeckRarity Rarity { get; private set; } = DeckRarity.None;

    /// <summary>
    /// 卡牌
    /// </summary>
    protected readonly HashSet<CardModel> cards = new();

    private bool _filled;

    /// <summary>
    /// 子类覆写此属性提供初始填充内容。
    /// 不要在这里返回共享实例；每次访问应返回新的模板列表。
    /// </summary>
    protected virtual IReadOnlyList<CardModel> DeckCards { get; }

    /// <summary>
    /// 包含的卡牌。
    /// </summary>
    public sealed override IReadOnlyList<CardModel> Cards
    {
        get
        {
            EnsureFilled();
            return [..cards];
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
            if (CountByTitle(card.Title) >= 4) continue;
            cards.Add(card.Clone());
        }
    }

    private int CountByTitle(string title)
    {
        if (string.IsNullOrEmpty(title)) return 0;
        int n = 0;
        foreach (var c in cards)
            if (c != null && c.Title == title) n++;
        return n;
    }

    public void AddCard(CardModel card)
    {
        EnsureFilled();
        if (card == null) return;
        if (CountByTitle(card.Title) >= 4) return;
        cards.Add(card);
    }

    public void RemoveCard(CardModel card)
    {
        EnsureFilled();
        cards.Remove(card);
    }

    public void AddCards(params CardModel[] cards)
    {
        if (cards == null) return;
        foreach (var card in cards)
            AddCard(card);
    }

    public void AddCards(IEnumerable<CardModel> cards)
    {
        if (cards == null) return;
        foreach (var card in cards)
            AddCard(card);
    }

    /// <summary>
    /// 加载卡组
    /// </summary>
    /// <typeparam name="T">卡组类</typeparam>
    public static T Load<T>() where T : PresetPack, new()
    {
        return Load<T>(DeckString.Loading(typeof(T).Name));
    }

    /// <summary>
    /// 从 DeckString 加载卡组数据
    /// </summary>
    private static T Load<T>(DeckString deckString) where T : PresetPack, new()
    {
        var pack = new T
        {
            PackName = deckString.Name,
            Description = deckString.Description,
            Hero = deckString.Hero,
            CustomFrame = deckString.Frame,
            Rarity = deckString.Rarity
        };

        if (deckString.Cards != null && deckString.Cards.Count > 0)
        {
            pack._filled = true;
            foreach (var card in deckString.Cards)
                pack.AddCard(card);
        }

        return pack;
    }

    /// <summary>
    /// 按卡组名称获取卡组模板
    /// </summary>
    public static PresetPack GetTemplate(string name)
    {
        if (name != null && TemplateEntries.TryGetValue(name, out var template))
            return template;
        return null;
    }

    /// <summary>
    /// 按卡组类名（decks.json 键前缀，如 AllPea）获取卡组模板
    /// </summary>
    public static PresetPack GetTemplateByClassName(string className)
    {
        if (!string.IsNullOrEmpty(className) && ClassEntries.TryGetValue(className, out var template))
            return template;
        return null;
    }

    /// <summary>
    /// 所有已加载的卡组模板
    /// </summary>
    public static IReadOnlyCollection<PresetPack> AllPacks => TemplateEntries.Values;

    /// <summary>
    /// 初始化所有卡组模板
    /// </summary>
    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var type in assembly.GetTypes())
        {
            if (!typeof(PresetPack).IsAssignableFrom(type)) continue;
            if (type.IsAbstract) continue;
            if (type == typeof(PresetPack)) continue;
            if (type.GetConstructor(Type.EmptyTypes) == null) continue;

            try
            {
                var pack = (PresetPack)Activator.CreateInstance(type);
                var deckString = DeckString.Loading(type.Name);
                pack.ApplyString(deckString);

                string key = !string.IsNullOrEmpty(pack.PackName) ? pack.PackName : type.Name;
                TemplateEntries[key] = pack;
                ClassEntries[type.Name] = pack;
            }
            catch (Exception ex)
            {
                GD.PushError($"PresetPack.Init 加载 {type.Name} 失败: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// 把 DeckString 的数据写入当前卡组
    /// </summary>
    private void ApplyString(DeckString deckString)
    {
        if (deckString == null) return;

        if (!string.IsNullOrEmpty(deckString.Name))
            PackName = deckString.Name;
        if (!string.IsNullOrEmpty(deckString.Description))
            Description = deckString.Description;
        if (deckString.Hero != null)
            Hero = deckString.Hero;
        if (!string.IsNullOrEmpty(deckString.Frame))
            CustomFrame = deckString.Frame;

        Rarity = deckString.Rarity;

        if (deckString.Cards != null && deckString.Cards.Count > 0)
        {
            _filled = true;
            cards.Clear();
            foreach (var card in deckString.Cards)
                AddCard(card);
        }
    }
}