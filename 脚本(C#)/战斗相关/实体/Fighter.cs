using Card;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Variable;

namespace Battle.Entity;

[GlobalClass]
/// <summary>
/// 战斗单位的实体
/// </summary>
public partial class Fighter : Control
{
    private static readonly PackedScene Scene = GD.Load<PackedScene>("res://场景(C#)/战斗单位.tscn");
    /// <summary>
    /// 绑定的卡牌
    /// </summary>
    private FighterCardModel Model;
    /// <summary>
    /// 生成单位
    /// </summary>
    /// <param name="model">绑定的卡牌</param>
    /// <returns>单位</returns>
    public static Fighter Generate(FighterCardModel model)
    {
        var fighter = Scene.Instantiate<Fighter>();
        fighter.Model = model;
        return fighter;
    }
    /// <summary>
    /// 返还绑定的卡牌
    /// </summary>
    /// <returns>绑定的卡牌</returns>
    public FighterCardModel ReturnCard()
    {
        var fighterCard = Model;
        Model = null;
        return fighterCard;
    }
}
