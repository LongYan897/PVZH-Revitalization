using Controller;
using Godot;
using System;
namespace Scene;

[GlobalClass]
public partial class SubMenu : Control
{
	[Export]
	public string SubScene { get; set; }
	/// <summary>
	/// 打开这个菜单时是否显示加载界面。<br/>
	/// </summary>
	[Export]
	public bool ShowLoading { get; set; } = true;
    /// <summary>
    /// 是否控制中心为 0.5,0.5
    /// </summary>
    [Export]
    public bool CenterPivot { get; set; } = false;
    /// <summary>
    /// 是否是常驻按钮(不会取消可以点按)
    /// </summary>
    [Export]
    public bool IsConstant { get; set; } = false;
	private SubMenuScene SubmenuScene;
	private float InitScale;
    public void ResetPress() => canPress = true;
    public object Payload { get; set; }
    public SubMenuScene GetScene() => SubmenuScene;

    public sealed override void _Ready()
	{
        if (PivotOffset == Vector2.Zero && PivotOffsetRatio == Vector2.Zero && CenterPivot)
            PivotOffsetRatio = new Vector2(0.5f, 0.5f);
        InitScale = Scale.X;
		GetNode<Button>("Button").Pressed += Enter;
		ReadyField();
	}
    public sealed override void _EnterTree()
    {
        SceneManager.Register(this);
    }
	protected virtual void ReadyField()
	{ }
	public void Function(bool enable)
	{
		GetNode<Button>("Button").MouseFilter = enable ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
	}
	private async void Enter()
	{
		if (!canPress) return;
        if (!IsConstant)
		    canPress = false;
		Tween tween = CreateTween();
		tween.TweenProperty(this, "scale", (InitScale - 0.05f) * new Vector2(1, 1), 0.1f);
		tween.TweenProperty(this, "scale", InitScale * new Vector2(1, 1), 0.1f);
		await ToSignal(tween, Tween.SignalName.Finished);
		EnterField();
		SceneManager.JumpToSub(this);
    }
    protected virtual void EnterField()
    { }
    public sealed override void _ExitTree()
    {
        SceneManager.Unregister(this);
    }
    
	private bool canPress = true;
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
    public void FreeScene()
    {
        if (SubmenuScene != null && GodotObject.IsInstanceValid(SubmenuScene))
        {
            SubmenuScene.IsOpening = false;
            SubmenuScene.QueueFree();
        }
        SubmenuScene = null;
    }

    public void Destory()
    {
        SubmenuScene.IsOpening = false;
        Visible = true;
		canPress = true;
		Tween tween = CreateTween();
		tween.TweenProperty(this, "scale", (InitScale + 0.05f) * new Vector2(1, 1), 0.15f);
		tween.TweenProperty(this, "scale", InitScale * new Vector2(1, 1), 0.1f);
        FreeScene();
    }
	public void Reparent(Node node)
	{
        SubmenuScene.Reparent(node);
    }
}
