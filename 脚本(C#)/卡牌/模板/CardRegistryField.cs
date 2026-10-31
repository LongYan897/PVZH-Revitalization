namespace Card;

/// <summary>
/// 卡牌描述URL的注册区域
/// </summary>
public static class CardRegistryField
{
    [URL]
    public static string 消灭 => "使单位立即死亡。";
    [URL]
    public static string 被消灭 => "单位死亡时。";
    [URL]
    public static string 弹射 => "使单位移回其所有者的手牌。";
    [URL]
    public static string 移动 => "单位的位置切换到其他线。";
    [URL]
    public static string 现身 => "僵尸从墓碑中显现时。";
    [URL]
    public static string 打出 => "单位被打出到场上时。";
    [URL]
    public static string 额外攻击 => "使单位额外进行攻击。";
    [URL]
    public static string 生成 => "在一条线上生成单位。";
    [URL]
    public static string 召唤 => "把随机卡牌加入手牌。";
    [URL]
    public static string 额外战斗 => "使一条线上的所有单位额外进行攻击。";
    [URL]
    public static string 融合 => "在这个单位上打出{#/&:n}来使用此能力。";
    [URL]
    public static string 进化 => "在{#/&:n}上打出这个单位来使用此能力。";
    [URL]
    public static string 两栖 => "可在水路或陆地上打出。";
    [URL]
    public static string 组队 => "可在同一行与另一植物组队。";
    [URL]
    public static string 墓碑 => "躲在墓碑下，直到锦囊牌阶段。";
    [URL]
    public static string 本回合 => "直到战斗阶段结束。";
    [URL]
    public static string 下回合 => "直到僵尸出牌阶段开始。";
}
