using Card;

namespace Target;
/// <summary>
/// 用于卡牌选择目标的类
/// </summary>
public readonly struct TargetContext
{
    public bool Line { get; init; }
    public bool Grid { get; init; }
    public bool CoopGrid { get; init; }
    public FighterCardModel FighterCard { get; init; }
    public static TargetContext Line1 => new() { Line = true };
    public static TargetContext Grid1 => new() { Grid = true };
    public static TargetContext Coop1 => new() { CoopGrid = true };

    public static TargetContext From(TargetType type, FighterCardModel card = null)
    {
        return new TargetContext
        {
            Line = type.HasFlag(TargetType.Lines),
            Grid = type.HasFlag(TargetType.Grids),
            CoopGrid = type.HasFlag(TargetType.CoopGrids),
            FighterCard = card,
        };
    }
}