using Controller;
using Godot;
namespace Scene;

[GlobalClass]
public partial class SubMenu : Control
{
    [Export]
    public string SubScene {  get; set; }
    private BackButton Return;
    private SubMenuScene SubmenuScene;
    private float InitScale;
    public sealed override void _Ready()
    {
        InitScale = Scale.X;
        GetNode<Button>("Button").Pressed += Enter;
        ReadyField();
    }
    public virtual void ReadyField()
    { }
    private async void Enter()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this,"scale",(InitScale - 0.1f) * new Vector2(1,1),0.1f);
        tween.TweenProperty(this, "scale", InitScale * new Vector2(1, 1), 0.1f);
        await ToSignal(tween, Tween.SignalName.Finished);
        SceneManager.JumpToSub(this);
    }
    public SubMenuScene Instantiate()
    {
        SubmenuScene = GD.Load<PackedScene>(SubScene).Instantiate<SubMenuScene>();
        Visible = false;
        return SubmenuScene;
    }
    public void Destory()
    {
        Visible = true;
        Tween tween = CreateTween();
        tween.TweenProperty(this, "scale", (InitScale + 0.1f) * new Vector2(1, 1), 0.15f);
        tween.TweenProperty(this, "scale", InitScale * new Vector2(1,1), 0.1f);
        SubmenuScene.QueueFree();
    }
}
