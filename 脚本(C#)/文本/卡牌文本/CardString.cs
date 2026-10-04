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
///     等级类型[可选(默认是无任何特殊效果的攻击)]  : 使用 "StarType" 当作Json的键 比较复杂 详见StarType.cs<br/>
///     子弹[可选]  : 使用 "Ammo" 当作Json的键<br/>
///     费用  : 使用 "Cost" 当作Json的键<br/>
///     名称  : 使用 "Title" 当作Json的键<br/>
///     标签  : 使用 "Label" 当作Json的键 值是列表 [,..]<br/>
///     关键词  : 使用 "Tag" 当作Json的键 值是列表 [,..]<br/>
///     派系  : 使用 "Category" 当作Json的键<br/>
///     稀有度  : 使用 "Rarity" 当作Json的键<br/>
///     类别 : 使用 "Type" 当作Json的键<br/>
///     卡包[可选] : 使用 "Pack" 当作Json的键 如果是基础卡默认是 "基础-常见"<br/>
///     阵营 : 使用 "Camp" 当作Json的键<br/>
///     特别描述 : 使用 "Flavor" 当作Json的键<br/>
///     骨骼 : 使用 "Animation" 当作Json的键<br/>
///     骨骼场景 : 使用 "Scene" 当作Json的键 需要tscn的场景 该场景内部使用相对位置确定动画位置<br/>
///     <br/>
///     ***<br/>
///     Marker2D *Collection(Marker2D) 代表图鉴中动画位置 Collection2(SpineSprite)是土坑位置 Marker2D *Battle(Marker2D)  代表战斗中动画位置 Battle2(SpineSprite)是土坑位置 <br/>
///     如果不是单位 *Collection(Marker2D) 代表图鉴中动画位置 Collection2(Marker2D)是原点位置<br/>
///     加载场景时会销毁所有名称不带Saved的节点<br/>
///     ***<br/>
///     <br/>
///     卡面 : 使用 "Icon" 当作Json的键 需要tscn的场景<br/>
///     描述  : 使用 "Description" 或 "Des"<br/>
///     <br/>
///     ***<br/>
///         描述比较特别 包含了关键字系统 <br/>
///         文本中使用 _XXX_ 可以实现原版下划线功能 例如召唤<br/>
///         文本中使用 =XXX= 可以实现图标显示 例如致命 必中<br/>
///         文本中使用 =XXX={N} 可以实现带数字的图标显示 例如 =生命值={2} 获得+2(生命图标)
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
///         "Peashooter.Type": "单位" （关于英雄的可以写成"单位;英雄"）<br/>
///         "Peashooter.AtkType": "普通",<br/>
///         "Peashooter.HpType": "普通"<br/>
///         "Peashooter.HpType": "星星"<br/>
///         "Peashooter.Icon": "res://素材(C#)/卡牌/豌豆射手/Pea.png"<br/>
///         "Peashooter.Animation": "res://素材(C#)/卡牌/豌豆射手/豌豆射手植物动画.tres"<br/>
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
    /// 卡包
    /// </summary>
    public string Pack { get; private set; }

    /// <summary>
    /// 动画路径
    /// </summary>
    public string AnimationPath { get; private set; }
    /// <summary>
    /// 动画场景路径
    /// </summary>
    public string AnimationScenePath { get; private set; }
    /// <summary>
    /// 卡面路径
    /// </summary>
    public string IconPath { get; private set; }
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
    /// 卡牌关键词
    /// </summary>
    public CardTag CardTag { get; private set; }
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
    public AtkType AtkType { get; private set; }
    /// <summary>
    /// 等级类型
    /// </summary>
    public StarType StarType { get; private set; }
    /// <summary>
    /// 阵营
    /// </summary>
    public Camp Camp { get; private set; }
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
        if (GetData(cardTitle, "Pack").VariantType != Variant.Type.Nil)
        {
            cardString.Pack = GetData(cardTitle, "Pack").AsString() + "-";
        }
        if (GetData(cardTitle, "Animation").VariantType != Variant.Type.Nil)
        {
            cardString.AnimationPath = GetData(cardTitle, "Animation").AsString();
        }
        if (GetData(cardTitle, "Icon").VariantType != Variant.Type.Nil)
        {
            cardString.IconPath = GetData(cardTitle, "Icon").AsString();
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
        if (GetData(cardTitle, "Tag").VariantType != Variant.Type.Nil)
        {
            CardTag tag = CardTag.None;
            if (GetData(cardTitle, "Tag").AsStringArray().Contains("Coop") || GetData(cardTitle, "Tag").AsStringArray().Contains("组队"))
            {
                tag |= CardTag.Coop;
            }
            if (GetData(cardTitle, "Tag").AsStringArray().Contains("Amphibious") || GetData(cardTitle, "Tag").AsStringArray().Contains("两栖"))
            {
                tag |= CardTag.Amphibious;
            }
            cardString.CardTag = tag;
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
        if (GetData(cardTitle, "StarType").VariantType != Variant.Type.Nil)
        {
            cardString.StarType = StarType.Parser(GetData(cardTitle, "StarType").AsString());
        }
        if (GetData(cardTitle, "Scene").VariantType != Variant.Type.Nil)
        {
            cardString.AnimationScenePath = GetData(cardTitle, "Scene").AsString();
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
