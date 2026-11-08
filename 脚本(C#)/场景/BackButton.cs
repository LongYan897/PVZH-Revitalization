using Controller;
using Godot;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class BackButton : TextureButton
{
    private static readonly Vector2 Offset = new Vector2(112f, 0f);

    public static BackButton JoinScene(bool hideMe)
    {
        var btn = GetGlobalButton();
        if (btn == null) return null;

        if (!hideMe)
            btn.JoinIn();
        return btn;
    }

    public static BackButton GetGlobalButton()
    {
        var container = Main.SceneContainer;
        if (container == null || !GodotObject.IsInstanceValid(container))
            return null;

        var btn = container.GetNodeOrNull<BackButton>("%返回按钮");
        return btn != null && GodotObject.IsInstanceValid(btn) ? btn : null;
    }

    private void JoinIn()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "position", new Vector2(-280.0f, 888.0f), 0.1f)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", new Vector2(0.9f, 0.9f), 0.1f)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", new Vector2(1f, 1f), 0.1f)
            .SetEase(Tween.EaseType.Out);
    }

    private async Task LeaveOut()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "position", new Vector2(-392.0f, 888.0f), 0.1f)
            .SetEase(Tween.EaseType.In);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public override void _Ready()
    {
        Pressed += BackButtonPressed;
    }

    private async void BackButtonPressed()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "scale", new Vector2(0.9f, 0.9f), 0.1f)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", new Vector2(1f, 1f), 0.1f)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(tween, Tween.SignalName.Finished);
        await LeaveOut();
        SceneManager.ReturnToSub();
    }
}