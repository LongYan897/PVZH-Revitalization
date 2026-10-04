using Controller;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class BackButton : TextureButton
{
    public static BackButton JoinScene()
    {
        var btn = Main.SceneContainer.GetNode<BackButton>("%返回按钮");
        btn.GlobalPosition = new Vector2(-112f, 1168.0f);
        btn.JoinIn();
        return btn;
    }
    private void JoinIn()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this,"position",new Vector2(0f,1168f),0.1f)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", new Vector2(0.9f, 0.9f), 0.1f)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", new Vector2(1f, 1f), 0.1f)
            .SetEase(Tween.EaseType.Out);
    }
    private async Task LeaveOut()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "position", new Vector2(-112f, 1168f), 0.1f)
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
