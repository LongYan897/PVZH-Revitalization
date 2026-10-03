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

    /// <summary>
    /// 每个 SceneAnimKind 对应的场景路径
    /// </summary>
    private static string PathOf(SceneAnimKind kind) => kind switch
    {
        SceneAnimKind.FighterDown => "res://场景(C#)/场景动画/战斗单位倒下.tscn",
        SceneAnimKind.CardDown => "res://场景(C#)/场景动画/卡牌落下.tscn",
        SceneAnimKind.CardToAnim => "res://场景(C#)/场景动画/卡牌到动画.tscn",
        SceneAnimKind.AnimToDes => "res://场景(C#)/场景动画/动画到描述.tscn",
        _ => null,
    };

    /// <summary>
    /// 卡牌统一入口：播一个从 startAt 到 endAt 的位移动画
    /// </summary>
    /// <param name="kind">场景动画类型</param>
    /// <param name="startAt">起始位置（全局坐标）</param>
    /// <param name="endAt">结束位置（全局坐标）</param>
    /// <param name="duration">位移时长（秒）</param>
    /// <param name="soundPath">可选：音效路径，为 null 不播</param>
    /// <param name="soundVolumeDb">音效音量</param>
    /// <param name="soundPitch">音效音调</param>
    public static async Task PlayCardAsync(
        SceneAnimKind kind,
        Vector2 startAt,
        Vector2 endAt,
        float duration,
        string soundPath = null,
        float soundVolumeDb = 0f,
        float soundPitch = 1f)
    {
        if (!string.IsNullOrEmpty(soundPath))
            WavPlayer.Play(soundPath, soundVolumeDb, soundPitch);

        await PlayTask(kind, PathOf(kind), Position(startAt, endAt, duration));
    }

    public static async Task FighterDownAsync(Vector2 startAt, Vector2 endAt, float duration, string soundPath = null)
        => await PlayCardAsync(SceneAnimKind.FighterDown, startAt, endAt, duration, soundPath);

    public static async Task CardDownAsync(Vector2 startAt, Vector2 endAt, float duration, string soundPath = null)
        => await PlayCardAsync(SceneAnimKind.CardDown, startAt, endAt, duration, soundPath);

    public static async Task CardToAnimAsync(Vector2 startAt, Vector2 endAt, float duration, string soundPath = null)
        => await PlayCardAsync(SceneAnimKind.CardToAnim, startAt, endAt, duration, soundPath);

    public static async Task AnimToDesAsync(Vector2 startAt, Vector2 endAt, float duration, string soundPath = null)
        => await PlayCardAsync(SceneAnimKind.AnimToDes, startAt, endAt, duration, soundPath);
}