using Frenemypillar.Script.Save;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Frenemypillar.Script.RunSet;

public class RunHistory
{
    private RunHistory() { }

    private ConcurrentDictionary<string, IRunProps> _runtimeProps = new ();

    public static RunHistory Current;

    /// <summary>新建对局初始化</summary>
    public static void RecordStart()
    {
        Current = new RunHistory();
    }

    /// <summary>写入内存缓存</summary>
    public static void Record(IRunProps props)
    {

        Current._runtimeProps[props.Name] = props;
    }

    /// <summary>从内存读取</summary>
    public static T Get<T>(string key) where T : IRunProps
    {
        if (Current._runtimeProps.TryGetValue(key, out var val))
            return (T)val;
        return default;
    }

    /// <summary>保存历程 → 输出完整Json文本（供存档写入）</summary>
    public string ExportToJson()
    {
        List<string> entries = new List<string>();
        foreach (var kv in _runtimeProps)
        {
            var serial = kv.Value.ToSerializable();
            entries.Add(serial.ToJsonPair());
        }
        return "{" + string.Join(",", entries) + "}";
    }

    /// <summary>加载历程Json，填充进运行缓存</summary>
    public void ImportFromJson(string jsonText)
    {
        _runtimeProps.Clear();
        var dict = RunHelper.LoadRootJson(jsonText);
        foreach (var kv in dict)
        {
            var prop = kv.Value.ToProps();
            _runtimeProps[kv.Key] = prop;
        }
    }

    public static void RecordEnd()
    {
        Current = null;
    }
}
