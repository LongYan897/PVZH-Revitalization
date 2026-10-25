using Card;
using Card.Pack;
using Godot;
using Hero.Model;
using System.Collections.Generic;

namespace Play;

public class Player
{
    protected Player() { }
    public Player(string id) { Id = id; }

    /// <summary>
    /// 玩家的名称
    /// </summary>
    public string Id { get; private set; }

    /// <summary>
    /// 玩家的战斗状态
    /// </summary>
    public readonly PlayerBox Status = new();

    /// <summary>
    /// 玩家的资产
    /// </summary>
    public PlayerAsset Asset { get; private set; } = new();

    /// <summary>
    /// 玩家的卡组
    /// </summary>
    public IReadOnlyList<PresetPack> Presets => Asset.Presets;

    /// <summary>
    /// 玩家收集的卡牌
    /// </summary>
    public IReadOnlyList<CardModel> Cards => Asset.Cards;

    /// <summary>
    /// 准备一次战斗，创建一个战斗状态
    /// </summary>
    /// <typeparam name="T">英雄类</typeparam>
    public void PrefABox<T>(PresetPack deck) where T : HeroModel, new()
    {
        var hero = HeroModel.Load<T>();
        Status.PrefABox(this, hero, deck);
    }
}