using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 卡包切换动画：旧内容往上到下依次飞走 → 新内容从右飞入。<br/>
/// 只作用于「一排一个 HBoxContainer」的 VBox 结构（卡包列表右侧详情栏），
/// 也就是 <see cref="CardPackPreview.Fill"/> 在非 FlowContainer 容器里搭出来的那种结构。<br/>
/// 节点结构约定（Fill 负责搭）：
/// <code>
/// VBoxContainer
/// ├─ Control(横幅格, 高度 50) → CardBar（anchors 铺满，宽度跟着格子）
/// └─ HBoxContainer              → Control(卡片格) → NodeCard
/// </code>
/// 注意：横幅必须放在普通 Control 格子里才能动 Scale —— 直接当 VBox 的子节点，
/// 容器每次排版都会把它 scale 重置成 1，动画看不见。
/// </summary>
public static class CardPackTransition
{
    /// <summary>卡片飞行距离（从右边多远飞进来 / 往右飞多远出去）；实际起点会按屏幕宽度算，这个只是下限</summary>
    public const float FlyOffsetX = 640f;
    /// <summary>横幅格的高度，和 横幅.tscn 的 custom_minimum_size.y 一致</summary>
    public const float BarHeight = 50f;

    private const float BarHiddenScaleX = 0.04f;
    private const float BarHiddenScaleY = 0.25f;

    private const double CardStep = 0.05;   // 每张牌之间错开的延时（从上到下）
    private const double CardMove = 0.26;
    private const double CardFade = 0.16;
    private const double BarMove = 0.18;
    private const double BarStep = 0.03;

    /// <summary>记在卡牌节点上的"这批牌从多远处飞进来"</summary>
    private static readonly StringName MetaFlyOffset = "pack_fly_offset";

    /// <summary>一个"块"：一行卡，或者一条横幅（横幅格 + 条本体）</summary>
    private sealed class Block
    {
        public CardBar Bar;
        public readonly List<NodeCard> Cards = new();
    }

    /// <summary>
    /// 按 VBox 里自上而下的顺序收集块。只认识 Fill 搭出来的结构，
    /// 不认识的子节点（比如「顶部占位」）直接跳过。
    /// </summary>
    private static List<Block> Collect(Container target)
    {
        var blocks = new List<Block>();
        if (target == null) return blocks;

        foreach (Node child in target.GetChildren())
        {
            if (child is CardBar directBar)
            {
                blocks.Add(new Block { Bar = directBar });
                continue;
            }

            // 横幅格：一个 Control，里面只有一条 CardBar
            if (child.GetChildCount() == 1 && child.GetChild(0) is CardBar bar)
            {
                blocks.Add(new Block { Bar = bar });
                continue;
            }

            var block = new Block();
            CollectCards(child, block.Cards);
            if (block.Cards.Count > 0)
                blocks.Add(block);
        }
        return blocks;
    }

    private static void CollectCards(Node node, List<NodeCard> into)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is NodeCard card)
            {
                into.Add(card);
                continue;   // 卡牌自己的子节点不用再往下找
            }
            CollectCards(child, into);
        }
    }

    /// <summary>铺卡时先把横幅/卡牌摆到"还没出现"的状态，等 <see cref="InAsync"/> 统一飞进来。</summary>
    public static void HideForIn(Control barCell, CardBar bar)
    {
        if (barCell != null)
            barCell.CustomMinimumSize = new Vector2(0, BarHeight);   // 高度不变，避免整列跳动
        if (bar == null) return;

        bar.Scale = new Vector2(BarHiddenScaleX, BarHiddenScaleY);
        bar.Modulate = new Color(1, 1, 1, 0);
    }

    public static void HideForIn(NodeCard card, float flyOffsetX)
    {
        if (card == null) return;
        card.Position += new Vector2(flyOffsetX, 0);
        card.SetMeta(MetaFlyOffset, flyOffsetX);   // 入场时按这个值倒推原位
        card.Modulate = new Color(1, 1, 1, 0);
    }

    /// <summary>
    /// 算出"飞入起点"要往右偏多少，才能让卡牌完全躲在屏幕右边外面。<br/>
    /// 容器左边缘（屏幕坐标）到屏幕右边缘的距离，再多送一点余量；
    /// 取不到视口时用 <see cref="FlyOffsetX"/> 兜底。
    /// </summary>
    public static float ComputeFlyOffset(Container target, float cardWidth)
    {
        if (target == null || !GodotObject.IsInstanceValid(target)) return FlyOffsetX;
        Viewport vp = target.GetViewport();
        if (vp == null) return FlyOffsetX;

        float screenRight = vp.GetVisibleRect().End.X;
        float contentLeft = target.GlobalPosition.X;
        float need = screenRight - contentLeft + cardWidth * 0.25f;
        return Mathf.Max(FlyOffsetX, need);
    }

    /// <summary>取出铺卡时记下的飞入偏移（没记过就用默认值）</summary>
    private static float GetFlyOffset(NodeCard card)
        => card != null && card.HasMeta(MetaFlyOffset) ? card.GetMeta(MetaFlyOffset).AsSingle() : FlyOffsetX;

    /// <summary>
    /// 旧内容退场：卡牌从上到下依次往右飞走（带淡出），横幅压扁 + 淡出。<br/>
    /// 返回一个 Task，等全部动画播完（约 0.5s）才完成；没有可动画的内容时立刻完成。
    /// </summary>
    public static async Task OutAsync(Container target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target)) return;

        List<Block> blocks = Collect(target);
        if (blocks.Count == 0) return;

        Tween tween = target.CreateTween();
        tween.SetParallel(true);

        double seq = 0;      // 全局顺序：先横幅、再它下面那一排牌，逐条往后错开
        double end = 0;

        foreach (Block block in blocks)
        {
            if (block.Bar != null)
            {
                double d = seq * BarStep;
                tween.TweenProperty(block.Bar, "scale:y", 0.0f, BarMove)
                    .SetDelay(d)
                    .SetTrans(Tween.TransitionType.Quad)
                    .SetEase(Tween.EaseType.In);
                tween.TweenProperty(block.Bar, "modulate:a", 0.0f, BarMove * 0.8)
                    .SetDelay(d);
                end = Mathf.Max(end, d + BarMove);
                seq += 1;
            }

            foreach (NodeCard card in block.Cards)
            {
                if (!GodotObject.IsInstanceValid(card)) continue;

                double d = seq * CardStep;
                float targetX = card.Position.X + FlyOffsetX;
                tween.TweenProperty(card, "position:x", targetX, CardMove)
                    .SetDelay(d)
                    .SetTrans(Tween.TransitionType.Quad)
                    .SetEase(Tween.EaseType.In);
                tween.TweenProperty(card, "modulate:a", 0.0f, CardFade)
                    .SetDelay(d + (CardMove - CardFade));
                end = Mathf.Max(end, d + CardMove);
                seq += 1;
            }
        }

        if (end <= 0) return;
        await target.ToSignal(tween, Tween.SignalName.Finished);
    }

    /// <summary>
    /// 新内容入场：横幅由细变宽、淡入；卡牌从右边飞回原位、淡入。<br/>
    /// 前提是铺卡时调过 <see cref="HideForIn(NodeCard)"/> / <see cref="HideForIn(Control, CardBar)"/>。
    /// </summary>
    public static async Task InAsync(Container target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target)) return;

        List<Block> blocks = Collect(target);
        if (blocks.Count == 0) return;

        Tween tween = target.CreateTween();
        tween.SetParallel(true);

        double seq = 0;
        double end = 0;

        foreach (Block block in blocks)
        {
            if (block.Bar != null)
            {
                double d = 0.02 + seq * BarStep;
                tween.TweenProperty(block.Bar, "scale", Vector2.One, BarMove + 0.06)
                    .SetDelay(d)
                    .SetTrans(Tween.TransitionType.Back)
                    .SetEase(Tween.EaseType.Out);
                tween.TweenProperty(block.Bar, "modulate:a", 1.0f, BarMove * 0.7)
                    .SetDelay(d);
                end = Mathf.Max(end, d + BarMove + 0.06);
                seq += 1;
            }

            foreach (NodeCard card in block.Cards)
            {
                if (!GodotObject.IsInstanceValid(card)) continue;

                double d = seq * CardStep;
                float restX = card.Position.X - GetFlyOffset(card);
                tween.TweenProperty(card, "position:x", restX, CardMove)
                    .SetDelay(d)
                    .SetTrans(Tween.TransitionType.Quart)
                    .SetEase(Tween.EaseType.Out);
                tween.TweenProperty(card, "modulate:a", 1.0f, CardFade)
                    .SetDelay(d);
                end = Mathf.Max(end, d + CardMove);
                seq += 1;
            }
        }

        if (end <= 0) return;
        await target.ToSignal(tween, Tween.SignalName.Finished);
    }
}
