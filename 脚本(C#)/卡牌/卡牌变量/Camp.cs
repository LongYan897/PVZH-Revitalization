
namespace Card;

/// <summary>
/// 阵营
/// </summary>
public enum Camp
{
    /// <summary>
    /// 植物阵营
    /// </summary>
    Plant,
    /// <summary>
    /// 僵尸阵营
    /// </summary>
    Zombie
}

public static class CampExtension
{
    public static string ToName(this Camp camp)
    {
        return camp switch
        {
            Camp.Plant => "植物",
            Camp.Zombie => "僵尸",
            _=> ""
        };
    }
}