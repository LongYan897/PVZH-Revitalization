using Controller;
using Godot;

/// <summary>
/// 开包结果底部的「收集」按钮。<br/>
/// 点它会结束这次开包，直接回主界面。<br/>
/// 注意：<b>不要动画 CenterContainer 的直接子节点</b> —— Container 排版时会把直接子节点的
/// scale 重置成 1，第一帧就会是满大小（看起来"闪一下"），再被 tween 拉回 0 再涨回来。
/// 所以这里动画加在内部孙节点「购买按钮」(NinePatchRect) 上，它不在任何 Container 的直接子级，
/// scale 不会被重置，出现时就是 0 → 弹到目标大小。
/// </summary>
public partial class EndButton : CenterContainer
{
	/// <summary>弹出动画的可视节点（NinePatchRect）</summary>
	private NinePatchRect _visual;

	/// <summary>点击区域</summary>
	private Button _hitButton;

	private bool _pressed;

	public override void _Ready()
	{
		_hitButton = GetNode<Button>("结束按钮/Button");
		_hitButton.Pressed += OnCollectPressed;

		_visual = GetNode<NinePatchRect>("结束按钮/Button/购买按钮");

		// 以中心缩放
		_visual.PivotOffset = Vector2.Zero;
		_visual.PivotOffsetRatio = new Vector2(0.5f, 0.5f);

		// 场景里给的缩放（0.5）就是目标大小
		Vector2 targetScale = _visual.Scale;

		// 位置补偿：以中心缩放会把视觉往右下推 pivot*(1 - scale)，
		// 这里把 Position 反向偏移同样的距离，保证最终视觉位置和原来（pivot 在左上角时）一致，
		// 否则按钮会整体偏右下（看起来"位置偏了"）。
		_visual.Position -= _visual.Size * 0.5f * (1f - targetScale.X);

		// 立刻设 0（_Ready 在首帧渲染前执行，不会闪）
		_visual.Scale = Vector2.Zero;

		var tween = CreateTween();
		tween.TweenProperty(_visual, "scale", targetScale, 0.5f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
	}

	/// <summary>点「收集」：结束开包，回到主界面</summary>
	private void OnCollectPressed()
	{
		if (_pressed) return;
		_pressed = true;

		// 想只退回上一层（卡包列表）就把这句换成 SceneManager.ReturnToSub();
		SceneManager.ReturnToSub(null,true,true);
	}
}
