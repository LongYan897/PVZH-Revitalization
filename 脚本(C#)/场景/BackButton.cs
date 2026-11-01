using Controller;
using Godot;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class BackButton : TextureButton
{
    /// <summary>
    /// 显示/隐藏主界面那个全局返回按钮。<br/>
    /// 单独测试某个子场景（编辑器里直接运行该场景）时没有主节点脚手架，
    /// 这里直接返回 null，不抛异常，让场景按自己的逻辑跑起来。
    /// </summary>
    public static BackButton JoinScene(bool hideMe)
    {
        var btn = GetGlobalButton();
        if (btn == null) return null;

        btn.GlobalPosition = new Vector2(-112f, 1168.0f);
        if (!hideMe)
            btn.JoinIn();
        return btn;
    }

    /// <summary>取主界面里的全局返回按钮；没有脚手架（单独测试场景）时返回 null</summary>
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
        tween.TweenProperty(this, "position", new Vector2(0f, 1168f), 0.1f)
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
