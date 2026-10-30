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

	public override void _Ready()
	{
		NodeList = GetNode<FlowContainer>("底版/SmoothScrollContainer/HFlowContainer");
		NodeList.AddThemeConstantOverride("h_separation", Separation);
		NodeList.AddThemeConstantOverride("v_separation", Separation);
		NodeList.Alignment = FlowContainer.AlignmentMode.Begin;
		NodeList.LastWrapAlignment = FlowContainer.LastWrapAlignmentMode.Begin;
	}
}
