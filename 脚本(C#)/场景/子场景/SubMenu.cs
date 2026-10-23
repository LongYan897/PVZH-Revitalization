using Controller;
using Godot;
namespace Scene;

[GlobalClass]
public partial class SubMenu : Control
{
	[Export]
	public string SubScene { get; set; }
	/// <summary>
	/// 打开这个菜单时是否显示加载界面。<br/>
	/// 盖在主界面上的二级面板（如卡包列表）保持 false；整屏切换的场景（如对战、图鉴）勾上 true。
	/// </summary>
	[Export]
	public bool ShowLoading { get; set; } = true;

	private SubMenuScene SubmenuScene;
	private float InitScale;

	public sealed override void _Ready()
	{
		InitScale = Scale.X;
		GetNode<Button>("Button").Pressed += Enter;
		ReadyField();
	}
	public sealed override void _EnterTree()
	{
		SceneManager.Register(this);
	}
	public sealed override void _ExitTree()
	{
		SceneManager.Unregister(this);
	}
	public virtual void ReadyField()
	{ }

	/// <summary>切换这个菜单按钮是否可点</summary>
	public void Function(bool enable)
	{
		GetNode<Button>("Button").MouseFilter = enable ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
	}

	private async void Enter()
	{
		if (!canPress) return;
		canPress = false;
		Tween tween = CreateTween();
		tween.TweenProperty(this, "scale", (InitScale - 0.1f) * new Vector2(1, 1), 0.1f);
		tween.TweenProperty(this, "scale", InitScale * new Vector2(1, 1), 0.1f);
		await ToSignal(tween, Tween.SignalName.Finished);
		SceneManager.JumpToSub(this);
	}

	private bool canPress = true;

	/// <summary>从子场景返回这一层后，恢复这个按钮的可点状态</summary>
	public void ResetPress() => canPress = true;

	/// <summary>
	/// 打开这个菜单时要传给子场景的数据（例如"点的是哪个卡包"）。<br/>
	/// SceneManager.JumpToSub 会把它放进子场景的 <see cref="SubMenuScene.Payload"/>，子场景在 _Load 里读。
	/// </summary>
	public object Payload { get; set; }

	/// <summary>本菜单打开的子场景实例（没打开过就是 null）</summary>
	public SubMenuScene GetScene() => SubmenuScene;

	/// <summary>
	/// 实例化 SubScene 指向的场景。失败时返回 null 并打印明确错误，
	/// 而不是像 Instantiate&lt;SubMenuScene&gt;() 那样直接抛 InvalidCastException。
	/// </summary>
	public SubMenuScene Instantiate()
	{
		SubmenuScene = null;
		if (string.IsNullOrWhiteSpace(SubScene))
		{
			GD.PushError($"[SubMenu] {Name} 没有设置 SubScene");
			return null;
		}
		PackedScene packed = GD.Load<PackedScene>(SubScene);
		if (packed == null)
		{
			GD.PushError($"[SubMenu] 加载子场景失败：{SubScene}");
			return null;
		}
		Node node = packed.Instantiate();
		if (node is SubMenuScene scene)
		{
			SubmenuScene = scene;
			return scene;
		}
		GD.PushError($"[SubMenu] {SubScene} 的根节点不是 SubMenuScene（请在根节点挂继承 SubMenuScene 的脚本）");
		node.Free();
		return null;
	}

	/// <summary>释放本菜单打开的场景，并清空引用</summary>
	public void FreeScene()
	{
		if (SubmenuScene != null && GodotObject.IsInstanceValid(SubmenuScene))
		{
			SubmenuScene.IsOpening = false;
			SubmenuScene.QueueFree();
		}
		SubmenuScene = null;
	}

	/// <summary>回主界面时用：播一下按钮缩放并释放场景</summary>
	public void Destory()
	{
		Visible = true;
		canPress = true;
		Tween tween = CreateTween();
		tween.TweenProperty(this, "scale", (InitScale + 0.1f) * new Vector2(1, 1), 0.15f);
		tween.TweenProperty(this, "scale", InitScale * new Vector2(1, 1), 0.1f);
		FreeScene();
	}
}
