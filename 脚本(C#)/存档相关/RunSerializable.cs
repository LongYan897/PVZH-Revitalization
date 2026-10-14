using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frenemypillar.Script.RunSet;

public abstract class RunSerializable<T> : IRunSerializable where T : IRunSaveProp
{
    public abstract string Key { get; }
    public Type DataType => typeof(T);

    public static TOut CreateSnap<TOut>() where TOut : IRunSerializable, new()
    {
        return new TOut();
    }
    /// <summary>快照承载的数据</summary>
    protected abstract T SnapshotData { get; }

    public T Snap => SnapshotData;

    public IRunSaveProp Snapshot() => SnapshotData;

    /// <summary>快照转为运行时RunProps</summary>
    public abstract IRunProps ToProps();

    /// <summary>输出标准格式Json字符串</summary>
    public string ToJsonPair(bool onlyValue = false)
    {
        if (SnapshotData is DictionaryProp dictionaryProp)
        {
            List<string> childFrags = new List<string>();
            foreach (var entry in dictionaryProp.ValueProp)
            {
                IRunSerializable childSnap = RunHelper.Build(entry.Key, entry.SaveProp);
                childFrags.Add(childSnap.ToJsonPair());
            }
            string body = string.Join(",", childFrags);
            return onlyValue ? $"{{{body}}}" : $"\"{ Key}\":{{{body}}}";
        }
        else if (SnapshotData is ListProp listProp)
        {
            List<string> childFrags = new List<string>();
            foreach (var entry in listProp.ValueProp)
            {
                IRunSerializable childSnap = RunHelper.Build("", entry);
                childFrags.Add(childSnap.ToJsonPair(true));
            }
            string body = string.Join(",", childFrags);
            return onlyValue ? $"[{body}]" : $"\"{Key}\":[{body}]";
        }
        else if (SnapshotData is StringProp stringProp)
        {
            string esc = stringProp.ValueProp.EscapeJson();
            string val = $"\"{ esc}\"";
            return onlyValue ? val : $"\"{ Key}\":{val}";
        }
        else if (SnapshotData is LongProp longProp)
        {
            long raw = longProp.ValueProp;
            ulong u = (ulong)raw;
            uint high = (uint)(u >> 32);
            uint low = (uint)u;
            string body = $"\"$LongHalfUp\":{high},\"$LongHalfDown\":{low}";
            string objText = $"{{{body}}}";
            return onlyValue ? objText : $"\"{ Key}\":{objText}";
        }
        else
        {
            return onlyValue ? $"{SnapshotData.Value}" : $"\"{Key}\":{SnapshotData.Value}";
        }
    }

    public string ToJson()
    {
        return $"{{{ToJsonPair()}}}";
    }
}