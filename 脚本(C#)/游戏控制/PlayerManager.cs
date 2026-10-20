
using Card;
using Hero.Model;
using Phrases;

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
    public static void FillPlayer(Player p1, Player p2)
    {
        P1 = p1;
        P2 = p2;
    }
    /// <summary>
    /// 获取对应阵营的玩家
    /// </summary>
    /// <param name="camp">玩家的阵营</param>
    /// <returns></returns>
    public static Player GetPlayer(Camp camp)
    {
        if (!PhraseManager.IsInBattle)
            return null;
        return P1.Status.Hero.Camp == camp ? P1 : P2;
    }
    public static void Hosting(Player host)
    {
        HostPlayer = host;
    }
    /// <summary>
    /// 通过英雄获取当前玩家
    /// </summary>
    /// <param name="hero">英雄</param>
    /// <returns>操控该英雄的玩家</returns>
    public static Player GetFromHero(HeroModel hero)
    {
        if (hero == P1.Status.Hero)
            return P1;
        else if (hero == P2.Status.Hero)
            return P2;
        return null;
    }
    /// <summary>
    /// 当前视角的玩家
    /// </summary>
    public static Player HostPlayer { get; private set; }
}
