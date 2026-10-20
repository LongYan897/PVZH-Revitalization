using Card;
using Controller;
using Godot;
using Scene;
using System;
using System.Threading.Tasks;

public partial class CardTilesControl : SubMenuScene
{
	private BoxContainer CardTilesGroup;
	private Panel Container;
	private Tween Tween1;
	private TextureButton BackButton;
	private bool _closing;

	/// <summary>本场景自带返回按钮，隐藏主界面那个全局返回按钮。</summary>
	public override bool HideReturnButton => true;

	/// <summary>
	/// 子场景初始化。注意：_Load 由 SubMenuScene._EnterTree() 在进入场景树时调用，
	/// 比 _Ready 更早，所以节点初始化要写在这里，不能再放 _Ready。
	/// </summary>
	public override Task _Load()
	{
		CardTilesGroup = GetNode<BoxContainer>("卡包遮罩/ScrollContainer/VBoxContainer");
		Container = GetNode<Panel>("卡包遮罩");

		// 场景里的返回按钮就是个普通 TextureButton（卡包目录/返回按钮），
		// TextureButton 继承 BaseButton，所以直接连 Pressed 信号即可。
		BackButton = GetNodeOrNull<TextureButton>("返回按钮");
		if (BackButton != null)
			BackButton.Pressed += PlayOutro;
		else
			GD.PushWarning("[CardTilesControl] 场景里没找到 返回按钮(TextureButton)");

		IntroAn(0.6f);
		var Tile = GD.Load<PackedScene>("res://场景(C#)/卡包/卡包.tscn");
		for(int i = 1; i <= 5; i++)
		{
			var NewTile = Tile.Instantiate<CardTile>();
			CardTilesGroup.AddChild(NewTile);
			NewTile.LoadResource(i%2 == 0 ? "Pack1" : "Pack2");

		}
		return Task.CompletedTask;
	}
	public override void _ExitTree()
	{
		// 场景销毁时解绑，避免信号引用到已释放的节点
		if (BackButton != null)
			BackButton.Pressed -= PlayOutro;
	}
	private void IntroAn(float time)
	{
		if (Container == null) return;
		Vector2 restPos = Container.Position;
		Container.Position = new Vector2(restPos.X, GetViewportRect().Size.Y);
		// 上一轮收回时把按钮淡出了，这里还原
		if (BackButton != null)
			BackButton.Modulate = new Color(1, 1, 1, 1);
		QuickTween().TweenProperty(Container, "position", restPos, time)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.InOut);
	}
	/// <summary>
	/// 点返回：卡包遮罩滑回屏幕下方、返回按钮淡出，播完再返回上一级。
	/// </summary>
	private async void PlayOutro()
	{
		if (_closing || Container == null) return;
		_closing = true;

		Vector2 outPos = new Vector2(Container.Position.X, GetViewportRect().Size.Y);
		var tween = QuickTween();
		tween.TweenProperty(Container, "position", outPos, 0.3f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);
		// 返回按钮在遮罩外面（兄弟节点），单独淡出，否则收回后它还留在屏幕上
		if (BackButton != null)
			tween.Parallel().TweenProperty(BackButton, "modulate:a", 0.0f, 0.2f);

		await ToSignal(tween, Tween.SignalName.Finished);
		_closing = false;
		OnOutroFinished();
	}
	/// <summary>
	/// 收回动画播完后的去向：返回上一级菜单。
	/// 想改成别的（比如直接释放本场景）就重写这个方法。
	/// </summary>
	protected virtual void OnOutroFinished()
	{
		SceneManager.ReturnToSub();
	}
	private Tween QuickTween()
	{
		// 第一次调用时 Tween1 还是 null，直接调 IsRunning() 会抛 NullReferenceException。
		// 而已经播完（或被 Kill）的 Tween 会被引擎释放，此时再访问会抛异常，
		// 所以判空之后要用 IsValid() 而不是只看 IsRunning()。
		if (Tween1 != null && Tween1.IsValid())
			Tween1.Kill();
		Tween1 = CreateTween();
		return Tween1;
	}
}
