using Godot;
using System;
using System.Collections.Generic;

namespace Save;

public static class RunHelper
{
    /// <summary>生成快照</summary>
    public static IRunSerializable Build(string key, IRunSaveProp prop)
    {
        return prop switch
        {
            IntProp p => new IntSerializable(key, p.ValueProp),
            FloatProp p => new FloatSerializable(key, p.ValueProp),
            StringProp p => new StringSerializable(key, p.ValueProp),
            LongProp p => new LongSerializable(key, p.ValueProp),
            BoolProp p => new BoolSerializable(key, p.ValueProp),
            UIntProp p => new UIntSerializable(key, p.ValueProp),
            ListProp p => new ListSerializable(key, p.ValueProp),
            DictionaryProp p => new DictionarySerializable(key, p.ValueProp),
            _ => throw new NotSupportedException($"Unknown SaveProp：{prop.GetType().Name}")
        };
    }
    // Json → Dictionary<string,IRunSerializable>
    public static Dictionary<string, IRunSerializable> LoadRootJson(string jsonText)
    {
        Variant rootVariant = Json.ParseString(jsonText);
        if (rootVariant.VariantType != Variant.Type.Dictionary)
            throw new FormatException("Root must be json object");

        Godot.Collections.Dictionary rootDict = rootVariant.As<Godot.Collections.Dictionary>();
        var result = new Dictionary<string, IRunSerializable>();
        foreach (var kv in rootDict)
        {
            string outerKey = (string)kv.Key;
            IRunSerializable snap = ParseRawVariant(outerKey, kv.Value);
            result[outerKey] = snap;
        }
        return result;
    }

    public static IRunSerializable ParseRawVariant(string key, Variant variant)
    {
        switch (variant.VariantType)
        {
            case Variant.Type.Dictionary:
                {
                    Godot.Collections.Dictionary godotDict = variant.As<Godot.Collections.Dictionary>();
                    if (godotDict.ContainsKey("$LongHalfUp") && godotDict.ContainsKey("$LongHalfDown"))
                    {
                        uint high = (uint)godotDict["$LongHalfUp"].AsInt64();
                        uint low = (uint)godotDict["$LongHalfDown"].AsInt64();
                        ulong combined = ((ulong)high << 32) | low;
                        long value = (long)combined;
                        LongProp lp = new LongProp(value);
                        return Build(key, lp);
                    }
                    var entryList = new List<(string Key, IRunSaveProp SaveProp)>();
                    DictionaryProp dictProp = new DictionaryProp(entryList);
                    foreach (var kv in godotDict)
                    {
                        string childKey = kv.Key.AsString();
                        var childSnap = ParseRawVariant(childKey, kv.Value);
                        dictProp.ValueProp.Add((childSnap.Key, childSnap.Snapshot()));
                    }
                    return Build(key, dictProp);
                }
            case Variant.Type.Array:
                {
                    Godot.Collections.Array arr = variant.As<Godot.Collections.Array>();
                    var entryList = new List<IRunSaveProp>();
                    ListProp listProp = new ListProp(entryList);
                    foreach (var itemVar in arr)
                    {
                        var childSnap = ParseRawVariant("", itemVar);
                        listProp.ValueProp.Add(childSnap.Snapshot());
                    }
                    return Build(key, listProp);
                }
            case Variant.Type.String:
                {
                    StringProp strProp = new StringProp(variant.AsString());
                    return Build(key, strProp);
                }
            case Variant.Type.Bool:
                {
                    BoolProp boolProp = new BoolProp(variant.AsBool());
                    return Build(key, boolProp);
                }
            case Variant.Type.Int:
                {
                    long val = variant.AsInt64();
                    if (val >= uint.MinValue && val <= uint.MaxValue)
                    {
                        UIntProp uintProp = new UIntProp((uint)val);
                        return Build(key, uintProp);
                    }
                    if (val >= int.MinValue && val <= int.MaxValue)
                    {
                        IntProp intProp = new IntProp((int)val);
                        return Build(key, intProp);
                    }
                    throw new NotSupportedException($"[{val}] Wrong，long Must Be $LongHalfUp && $LongHalfDown Packed，Key={key}");
                }
            case Variant.Type.Float:
                {
                    double dVal = variant.AsDouble();
                    if (Math.Abs(dVal - Math.Round(dVal)) < double.Epsilon)
                    {
                        long longVal = (long)Math.Round(dVal);
                        if (longVal >= uint.MinValue && longVal <= uint.MaxValue)
                        {
                            UIntProp uintProp = new UIntProp((uint)longVal);
                            return Build(key, uintProp);
                        }
                        if (longVal >= int.MinValue && longVal <= int.MaxValue)
                        {
                            IntProp intProp = new IntProp((int)longVal);
                            return Build(key, intProp);
                        }
                        LongProp longProp = new LongProp(longVal);
                        return Build(key, longProp);
                    }
                    FloatProp floatProp = new FloatProp((float)dVal);
                    return Build(key, floatProp);
                }
            default:
                throw new NotSupportedException($"VariantType[{variant.VariantType}] not supported, Key:{key}");
        }
    }


    /// <summary>字符串转义，防止json损坏</summary>
    public static string EscapeJson(this string str)
    {
        if (str is null)
            return string.Empty;
        return str
            .Replace(@"\", @"\\")
            .Replace(@"""", @"\""")
            .Replace("\n", @"\n")
            .Replace("\r", @"\r");
    }
}
