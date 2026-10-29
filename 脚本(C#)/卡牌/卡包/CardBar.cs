using Godot;
using System.Threading.Tasks;
/// <summary>
/// 卡包详情里分组用的横幅。<br/>
/// 注意：详情场景是在 SubMenuScene._EnterTree() 里构建内容的，那时子节点还没进树、也没跑 _Ready，
/// 所以这里不能用 _Ready 里缓存节点的方式，改成"访问时才取"（GetNode 在未入树时也能取到子节点）。
/// </summary>
public partial class CardBar : Panel
{
	/// <summary>横幅上的文字（第一次访问时取节点）</summary>
	public Label IntroduceLabel => GetNode<Label>("介绍文本");
	public Tween Tween1 { get; set; }

	/// <summary>设置横幅文字</summary>
	public void SetText(string text)
	{
		IntroduceLabel.Text = text ?? "";
	}
	public async Task IntroIn(float time = 0.5f)
	{
		// 从左侧展开 + 淡入。
		// 注意：动的是 position/scale 时，横幅必须放在普通 Control 格子里，
		// 直接放进 FlowContainer 会被容器排版重置掉，动画就看不到。
		Scale = new Vector2(0, 1);
		Modulate = new Color(1, 1, 1, 0);
		QuickTween().TweenProperty(this, "scale", Vector2.One, time)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		Tween1.Parallel().TweenProperty(this, "modulate:a", 1.0f, time * 0.6f);
		await ToSignal(Tween1, Tween.SignalName.Finished);
	}
	private Tween QuickTween()
	{
		if (Tween1 != null && Tween1.IsValid() && Tween1.IsRunning())
		{
			Tween1.Kill();
		}
		Tween1 = CreateTween();
		return Tween1;
	}
}
