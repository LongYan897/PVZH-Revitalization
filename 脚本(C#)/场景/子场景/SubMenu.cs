using Controller;
using Godot;
namespace Scene;

[GlobalClass]
public partial class SubMenu : Control
{
    [Export]
    public string SubScene { get; set; }
    private BackButton Return;
    private SubMenuScene SubmenuScene;
    private float InitScale;
    public sealed override void _Ready()
    {
        InitScale = Scale.X;
        GetNode<Button>("Button").Pressed += Enter;
        ReadyField();
        SceneManager.Register(this);
    }
    public virtual void ReadyField()
    { }
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
    public SubMenuScene Instantiate()
    {
        SubmenuScene = GD.Load<PackedScene>(SubScene).Instantiate<SubMenuScene>();
        Visible = false;
        return SubmenuScene;
    }
    public void Destory()
    {
        Visible = true;
        canPress = true;
        Tween tween = CreateTween();
        tween.TweenProperty(this, "scale", (InitScale + 0.1f) * new Vector2(1, 1), 0.15f);
        tween.TweenProperty(this, "scale", InitScale * new Vector2(1, 1), 0.1f);
        SubmenuScene.QueueFree();
    }
}
