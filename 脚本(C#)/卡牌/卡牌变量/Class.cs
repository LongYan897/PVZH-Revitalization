using Godot;
using System;
using System.Runtime.CompilerServices;

namespace Card;

/// <summary>
/// 派系
/// </summary>
[Flags]
public enum Class
{
    /// <summary>猛长</summary>
    MegaGrow = 1 << 0,
    /// <summary>光能</summary>
    Solar = 1 << 1,
    /// <summary>守卫</summary>
    Guardian = 1 << 2,
    /// <summary>爆花</summary>
    Kabloom = 1 << 3,
    /// <summary>聪明</summary>
    Smarty = 1 << 4,

    /// <summary>野兽</summary>
    Beastly = 1 << 5,
    /// <summary>有脑</summary>
    Brainy = 1 << 6,
    /// <summary>疯狂</summary>
    Crazy = 1 << 7,
    /// <summary>健壮</summary>
    Hearty = 1 << 8,
    /// <summary>狡猾</summary>
    Sneaky = 1 << 9,
    /// <summary>所有派系</summary>
}

public static class ClassExtension
{

    public static string ToString(this Class c)
    {
        return c switch {
            Class.Beastly =>"野兽",
            Class.Brainy => "有脑",
            Class.Crazy => "疯狂",
            Class.Guardian => "守卫",
            Class.Hearty => "健壮",
            Class.Kabloom => "爆花",
            Class.MegaGrow => "猛长",
            Class.Smarty => "聪明",
            Class.Sneaky => "狡猾",
            Class.Solar => "光能",
            _ => ""
        };
    }
    public static string ToPath(this Class c)
    {
        return c switch
        {
            Class.Solar => "res://素材/ui/界面ui/光能.png",
            Class.Crazy => "res://素材/ui/界面ui/疯狂.png",
            Class.Smarty => "res://素材/ui/界面ui/聪明.png",
            Class.Brainy => "res://素材/ui/界面ui/有脑.png",
            Class.Beastly => "res://素材/ui/界面ui/猛兽.png",
            Class.Guardian => "res://素材/ui/界面ui/守卫.png",
            Class.Hearty => "res://素材/ui/界面ui/健壮.png",
            Class.Kabloom => "res://素材/ui/界面ui/爆花.png",
            Class.MegaGrow => "res://素材/ui/界面ui/猛长.png",
            Class.Sneaky => "res://素材/ui/界面ui/狡猾.png",
            _ => "res://素材/ui/界面ui/光能.png"
        };
    }

    public static Color ToColor(this Class c)
    {
        return c switch
        {
            Class.Smarty => new Color("ffffff"),
            Class.Solar => new Color("f5c728"),
            Class.Guardian => new Color("844837"),
            Class.MegaGrow => new Color("3d9854"),
            Class.Kabloom => new Color("f43939"),
            Class.Brainy => new Color("e652c8"),
            Class.Sneaky => new Color("393a39"),
            Class.Beastly => new Color("2ebbda"),
            Class.Crazy => new Color("661dd1"),
            Class.Hearty => new Color("f19409"),
            _ => new Color("ffffff")
        };
    }
}