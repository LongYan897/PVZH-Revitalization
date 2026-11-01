using Godot;
using System.Collections.Generic;

namespace Controller.WavPlay;

/// <summary>
/// 通用"一次性音效"节点。<br/>
/// 用法：<c>WavPlayer.Play&lt;CustomWav&gt;(路径);</c><br/>
/// 特点：<br/>
/// - 一个实例对应一个 AudioStreamPlayer，连续触发会叠加，不会打断上一个；<br/>
/// - 播放结束（<see cref="AudioStreamPlayer.Finished"/>）后自动 QueueFree 播放器，
/// </summary>
public class CustomWav : Wav
{
    private readonly string _path;
    private readonly int _initTone;

    /// <param name="path">音频文件路径（res://...，支持 wav/mp3/ogg）</param>
    public CustomWav(string path) : this(path, 0)
    {
    }

    /// <param name="path">音频文件路径（res://...，支持 wav/mp3/ogg）</param>
    /// <param name="tone">音调偏移（半音），0 = 原调</param>
    public CustomWav(string path, int tone)
    {
        _path = path;
        _initTone = tone;
    }

    public override string Path => _path;

    public override int InitTone => _initTone;

    public override Rhythm Kind => Rhythm.SFX;

    public override Wav DefaultNext => null;

    public override Dictionary<string, Wav> Branches => [];

    /// <summary>一次性音效：不淡入，并且播完自动释放播放器</summary>
    protected override void OnPlay(AudioStreamPlayer player, float fadeIn)
    {
        // fadeIn 传 0：一次性短音效不需要淡入（Wav 默认 0.5s 淡入会把开头吃掉）
        base.OnPlay(player, 0f);

        player.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(player))
                player.QueueFree();
            Player = null;
        };
    }
}
