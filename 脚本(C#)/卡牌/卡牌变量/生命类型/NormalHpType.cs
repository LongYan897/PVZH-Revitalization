using Godot;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

public class NormalHpType : HpType
{
    public static (bool, HpType) Parse(string str)
    {
        var bol = str.Split(":")[0] == "普通" || str.Split(":")[0] == "Normal";
        var hp = new NormalHpType();
        return (bol, hp);
    }

    [IconText("ffffff", "91001b", 3)]
    public const string Health = "res://素材(C#)/文本图片/heart.png";
    public override Texture2D Icon => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/heart.png");
}
