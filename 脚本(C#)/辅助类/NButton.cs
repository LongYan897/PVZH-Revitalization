using Godot;

namespace Pack;

[GlobalClass]
public partial class NButton : TextureButton
{
    private new Vector2 Size;
    public override void _Ready()
    {
        ButtonDown += OnNButtonDown;
        ButtonUp += OnNButtonUp;
        Size = Scale;
    }
    protected virtual void OnDown()
    { }
    protected virtual void OnUp() { }
    private void OnNButtonDown()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "scale", Size * 0.9f, 0.1f);
        OnDown();
    }
    private void OnNButtonUp()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "scale", Size, 0.1f);
        OnUp();
    }
}
