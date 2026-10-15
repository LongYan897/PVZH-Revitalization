using Godot;
using System.Collections.Generic;

namespace Controller.WavPlay;
/// <summary>
/// 音频角色类型。
/// </summary>
public enum Rhythm
{
    /// <summary>水平分段，参与音乐图切换。</summary>
    Segment,
    /// <summary>垂直分层，常驻播放，仅调音量。</summary>
    Layer,
    /// <summary>环境音，常驻循环。</summary>
    Ambient,
    /// <summary>一次性音效，播完即弃。</summary>
    SFX
}

/// <summary>
/// 动态音乐节点抽象基类，负责存储音频数据。
/// </summary>
public abstract class Wav
{
    /// <summary>音频文件路径。</summary>
    public abstract string Path { get; }

    /// <summary>音调偏移（半音），0 为原调。</summary>
    public abstract int InitTone { get; }
    /// <summary>
    /// 音调
    /// </summary>
    private int _tone;
    private bool _first = true;
    /// <summary>
    /// 音调偏移（半音），0 为原调。
    /// </summary>
    public int Tone
    {
        get
        {
            if (_first)
            {
                _first = false;
                _tone = InitTone;
            }
            return _tone;
        }
        set
        {
            _tone = value;
        }
    }
    /// <summary>音频角色类型。</summary>
    public abstract Rhythm Kind { get; }

    /// <summary>默认后继节点。</summary>
    public abstract Wav DefaultNext { get; }

    /// <summary>条件分支表：状态标识 → 后继节点。</summary>
    public abstract Dictionary<string, Wav> Branches { get; }

    /// <summary>当前播放器实例。</summary>
    protected AudioStreamPlayer Player;

    /// <summary>是否正在播放。</summary>
    public bool IsPlaying => Player != null && Player.Playing;

    /// <summary>
    /// 根据状态获取下一个音乐节点，默认返回 DefaultNext。
    /// </summary>
    /// <param name="state">游戏状态标识。</param>
    /// <returns>下一个音乐节点，或 null。</returns>
    public virtual Wav GetNext(string state = null)
    {
        if (!string.IsNullOrEmpty(state) && Branches.TryGetValue(state, out var wav))
            return wav;
        return DefaultNext;
    }

    /// <summary>
    /// 播放钩子，由 WavPlayer 反射调用。
    /// </summary>
    /// <param name="player">播放器实例。</param>
    /// <param name="fadeIn">淡入时长（秒）。</param>
    protected virtual void OnPlay(AudioStreamPlayer player, float fadeIn)
    {
        Player = player;
        var tween = player.CreateTween();
        tween.TweenProperty(player, "volume_db", 0f, fadeIn);
    }

    /// <summary>
    /// 切换钩子，由 WavPlayer 反射调用。
    /// </summary>
    /// <param name="crossfade">交叉淡化时长（秒）。</param>
    protected virtual void OnSwitch(float crossfade)
    {
    }

    /// <summary>
    /// 停止播放并淡出。
    /// </summary>
    /// <param name="fadeOut">淡出时长（秒）。</param>
    public virtual void Stop(float fadeOut = 0.5f)
    {
        if (Player == null) return;

        var tween = Player.CreateTween();
        tween.TweenProperty(Player, "volume_db", -80f, fadeOut);
        tween.TweenCallback(Callable.From(() =>
        {
            Player.Stop();
            Player.QueueFree();
            Player = null;
        }));
    }
}
