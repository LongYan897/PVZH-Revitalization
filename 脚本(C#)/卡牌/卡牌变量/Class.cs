using System;

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
    Sneaky = 1 << 9
}
