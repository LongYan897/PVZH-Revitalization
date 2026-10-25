using Card.String;
using Godot;
using Godot.Collections;
using Hero.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Card.Pack.PresetPack;

namespace Card.Pack.String;

/// <summary>
/// 专门为卡组文本制作的类<br/>
/// <br/>
/// 可以自动从Json文本中提取卡组基础信息 统一使用 "卡组名.对应的键" (这里的卡组名称指的是"卡组对应类的名称"):<br/>
///     名称 : 使用 "Name" 当作Json的键<br/>
///     描述 : 使用 "Description" 或 "Des" 当作Json的键(不能超过88个字)<br/>
///     英雄 : 使用 "Hero" 当作Json的键 这里用的是英雄的中文名<br/>
///     品级 : 使用 "Rarity" 当作Json的键<br/>
///     封皮 : 使用 "Frame" 当作Json的键<br/>
///     卡牌 : 使用 "Cards" 当作Json的键 值是列表 [{"豌豆射手":10},..] 需要用对应的名称或者Id和数量 (比如豌豆射手)<br/>
/// </summary>
public class DeckString
{
    /// <summary>
    /// 官方卡组的名称
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// 官方卡组的描述
    /// </summary>
    public string Description { get; private set; }
    /// <summary>
    /// 官方卡组的卡牌
    /// </summary>
    public List<CardModel> Cards { get; private set; }
    /// <summary>
    /// 官方卡组对应的英雄
    /// </summary>
    public HeroModel Hero { get; private set; }
    /// <summary>
    /// 官方卡组的品级
    /// </summary>
    public DeckRarity Rarity { get; private set; }
    /// <summary>
    /// 官方卡组的封皮
    /// </summary>
    public string Frame { get; private set; }
    /// <summary>
    /// 全局的字典用于查找数据
    /// </summary>
    private static Dictionary _deckDict;
    /// <summary>
    /// 从decks.json中加载官方卡组文本
    /// </summary>
    /// <param name="deckTitle">卡组类名，例如 AllPea</param>
    /// <returns>卡组文本</returns>
    public static DeckString Loading(string deckTitle)
    {
        var deckString = new DeckString();

        if (GetData(deckTitle, "Name").VariantType != Variant.Type.Nil)
        {
            deckString.Name = GetData(deckTitle, "Name").AsString();
        }
        if (GetData(deckTitle, "Des").VariantType != Variant.Type.Nil)
        {
            deckString.Description = GetData(deckTitle, "Des").AsString();
        }
        if (GetData(deckTitle, "Description").VariantType != Variant.Type.Nil)
        {
            deckString.Description = GetData(deckTitle, "Description").AsString();
        }
        if (GetData(deckTitle, "Frame").VariantType != Variant.Type.Nil)
        {
            deckString.Frame = GetData(deckTitle, "Frame").AsString();
        }
        if (GetData(deckTitle, "Hero").VariantType != Variant.Type.Nil)
        {
            deckString.Hero = HeroModel.Identity(GetData(deckTitle, "Hero").AsString());
        }
        if (GetData(deckTitle, "Rarity").VariantType != Variant.Type.Nil)
        {
            var rarity = GetData(deckTitle, "Rarity").AsString();
            deckString.Rarity = rarity switch
            {
                "None" => DeckRarity.None,
                "Init" => DeckRarity.Init,
                "Adept" => DeckRarity.Adept,
                "Expert" => DeckRarity.Expert,
                "Universe" => DeckRarity.Universe,
                "Primordial" => DeckRarity.Primordial,
                "正常" => DeckRarity.None,
                "初学者" => DeckRarity.Init,
                "熟练" => DeckRarity.Adept,
                "精通" => DeckRarity.Expert,
                "宇宙" => DeckRarity.Universe,
                "原始" => DeckRarity.Primordial,
                _ => DeckRarity.None
            };
        }
        if (GetData(deckTitle, "Cards").VariantType != Variant.Type.Nil)
        {
            deckString.Cards = new List<CardModel>();
            foreach (var item in GetData(deckTitle, "Cards").AsGodotArray())
            {
                var cardDict = item.AsGodotDictionary();
                foreach (var kv in cardDict)
                {
                    string cardName = kv.Key.AsString();
                    int count = kv.Value.AsInt32();

                    CardModel card = CardModel.GetTemplate(cardName)
                                  ?? CardModel.GetTemplateByClassName(cardName);
                    if (card == null) continue;

                    for (int i = 0; i < count; i++)
                        deckString.Cards.Add(card.Clone());
                }
            }
        }

        return deckString;
    }
    /// <summary>
    /// 从cards.json中获得数据
    /// </summary>
    /// <param name="deckTitle">卡组类名，例如 AllPea</param>
    /// <param name="key">键</param>
    /// <returns></returns>
    public static Variant GetData(string deckTitle, string key)
    {
        if (_deckDict == null)
        {
            using var file = FileAccess.Open("res://数据(C#)/decks.json", FileAccess.ModeFlags.Read);
            if (file == null)
            {
                return default;
            }

            var json = new Json();
            Error err = json.Parse(file.GetAsText());
            if (err != Error.Ok)
            {
                return default;
            }

            _deckDict = json.Data.As<Dictionary>();
        }

        return _deckDict.GetValueOrDefault($"{deckTitle}.{key}");
    }
}
