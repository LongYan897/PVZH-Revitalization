using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Card.String;

/// <summary>
/// 专门为卡牌文本制作的类<br/>
/// <br/>
/// 可以自动从Json文本中提取卡牌基础信息 统一使用 "卡牌名.对应的键" (这里的卡牌名称指的是"卡牌对应类的名称"):<br/>
///     血量[可选]  : 使用 "Hp" 当作Json的键<br/>
///     生命值类型[可选(默认是无任何特殊效果的生命)]  : 使用 "HpType" 当作Json的键 比较复杂 详见HpType.cs<br/>
///     攻击力[可选]  : 使用 "Atk" 当作Json的键<br/>
///     攻击类型[可选(默认是无任何特殊效果的攻击)]  : 使用 "AtkType" 当作Json的键 比较复杂 详见AtkType.cs<br/>
///     费用  : 使用 "Cost" 当作Json的键<br/>
///     名称  : 使用 "Title" 当作Json的键<br/>
///     标签  : 使用 "Label" 当作Json的键 值是列表 [,..]<br/>
///     派系  : 使用 "Category" 当作Json的键<br/>
///     稀有度  : 使用 "Rarity" 当作Json的键<br/>
///     类别 : 使用 "Type" 当作Json的键<br/>
///     阵营 : 使用 "Camp" 当作Json的键<br/>
///     特别描述 : 使用 "Flavor" 当作Json的键<br/>
///     描述  : 使用 "Description" 或 "Des"<br/>
///     <br/>
///     ***<br/>
///         描述比较特别 包含了关键字系统 <br/>
///         文本中使用 _XXX_ 可以实现原版下划线功能 例如召唤<br/>
///         文本中使用 =XXX= 可以实现图标显示 例如致命 必中<br/>
///     ***<br/>
///     <br/>
/// 例如:<br/>
///     豌豆射手 Peashooter<br/>
///     ***Json***<br/>
///     {<br/>
///         "Peashooter.Title":"豌豆射手",<br/>
///         "Peashooter.Flavor": "自 2009 年起便开始打击僵尸…… 至今仍未停止。",<br/>
///         "Peashooter.Hp": 1,<br/>
///         "Peashooter.Atk": 1,<br/>
///         "Peashooter.Cost": 1,<br/>
///         "Peashooter.Rarity": "Basic", (改成中文的"基础"也可以)<br/>
///         "Peashooter.Label": ["豌豆"],<br/>
///         "Peashooter.Category": "MegaGrow" (改成中文的"猛长"也可以)<br/>
///         "Peashooter.Camp": "植物"<br/>
///         "Peashooter.Type": "单位" （关于英雄的可以写成"单位:英雄"）<br/>
///     }<br/>
///     *********
/// </summary>
public class CardString
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Title { get; private set; }
    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; private set; }
    /// <summary>
    /// 血量
    /// </summary>
    public int Health { get; private set; }
    /// <summary>
    /// 攻击力
    /// </summary>
    public int Attack { get; private set; }
    /// <summary>
    /// 费用
    /// </summary>
    public int Cost { get; private set; }
    /// <summary>
    /// 稀有度
    /// </summary>
    public Rarity Rarity { get; private set; }
    /// <summary>
    /// 标签
    /// </summary>
    public List<string> Labels { get; private set; }
    /// <summary>
    /// 派系
    /// </summary>
    public Class Category { get; private set; }
    /// <summary>
    /// 特别描述
    /// </summary>
    public string Flavor { get; private set; }
    /// <summary>
    /// 初始的生命类型
    /// </summary>
    public HpType HpType { get; private set; }
    /// <summary>
    /// 初始的攻击类型
    /// </summary>
    public AtkType AtkType {  get; private set; }
    /// <summary>
    /// 阵营
    /// </summary>
    public Camp Camp {  get; private set; }
    /// <summary>
    /// 卡牌种类
    /// </summary>
    public CardType CardType { get; private set; }
    /// <summary>
    /// 全局的字典用于查找数据
    /// </summary>
    private static Dictionary _cardDict;
    /// <summary>
    /// 从cards.json中加载卡牌文本
    /// </summary>
    /// <param name="cardTitle">卡牌类名，例如 Peashooter</param>
    /// <returns>卡牌文本</returns>
    public static CardString Loading(string cardTitle)
    {
        var cardString = new CardString();
        if (GetData(cardTitle, "Cost").VariantType != Variant.Type.Nil)
        {
            cardString.Cost = GetData(cardTitle, "Cost").AsInt32();
        }
        if (GetData(cardTitle, "Atk").VariantType != Variant.Type.Nil)
        {
            cardString.Attack = GetData(cardTitle, "Atk").AsInt32();
        }
        if (GetData(cardTitle, "Hp").VariantType != Variant.Type.Nil)
        {
            cardString.Health = GetData(cardTitle, "Hp").AsInt32();
        }
        if (GetData(cardTitle, "Title").VariantType != Variant.Type.Nil)
        {
            cardString.Title = GetData(cardTitle, "Title").AsString();
        }
        if (GetData(cardTitle, "Des").VariantType != Variant.Type.Nil)
        {
            cardString.Description = GetData(cardTitle, "Des").AsString();
        }
        if (GetData(cardTitle, "Description").VariantType != Variant.Type.Nil)
        {
            cardString.Description = GetData(cardTitle, "Description").AsString();
        }
        if (GetData(cardTitle, "Flavor").VariantType != Variant.Type.Nil)
        {
            cardString.Flavor = GetData(cardTitle, "Flavor").AsString();
        }
        if (GetData(cardTitle, "Camp").VariantType != Variant.Type.Nil)
        {
            var camp = GetData(cardTitle, "Camp").AsString();
            cardString.Camp = camp switch
            {
                "Zombie" => Camp.Zombie,
                "Plant" => Camp.Plant,
                "僵尸" => Camp.Zombie,
                "植物" => Camp.Plant,
                _ => Camp.Zombie
            };
        }
        if (GetData(cardTitle, "Type").VariantType != Variant.Type.Nil)
        {
            var cardType = GetData(cardTitle, "Type").AsString();
            cardString.CardType = cardType.Split(";")[0] switch
            {
                "Trick" => CardType.Trick,
                "Fighter" => CardType.Fighter,
                "Environment" => CardType.Environment,
                "锦囊" => CardType.Trick,
                "单位" => CardType.Fighter,
                "环境" => CardType.Environment,
                _ => CardType.None
            } | 
            (cardType.Split(";").Count() > 1 ? cardType.Split(";")[1] 
            switch 
            {
                "英雄" => CardType.Hero,
                _ => CardType.None
            } : CardType.None);
        }
        if (GetData(cardTitle, "Label").VariantType != Variant.Type.Nil)
        {
            cardString.Labels = [.. GetData(cardTitle, "Label").AsStringArray()];
        }
        if (GetData(cardTitle, "Rarity").VariantType != Variant.Type.Nil)
        {
            var rarity = GetData(cardTitle, "Rarity").AsString();
            cardString.Rarity = rarity switch
            {
                "Basic" => Rarity.Basic,
                "Common" => Rarity.Common,
                "Uncommon" => Rarity.Uncommon,
                "Rare" => Rarity.Rare,
                "SuperRare" => Rarity.SuperRare,
                "Legend" => Rarity.Legend,
                "Activity" => Rarity.Activity,
                "Token" => Rarity.Token,
                "基础" => Rarity.Basic,
                "普通" => Rarity.Common,
                "罕见" => Rarity.Uncommon,
                "稀有" => Rarity.Rare,
                "超稀有" => Rarity.SuperRare,
                "传说" => Rarity.Legend,
                "活动" => Rarity.Activity,
                "令牌" => Rarity.Token,
                _ => Rarity.None
            };
        }
        if (GetData(cardTitle, "Category").VariantType != Variant.Type.Nil)
        {
            var category = GetData(cardTitle, "Category").AsString();
            cardString.Category = category switch
            {
                "MegaGrow" => Class.MegaGrow,
                "Solar" => Class.Solar,
                "Guardian" => Class.Guardian,
                "Kabloom" => Class.Kabloom,
                "Smarty" => Class.Smarty,
                "Brainy" => Class.Brainy,
                "Crazy" => Class.Crazy,
                "Beastly" => Class.Beastly,
                "Sneaky" => Class.Sneaky,
                "Hearty" => Class.Hearty,
                "猛长" => Class.MegaGrow,
                "光能" => Class.Solar,
                "守卫" => Class.Guardian,
                "爆花" => Class.Kabloom,
                "聪明" => Class.Smarty,
                "有脑" => Class.Brainy,
                "疯狂" => Class.Crazy,
                "野兽" => Class.Beastly,
                "狡猾" => Class.Sneaky,
                "健壮" => Class.Hearty,
                _ => Class.Crazy
            };
        }
        if (GetData(cardTitle, "AtkType").VariantType != Variant.Type.Nil)
        {
            cardString.AtkType = AtkType.Parser(GetData(cardTitle, "AtkType").AsString());
        }
        if (GetData(cardTitle, "HpType").VariantType != Variant.Type.Nil)
        {
            cardString.HpType = HpType.Parser(GetData(cardTitle, "HpType").AsString());
        }
        return cardString;
    }
    /// <summary>
    /// 从cards.json中获得数据
    /// </summary>
    /// <param name="cardTitle">卡牌类名，例如 Peashooter</param>
    /// <param name="key">键，例如 Title、Cost、Atk</param>
    /// <returns></returns>
    public static Variant GetData(string cardTitle, string key)
    {
        if (_cardDict == null)
        {
            using var file = FileAccess.Open("res://数据(C#)/cards.json", FileAccess.ModeFlags.Read);
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

            _cardDict = json.Data.As<Dictionary>();
        }

        return _cardDict.GetValueOrDefault($"{cardTitle}.{key}");
    }
}
