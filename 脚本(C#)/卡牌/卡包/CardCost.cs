using Godot;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pack;

[GlobalClass]
public partial class CardCost : Control
{
    private static readonly PackedScene Scene = "res://场景(C#)/卡组费用.tscn".Load<PackedScene>();
    private void Exchange(Camp camp)
    {
        var tex = GetNode<Sprite2D>("花费");
        var tex2 = tex.GetNode<Sprite2D>("图标");
        var icon = (camp switch
        {
            Camp.Plant => "res://素材(C#)/文本图片/inhnd_sun_gb.png",
            Camp.Zombie => "res://素材(C#)/文本图片/inhnd_brains.png",
            _ => ""
        }).Load<Texture2D>();
        tex.Texture = icon;
        tex2.Texture = icon;
    }
    public static CardCost Cost(Camp camp)
    {
        CardCost cost = Scene.Instantiate<CardCost>();
        cost.Exchange(camp);
        return cost;
    }
}
