using Godot;

/// <summary>
/// 开包结果列表：底版 + 滚动容器 + HFlowContainer。<br/>
/// 排版规则和「卡包详情」一致：一行最多 4 张、靠左、间距 12。
/// </summary>
public partial class OpeningCardList : Node2D
{
    /// <summary>放组名横幅和卡牌格子的流式容器</summary>
    public FlowContainer NodeList { get; private set; }

    /// <summary>一排最多几张</summary>
    public const int CardsPerRow = 4;
    /// <summary>每个格子/卡牌之间的间距</summary>
    public const int Separation = 12;

    /// <summary>
    /// 列表的<b>可视宽度</b>：外层有 ScrollContainer 就取它的宽度，否则退回 FlowContainer 自己的。<br/>
    /// 不能直接用 NodeList.Size.X —— FlowContainer 在 ScrollContainer 里会被撑得比可视区还宽
    /// （实测 730 的可视区里它自己算出来 1280），按它居中会把内容整体推到面板外面去。
    /// </summary>
    public float VisibleWidth
    {
        get
        {
            if (NodeList == null) return 0f;
            if (NodeList.GetParent() is Control scroll && scroll.Size.X > 0f) return scroll.Size.X;
            return NodeList.Size.X;
        }
    }

    public override void _Ready()
    {
        FitToViewport();

        NodeList = ResolveList();
        if (NodeList == null)
        {
            GD.PushError("[OpeningCardList] 场景里找不到 FlowContainer（底版 下面任意层级都行），开包结果列表没法排版");
            return;
        }

        NodeList.AddThemeConstantOverride("h_separation", Separation);
        NodeList.AddThemeConstantOverride("v_separation", Separation);
        NodeList.Alignment = FlowContainer.AlignmentMode.Begin;
        NodeList.LastWrapAlignment = FlowContainer.LastWrapAlignmentMode.Begin;
    }

    private void FitToViewport()
    {
        if (GetParent() is Control) return; 

        Vector2 vp = GetViewportRect().Size;
        if (Mathf.IsEqualApprox(vp.X, 1280f) && Mathf.IsEqualApprox(vp.Y, 720f)) return;

        var basePanel = GetNodeOrNull<Control>("底版");
        if (basePanel == null) return;

        basePanel.Position = Vector2.Zero;
        basePanel.Size = vp;

        float panelWidth = Mathf.Min(730f, vp.X);

        var panel = basePanel.GetNodeOrNull<Control>("Panel");
        if (panel != null)
        {
            panel.CustomMinimumSize = new Vector2(panelWidth, 0f);
            panel.Position = new Vector2((vp.X - panelWidth) * 0.5f, 0f);
            panel.Size = new Vector2(panelWidth, vp.Y);
        }

        var scroll = basePanel.GetNodeOrNull<Control>("Panel/SmoothScrollContainer");
        if (scroll != null)
        {
            scroll.Position = Vector2.Zero;
            scroll.Size = new Vector2(panelWidth, vp.Y);
        }
    }

    /// <summary>
    /// 找放横幅/卡牌的那个 FlowContainer。<br/>
    /// 先按原来的路径找；找不到就在整棵子树里递归找第一个 FlowContainer。<br/>
    /// 之所以不写死路径：场景里只要在中间加/删一层 Panel、MarginContainer 之类的壳，
    /// 写死的 "底版/SmoothScrollContainer/HFlowContainer" 就会失效、GetNode 直接抛异常，
    /// 表现出来就是整个开包结果「什么都看不到」。
    /// </summary>
    private FlowContainer ResolveList()
    {
        var byPath = GetNodeOrNull<FlowContainer>("底版/SmoothScrollContainer/HFlowContainer");
        if (byPath != null) return byPath;

        return FindFirstFlow(this);
    }

    private static FlowContainer FindFirstFlow(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is FlowContainer flow) return flow;

            FlowContainer found = FindFirstFlow(child);
            if (found != null) return found;
        }
        return null;
    }
}
