using Godot;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

public class NormalAtkType : AtkType
{
    public static (bool, AtkType) Parse(string str)
    {
        var bol = str.Split(":")[0] == "普通" || str.Split(":")[0] == "Normal";
        var atk = new NormalAtkType();
        return (bol, atk);
    }

    [IconText("ffffff", "338000", 3)]
    public const string Strength = "res://素材(C#)/文本图片/attack.png";

    public override Texture2D Icon => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/attack.png");
}
