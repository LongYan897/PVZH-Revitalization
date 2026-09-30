using Pack;

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
    public override string IconSkelPath => "res://数据资源/属性动画/攻击动画.tres";
}
