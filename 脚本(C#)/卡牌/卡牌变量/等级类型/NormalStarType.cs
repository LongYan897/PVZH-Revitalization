namespace Card;

public class NormalStarType : StarType
{
    public static (bool, StarType) Parse(string str)
    {
        var bol = str.Split(":")[0] == "普通" || str.Split(":")[0] == "Normal";
        var atk = new NormalStarType();
        return (bol, atk);
    }
    public override string IconSkelPath => "res://数据资源/属性动画/等级动画.tres";
}
