using Pack;

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
    public override string IconSkelPath => "res://数据资源/属性动画/血量动画.tres";
}
