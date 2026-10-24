using Godot;

/// <summary>
/// 卡包详情里分组用的横幅。<br/>
/// 注意：详情场景是在 SubMenuScene._EnterTree() 里构建内容的，那时子节点还没进树、也没跑 _Ready，
/// 所以这里不能用 _Ready 里缓存节点的方式，改成"访问时才取"（GetNode 在未入树时也能取到子节点）。
/// </summary>
public partial class CardBar : Panel
{
	/// <summary>横幅上的文字（第一次访问时取节点）</summary>
	public Label IntroduceLabel => GetNode<Label>("介绍文本");

	/// <summary>设置横幅文字</summary>
	public void SetText(string text)
	{
		IntroduceLabel.Text = text ?? "";
	}
}
