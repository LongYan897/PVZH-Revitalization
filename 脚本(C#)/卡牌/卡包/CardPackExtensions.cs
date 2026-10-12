using FileExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Card.Pack;

/// <summary>
/// CardPack 的扩展方法，提供基于可控种子的伪随机抽取与洗牌。
/// 使用 System.Random 并传入 seed 保证可复现（伪随机）。
/// </summary>
public partial class CardPackExtensions
{
    public int seed { get; private set; }
    private string _path { get; } = "res://数据(C#)/cardPack.json";
    private FileJson _file { get; set; } = new();
    private Random _random { get; set; } = new();
    public CardPackExtensions(int seed)  => this.seed = seed; 
    //public CardPack DrawCard()
    //{
    //    //CardPack resCardPack = new();
    //}

}
