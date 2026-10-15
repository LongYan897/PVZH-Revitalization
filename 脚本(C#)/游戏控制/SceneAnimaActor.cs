using Godot;
using System;
using System.Threading.Tasks;

namespace Controller;

public static class SceneAnimaActor
{
    public enum SceneAnimKind
    {
        FighterDown,
        CardDown,
        CardToAnim,
        AnimToDes,
    }

    private static CanvasLayer _fighterDown;
    private static CanvasLayer _cardDown;
    private static CanvasLayer _cardToAnim;
    private static CanvasLayer _animToDes;

    public static void Init(Node node)
    {
        _fighterDown = node.GetNode<CanvasLayer>("%SceneAnimF");
        _cardDown = node.GetNode<CanvasLayer>("%SceneAnimFToC");
        _cardToAnim = node.GetNode<CanvasLayer>("%SceneAnimCToA");
        _animToDes = node.GetNode<CanvasLayer>("%SceneAnimAToD");
    }

    public static Func<Control, Task> Position(Vector2 startAt, Vector2 endAt, float duration)
    {
        return async (Control wrapper) =>
        {
            wrapper.Position = startAt;
            Tween tween = wrapper.CreateTween();
            tween.TweenProperty(wrapper, "position", endAt, duration);
            await wrapper.ToSignal(tween, Tween.SignalName.Finished);
        };
    }

    /// <summary>
    /// 播放场景动画，播完自动释放
    /// </summary>
    public static async Task PlayTask(SceneAnimKind kind, string scenePath, Func<Control, Task> preset)
    {
        var parent = Get(kind);
        if (parent == null) return;

        var scene = GD.Load<PackedScene>(scenePath);
        if (scene == null) return;

        var instance = scene.Instantiate();
        var wrapper = new Control();

        parent.AddChild(wrapper);
        wrapper.AddChild(instance);
        try
        {
            if (preset != null)
                await preset.Invoke(wrapper);
        }
        finally
        {
            wrapper.QueueFree();
        }
    }
    public static async void Play(SceneAnimKind kind, string scenePath, Func<Control, Task> preset) => await PlayTask(kind, scenePath, preset);

    private static CanvasLayer Get(SceneAnimKind kind) => kind switch
    {
        SceneAnimKind.FighterDown => _fighterDown,
        SceneAnimKind.CardDown => _cardDown,
        SceneAnimKind.CardToAnim => _cardToAnim,
        SceneAnimKind.AnimToDes => _animToDes,
        _ => null,
    };
}