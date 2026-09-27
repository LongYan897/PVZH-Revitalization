
using Card;
using Phrase;

namespace Play;

/// <summary>
/// 管理玩家的类(战斗中)
/// </summary>
public static class PlayerManager
{
    /// <summary>
    /// 玩家1(植物方)
    /// </summary>
    private static Player P1;
    /// <summary>
    /// 玩家2(僵尸方)
    /// </summary>
    private static Player P2;
    /// <summary>
    /// 填充玩家
    /// </summary>
    /// <param name="p1">玩家1</param>
    /// <param name="p2">玩家2</param>
    public static void FillPlayer(Player p1,Player p2)
    {

    }
    public static Player GetPlayer(Camp camp)
    {
        if (!PhraseManager.IsInBattle)
            return null;
        return camp switch
        {
            Camp.Plant => P2,
            Camp.Zombie => P1,
            _ => null
        };
    }
}
