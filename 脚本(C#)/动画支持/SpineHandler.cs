using Godot;
using System;
using System.Threading.Tasks;

namespace Spine;

[GlobalClass]
///<summary>
///用于辅助Spine动画的类
/// </summary>
public partial class SpineHandler : Node2D
{
    private static PackedScene Scene => GD.Load<PackedScene>("res://场景(C#)/动画.tscn");
    private Node _spineSprite;

    public override void _Ready()
    {
        _spineSprite = GetNode<Node>("Spine");
        ConnectAnimationEvent();
        var spine = GetNode<Node>("Spine");
    }
    /// <summary>
    /// 获取一个动画节点
    /// </summary>
    /// <returns></returns>
    public static SpineHandler Get() => Scene.Instantiate<SpineHandler>();
    /// <summary>
    /// 等待动画事件
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <returns></returns>
    public async Task<float> WaitForAnimEvent(string eventName)
    {
        var tcs = new TaskCompletionSource<float>();

        void Handler(string name, float remaining)
        {
            if (name == eventName)
                tcs.TrySetResult(remaining);
        }

        OnSpineAnimEvent += Handler;
        try { return await tcs.Task; }
        finally { OnSpineAnimEvent -= Handler; }
    }

    /// <summary>
    /// 加载 SpineSkeletonDataResource (.tres)
    /// </summary>
    public void LoadSkeletonData(string tresPath)
    {
        if (_spineSprite == null)
        {
            return;
        }
        Resource data = ResourceLoader.Load(tresPath);
        if (data == null)
        {
            return;
        }
        _spineSprite.Set("skeleton_data_res", data);

    }

    /// <summary>
    /// 设置动画。
    /// </summary>
    public void SetAnimation(int track, string animName, bool loop)
    {
        _spineSprite.Call("set_animation", track, animName, loop);
    }
    /// <summary>
    /// 添加动画。
    /// </summary>
    public void AddAnimation(int track, string animName, bool loop, float delay = 0.0f)
    {
        _spineSprite.Call("add_animation", track, animName, loop, delay);
    }

    public void SetAttachment(string id, string attachment)
    {
        _spineSprite.Call("set_attachment", id, attachment);
    }
    public void SetSkin(string skinName)
    {
        _spineSprite.Call("set_skin", skinName);
    }

    public Vector2 GetBoneWorldPos(string boneName)
    {
        return _spineSprite.Call("get_bone_world_pos", boneName).AsVector2();
    }

    public event Action<string,float> OnSpineAnimEvent;

    public void ConnectAnimationEvent()
    {
        _spineSprite.Connect("spine_anim_event", Callable.From<string,float>((name,obj) =>
        {
            OnSpineAnimEvent?.Invoke(name,obj);
        }));
    }
    public void ClearTrack(int track)
    {
        if (_spineSprite == null) return;
        _spineSprite.Call("clear_track", track);
    }
    public void ClearTracks()
    {
        if (_spineSprite == null) return;
        _spineSprite.Call("clear_tracks");
    }

    /// <summary>
    /// 设置动画，等待播放完成后销毁自身
    /// </summary>
    public void SetAnimationAndFreeOnEnd(int track, string animName, bool loop = false)
    {
        SetAnimation(track, animName, loop);
        if (loop) return;

        var callable = Callable.From<GodotObject, GodotObject, GodotObject>(
            (sprite, state, entry) => CallDeferred(nameof(QueueFree)));

        _spineSprite.Connect("animation_completed", callable,
            (uint)GodotObject.ConnectFlags.OneShot);
    }

    /// <summary>
    /// 设置动画，等待播放完成。
    /// </summary>
    public async Task SetAnimationTask(int track, string animName, bool loop = false)
    {
        SetAnimation(track, animName, loop);
        if (loop) return;

        var tcs = new TaskCompletionSource<bool>();
        Callable callable = Callable.From<GodotObject, GodotObject, GodotObject>(
            (sprite, state, entry) => tcs.TrySetResult(true));

        _spineSprite.Connect("animation_completed", callable,
            (uint)GodotObject.ConnectFlags.OneShot);

        await tcs.Task;
    }

    /// <summary>
    /// 设置动画，等待播放完成后销毁自身。循环动画会立即返回且不销毁。
    /// </summary>
    public async Task SetAnimationAndFreeOnEndTask(int track, string animName, bool loop = false)
    {
        SetAnimation(track, animName, loop);
        if (loop) return;

        var tcs = new TaskCompletionSource<bool>();
        Callable callable = Callable.From<GodotObject, GodotObject, GodotObject>(
            (sprite, state, entry) => tcs.TrySetResult(true));

        _spineSprite.Connect("animation_completed", callable,
            (uint)GodotObject.ConnectFlags.OneShot);

        await tcs.Task;

        QueueFree();
    }
    /// <summary>
    /// 播放动画，并等待指定事件触发后返回。
    /// loop 为 true 时也会等待事件，不会立即返回。
    /// </summary>
    /// <param name="track">轨道</param>
    /// <param name="animName">动画名</param>
    /// <param name="eventName">要等待的事件名</param>
    /// <param name="loop">是否循环播放</param>
    /// <returns>事件触发时返回 remaining（和 WaitForAnimEvent 一致）</returns>
    public async Task<float> SetAnimationAndWaitEvent(
        int track,
        string animName,
        string eventName,
        bool loop = false)
    {
        var tcs = new TaskCompletionSource<float>();

        void Handler(string name, float remaining)
        {
            if (name == eventName)
                tcs.TrySetResult(remaining);
        }

        OnSpineAnimEvent += Handler;
        try
        {
            SetAnimation(track, animName, loop);
            return await tcs.Task;
        }
        finally
        {
            OnSpineAnimEvent -= Handler;
        }
    }
    /// <summary>
    /// 播放动画，等待指定事件触发后销毁自身。
    /// loop 为 true 时也会等待事件，不会立即返回。
    /// </summary>
    /// <param name="track">轨道</param>
    /// <param name="animName">动画名</param>
    /// <param name="eventName">要等待的事件名</param>
    /// <param name="loop">是否循环播放</param>
    public async Task SetAnimationAndFreeOnEvent(
        int track,
        string animName,
        string eventName,
        bool loop = false)
    {
        var tcs = new TaskCompletionSource<float>();

        void Handler(string name, float remaining)
        {
            if (name == eventName)
                tcs.TrySetResult(remaining);
        }

        OnSpineAnimEvent += Handler;
        try
        {
            SetAnimation(track, animName, loop);
            await tcs.Task;
        }
        finally
        {
            OnSpineAnimEvent -= Handler;
        }

        QueueFree();
    }
    /// <summary>
    /// 获得动画的时长
    /// </summary>
    /// <param name="animName">动画名称</param>
    /// <returns></returns>
    public float GetAnimationDuration(string animName)
    {
        return _spineSprite.Call("get_animation_duration", animName).AsSingle();
    }
}
