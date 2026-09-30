namespace Card;

public class NormalStarType : StarType
{
    public static (bool, StarType) Parse(string str)
    {
        var bol = str.Split(":")[0] == "星星" || str.Split(":")[0] == "Star";
        var atk = new NormalStarType();
        return (bol, atk);
    }
    public override string IconSkelPath => "res://数据资源/属性动画/等级动画.tres";
}
