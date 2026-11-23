using Card;
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Logger;

/// <summary>
/// 发送日志的类
/// </summary>
public static class Log
{
    public static int DebugId = 0;
    /// <summary>
    /// 发送错误日志
    /// </summary>
    /// <param name="content">发送的内容</param>
    public static void Error(string content)
    {
        GD.PrintErr($"[{TimingPath}] {content}");
    }
    /// <summary>
    /// 发送用于修复bug的日志
    /// </summary>
    /// <param name="content">发送的内容</param>
    /// <param name="debugId">修复bug的代号,需要和DebugId相同才会发送日志</param>
    public static void Debug(string content, int debugId)
    {
        if (debugId == DebugId)
            GD.Print($"[{TimingPath}] {content}");
    }
    /// <summary>
    /// 发送带调用行号的调试日志（无参数）
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void DebugL()
    {
        var frame = new StackFrame(1, true);
        Debug($"line {frame.GetFileLineNumber()}", DebugId);
    }

    private static readonly List<string> _timingBook = new();

    private static readonly List<string> _pendingNames = new();
    private static string _pendingKey;
    private static long _pendingFirstMs;
    private const long BufferWindowMs = 100;

    public static string TimingPath
        => _timingBook.Count == 0 ? "-" : string.Join(" > ", _timingBook);

    public static string CurrentTiming
        => _timingBook.Count == 0 ? null : _timingBook[^1];

    public static void EnterTiming(CardModel cardModel, object timing, params object[] args)
    {
        var formatted = FormatTiming(timing, args);
        _timingBook.Add(formatted);

        var name = cardModel?.GetType().Name ?? "null";
        var now = (long)Time.GetTicksMsec();

        if (_pendingNames.Count > 0)
        {
            if (_pendingKey != formatted || now - _pendingFirstMs > BufferWindowMs)
                FlushPending();
        }

        if (_pendingNames.Count == 0)
        {
            _pendingKey = formatted;
            _pendingFirstMs = now;
        }

        _pendingNames.Add(name);
    }

    public static void ExitTiming()
    {
        if (_timingBook.Count > 0)
            _timingBook.RemoveAt(_timingBook.Count - 1);
    }

    public static void FlushTiming() => FlushPending();

    private static void FlushPending()
    {
        if (_pendingNames.Count == 0) return;

        var names = new string[_pendingNames.Count];
        for (int i = 0; i < _pendingNames.Count; i++)
            names[i] = $"[{_pendingNames[i]}]";

        GD.Print($"[Timing] : {string.Join(",", names)} -> {_pendingKey}");

        _pendingNames.Clear();
        _pendingKey = null;
        _pendingFirstMs = 0;
    }

    public static TimingScope BeginTiming(CardModel cardModel, object timing, params object[] args)
    {
        EnterTiming(cardModel, timing, args);
        return new TimingScope();
    }

    public readonly struct TimingScope : IDisposable
    {
        public void Dispose() => ExitTiming();
    }

    private static string FormatTiming(object timing, object[] args)
    {
        if (args == null || args.Length == 0) return timing?.ToString() ?? "-";
        var parts = new string[args.Length];
        for (int i = 0; i < args.Length; i++)
            parts[i] = FormatArg(args[i]);
        return $"{timing}({string.Join(", ", parts)})";
    }

    private static string FormatArg(object arg)
    {
        if (arg == null) return "null";
        var type = arg.GetType();
        if (type.IsPrimitive || arg is string || arg is decimal) return arg.ToString();
        if (arg is IEnumerable en)
        {
            var items = new List<string>();
            foreach (var item in en)
            {
                if (items.Count >= 8) { items.Add("..."); break; }
                items.Add(FormatArg(item));
            }
            return $"[{string.Join(", ", items)}]";
        }
        var prop = type.GetProperty("Title") ?? type.GetProperty("Name");
        if (prop != null)
        {
            try
            {
                var v = prop.GetValue(arg);
                if (v != null && !ReferenceEquals(v, arg)) return $"{type.Name}({v})";
            }
            catch { }
        }
        return type.Name;
    }
}