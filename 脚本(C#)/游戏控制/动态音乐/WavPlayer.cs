using Controller.WavPlay;
using Godot;
using System;
using System.Reflection;

namespace Controller;

/// <summary>
/// 音频播放器，统一管理音效、背景音乐与动态音乐节点的播放。
/// </summary>
public static class WavPlayer
{
    /// <summary>
    /// 播放动态音乐节点，内部创建 T 实例并调用其 OnPlay 钩子。
    /// </summary>
    /// <typeparam name="T">Wav 子类类型。</typeparam>
    /// <param name="args">构造参数。</param>
    /// <returns>创建的播放器实例，失败返回 null。</returns>
    public static AudioStreamPlayer Play<T>(params object[] args)
        where T : Wav
    {
        var wav = CreateWav<T>(args);
        if (wav == null) return null;

        var player = CreatePlayer(wav);
        if (player == null) return null;

        InvokeHook(wav, "OnPlay", player, 0.5f);
        return player;
    }

    /// <summary>
    /// 切换动态音乐节点，内部创建 T 实例并调用其 GetNext 与 OnSwitch 钩子。
    /// </summary>
    /// <typeparam name="T">Wav 子类类型。</typeparam>
    /// <param name="wav">当前节点。</param>
    /// <param name="state">游戏状态标识。</param>
    /// <param name="crossfade">交叉淡化时长（秒）。</param>
    /// <returns>切换后的节点，未切换则返回当前节点。</returns>
    public static T Advance<T>(T wav, string state, float crossfade = 0.8f)
        where T : Wav
    {
        if (wav == null) return null;

        var next = InvokeGetNext(wav, state);
        if (next == null || next == wav) return wav;

        wav.Stop(crossfade);
        InvokeHook(next, "OnSwitch", crossfade);

        return (T)next;
    }

    /// <summary>
    /// 设置并播放背景音乐，自动处理循环。
    /// </summary>
    /// <typeparam name="T">Wav 子类类型。</typeparam>
    /// <param name="args">构造参数。</param>
    /// <returns>创建的播放器实例，失败返回 null。</returns>
    public static AudioStreamPlayer SetBgMusic<T>(params object[] args)
        where T : Wav
    {
        var wav = CreateWav<T>(args);
        if (wav == null) return null;

        var player = CreatePlayer(wav);
        if (player == null) return null;

        if (player.Stream is AudioStreamWav wavStream)
        {
            wavStream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            wavStream.LoopBegin = 0;
            wavStream.LoopEnd = wavStream.Data.Length / 2;
        }
        else if (player.Stream is AudioStreamOggVorbis ogg)
        {
            ogg.Loop = true;
        }

        InvokeHook(wav, "OnPlay", player, 0.5f);
        return player;
    }

    /// <summary>
    /// 创建 T 实例。
    /// </summary>
    private static T CreateWav<T>(object[] args)
        where T : Wav
    {
        try
        {
            return (T)Activator.CreateInstance(typeof(T), args);
        }
        catch (Exception e)
        {
            GD.PushWarning($"WavPlayer: 创建 {typeof(T).Name} 失败 - {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 根据节点数据创建并启动播放器。
    /// </summary>
    private static AudioStreamPlayer CreatePlayer(Wav wav)
    {
        var stream = GD.Load<AudioStream>(wav.Path);
        if (stream == null) return null;

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = -80f,
            PitchScale = Mathf.Pow(2, wav.Tone / 12f)
        };

        Main.AudioContainer.AddChild(player);
        player.Play();
        return player;
    }

    /// <summary>
    /// 调用节点的 GetNext 钩子。
    /// </summary>
    private static Wav InvokeGetNext(Wav wav, string state)
    {
        var method = wav.GetType().GetMethod("GetNext",
            BindingFlags.Public | BindingFlags.Instance);
        return method?.Invoke(wav, new object[] { state }) as Wav;
    }

    /// <summary>
    /// 调用节点的无返回值钩子。
    /// </summary>
    private static void InvokeHook(Wav wav, string name, params object[] args)
    {
        var method = wav.GetType().GetMethod(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(wav, args);
    }
}