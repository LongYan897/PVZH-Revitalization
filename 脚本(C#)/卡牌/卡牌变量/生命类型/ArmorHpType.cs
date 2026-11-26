using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pack;

namespace Card;

public class ArmorHpType : HpType
{
    public static (bool, HpType) Parse(string str)
    {
        var name = str.Split(":")[0];
        return (name is "护甲" or "Armor", new ArmorHpType());
    }

    [IconText("ffffff", "91001b", 3)]
    public const string Armor = "res://素材(C#)/文本图片/Icon_HeartArmor.png";
    public override string IconSkelPath => "res://数据资源/属性动画/血量动画.tres";
    public override string IconPath => "res://素材(C#)/文本图片/Icon_HeartArmor.png";
    public override string ExLabel => "";
    public override Buff Buff => null;
}
