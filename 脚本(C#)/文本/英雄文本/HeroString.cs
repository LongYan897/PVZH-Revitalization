using Card;
using Card.String;
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hero.String;

/// <summary>
/// 专门为英雄文本制作的类<br/>
/// <br/>
/// 可以自动从Json文本中提取英雄基础信息 统一使用 "英雄名.对应的键" (这里的英雄名称指的是"英雄对应类的名称"):<br/>
///     名称  : 使用 "Title" 当作Json的键<br/>
///     派系  : 使用 "Category" 当作Json的键，必须写为含两个派系列表形式 []<br/>
///     阵营 : 使用 "Camp" 当作Json的键<br/>
///     特别描述 : 使用 "Flavor" 当作Json的键<br/>
///     图鉴缩小倍率 : 使用 "DesScale" 当作Json的键<br/>
///     图鉴Y偏移量 : 使用 "DesOffset" 当作Json的键<br/>
/// 例如：<br/>
///"Penelopea.Title": "绿影侠",<br/>
///"Penelopea.Flavor": "冷知识：当脱下斗篷和面具、回归寻常生活的时候，她用的名字是\"佩妮豆\"",<br/>
///"Penelopea.Category": [ "猛长", "聪明" ],<br/>
///"Penelopea.Camp": "植物",<br/>
///"Penelopea.SuperPower" :  ["GreenShadow", "Freeze", "Tornado", "Embiggen"]<br/>
/// </summary>
public class HeroString
{
    ///<summary>
    ///名称
    ///</summary>
    public string Title { get; private set; }
    ///<summary>
    ///特别描述
    ///</summary>
    public string Flavor { get; private set; }
    ///<summary>
    ///派系
    ///</summary>
    public List<Class> Category { get; private set; }
    ///<summary>
    ///阵营
    ///</summary>
    public Camp Camp { get; private set; }
    /// <summary>
    /// 全局的字典用于查找数据
    /// </summary>
    private static Dictionary _heroDict;
    /// <summary>
    /// 动画路径
    /// </summary>
    public string AnimationPath { get; private set; }
    /// <summary>
    /// 卡面路径
    /// </summary>
    public string IconPath { get; private set; }
    /// <summary>
    /// 图鉴缩小倍率
    /// </summary>
    public float DesScale { get; private set; }
    /// <summary>
    /// 图鉴Y偏移量
    /// </summary>
    public float DesOffset { get; private set; }
    /// <summary>
    /// 从heroes.json中获得数据
    /// </summary>
    /// <param name="heroTitle">英雄类名，例如 Penelopea</param>
    /// <param name="key">键，例如 Title、Category</param>
    /// <returns></returns>
    public static Variant GetData(string heroTitle, string key)
    {
        if (_heroDict == null)
        {
            using var file = FileAccess.Open("res://数据(C#)/heroes.json", FileAccess.ModeFlags.Read);
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

            _heroDict = json.Data.As<Dictionary>();
        }

        return _heroDict.GetValueOrDefault($"{heroTitle}.{key}");
    }
    /// <summary>
    /// 从cards.json中加载英雄文本
    /// </summary>
    /// <param name="heroTitle">卡牌类名，例如 Penelopea</param>
    /// <returns>英雄文本</returns>
    public static HeroString Loading(string heroTitle)
    {
        var heroString = new HeroString();
        if (GetData(heroTitle, "Title").VariantType != Variant.Type.Nil)
        {
            heroString.Title = GetData(heroTitle, "Title").AsString();
        }
        if (GetData(heroTitle, "Flavor").VariantType != Variant.Type.Nil)
        {
            heroString.Flavor = GetData(heroTitle, "Flavor").AsString();
        }
        if (GetData(heroTitle, "Camp").VariantType != Variant.Type.Nil)
        {
            var camp = GetData(heroTitle, "Camp").AsString();
            heroString.Camp = camp switch
            {
                "Zombie" => Camp.Zombie,
                "Plant" => Camp.Plant,
                "僵尸" => Camp.Zombie,
                "植物" => Camp.Plant,
                _ => Camp.Zombie
            };
        }
        if (GetData(heroTitle, "Category").VariantType != Variant.Type.Nil)
        {
            var category = GetData(heroTitle, "Category").AsStringArray();
            heroString.Category = new List<Class>();
            for (int i = 0; i < 2; i++)
            {
                heroString.Category.Add(category[i] switch
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
                });
            }
        }
        if (GetData(heroTitle, "Animation").VariantType != Variant.Type.Nil)
        {
            heroString.AnimationPath = GetData(heroTitle, "Animation").AsString();
        }
        if (GetData(heroTitle, "Icon").VariantType != Variant.Type.Nil)
        {
            heroString.IconPath = GetData(heroTitle, "Icon").AsString();
        }
        if (GetData(heroTitle, "DesScale").VariantType != Variant.Type.Nil)
        {
            heroString.DesScale = GetData(heroTitle, "DesScale").AsSingle();
        }
        if (GetData(heroTitle, "DesOffset").VariantType != Variant.Type.Nil)
        {
            heroString.DesOffset = GetData(heroTitle, "DesOffset").AsSingle();
        }
        return heroString;
    }
}
