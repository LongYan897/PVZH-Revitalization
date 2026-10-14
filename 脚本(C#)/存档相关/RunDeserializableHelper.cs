using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Save;

public static class RunDeserializeHelper
{
    public static T LoadSource<T>(this string json, string runRootKey, string battleRootKey) where T : new()
    {
        T instance = new T();

        if (!string.IsNullOrEmpty(runRootKey))
        {
            InternalPopulateFromJson(instance, json, runRootKey, SavedType.Run);
        }
        if (!string.IsNullOrEmpty(battleRootKey))
        {
            InternalPopulateFromJson(instance, json, battleRootKey, SavedType.Battle);
        }
        return instance;
    }
    public static T LoadTwoSource<T>(string runJson, string runRootKey, string battleJson, string battleRootKey) where T : new()
    {
        T instance = new T();

        if (!string.IsNullOrEmpty(runJson))
        {
            InternalPopulateFromJson(instance, runJson, runRootKey, SavedType.Run);
        }
        if (!string.IsNullOrEmpty(battleJson))
        {
            InternalPopulateFromJson(instance, battleJson, battleRootKey, SavedType.Battle);
        }
        return instance;
    }

    private static void InternalPopulateFromJson<T>(T instance, string jsonText, string rootKey, SavedType savedType)
    {
        if (string.IsNullOrEmpty(jsonText))
            return;

        Variant rootVariant = Json.ParseString(jsonText);
        if (rootVariant.VariantType != Variant.Type.Dictionary)
            throw new FormatException("Must (Dictionary)");

        Dictionary rootDict = rootVariant.As<Dictionary>();
        if (!rootDict.ContainsKey(rootKey))
            throw new Exception($"Cannot Find Key From Save：{rootKey}");

        Variant innerVariant = rootDict[rootKey];
        if (innerVariant.VariantType != Variant.Type.Dictionary)
            throw new FormatException($"{rootKey} Must Contains Object");

        Dictionary innerDict = innerVariant.As<Dictionary>();
        Populate(instance, innerDict, savedType);
    }
    private static void Populate(object target, Dictionary dataDict, SavedType savedType)
    {
        Type targetType = target.GetType();
        List<MemberBinding> bindings = new List<MemberBinding>();

        Type scanType = targetType;
        while (scanType != null && scanType != typeof(object))
        {
            var fields = scanType.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                var runAttr = f.GetCustomAttribute<RunSavedAttribute>();
                var battleAttr = f.GetCustomAttribute<BattleSavedAttribute>();

                if (runAttr != null && battleAttr != null)
                {
                    throw new InvalidOperationException($"Field {f.Name} Cannot Have both RunSaved / BattleSaved");
                }

                string jsonKey = null;
                bool include = false;

                if (savedType == SavedType.Run && runAttr != null)
                {
                    jsonKey = string.IsNullOrWhiteSpace(runAttr.Key) ? f.Name : runAttr.Key;
                    include = true;
                }
                else if (savedType == SavedType.Battle && battleAttr != null)
                {
                    jsonKey = string.IsNullOrWhiteSpace(battleAttr.Key) ? f.Name : battleAttr.Key;
                    include = true;
                }

                if (include)
                {
                    bindings.Add(new MemberBinding
                    {
                        JsonKey = jsonKey,
                        MemberType = f.FieldType,
                        SetValue = (inst, val) => f.SetValue(inst, val)
                    });
                }
            }

            var props = scanType.GetProperties(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.CanRead && p.CanWrite);
            foreach (var p in props)
            {
                var runAttr = p.GetCustomAttribute<RunSavedAttribute>();
                var battleAttr = p.GetCustomAttribute<BattleSavedAttribute>();

                if (runAttr != null && battleAttr != null)
                {
                    throw new InvalidOperationException($"Property {p.Name} Cannot Have both RunSaved / BattleSaved");
                }

                string jsonKey = null;
                bool include = false;

                if (savedType == SavedType.Run && runAttr != null)
                {
                    jsonKey = string.IsNullOrWhiteSpace(runAttr.Key) ? p.Name : runAttr.Key;
                    include = true;
                }
                else if (savedType == SavedType.Battle && battleAttr != null)
                {
                    jsonKey = string.IsNullOrWhiteSpace(battleAttr.Key) ? p.Name : battleAttr.Key;
                    include = true;
                }

                if (include)
                {
                    bindings.Add(new MemberBinding
                    {
                        JsonKey = jsonKey,
                        MemberType = p.PropertyType,
                        SetValue = (inst, val) => p.SetValue(inst, val)
                    });
                }
            }

            scanType = scanType.BaseType;
        }

        foreach (var binding in bindings)
        {
            Variant val = default;
            bool found = false;
            foreach (var kv in dataDict)
            {
                if (kv.Key.AsString() == binding.JsonKey)
                {
                    val = kv.Value;
                    found = true;
                    break;
                }
            }
            if (!found)
                continue;

            Type memberType = binding.MemberType;

            if (memberType == typeof(long) && val.VariantType == Variant.Type.Dictionary)
            {
                Dictionary packDict = val.As<Dictionary>();
                uint? high = null;
                uint? low = null;
                foreach (var kv in packDict)
                {
                    string k = kv.Key.AsString();
                    if (k == "$LongHalfUp") high = (uint)kv.Value;
                    if (k == "$LongHalfDown") low = (uint)kv.Value;
                }
                if (high.HasValue && low.HasValue)
                {
                    ulong combined = ((ulong)high.Value << 32) | low.Value;
                    long result = (long)combined;
                    binding.SetValue(target, result);
                    continue;
                }
            }

            if (val.VariantType == Variant.Type.Dictionary)
            {
                Dictionary childDict = val.As<Dictionary>();
                object childInstance = Activator.CreateInstance(memberType);
                Populate(childInstance, childDict, savedType);
                binding.SetValue(target, childInstance);
            }
            else if (val.VariantType == Variant.Type.Array)
            {
                Array<Variant> arr = val.As<Array<Variant>>();
                Type elementType = memberType.GetGenericArguments()[0];
                object listInstance = Activator.CreateInstance(memberType);
                MethodInfo addMethod = listInstance.GetType().GetMethod("Add");

                foreach (var itemVariant in arr)
                {
                    object itemValue;
                    if (elementType == typeof(long) && itemVariant.VariantType == Variant.Type.Dictionary)
                    {
                        Dictionary packDict = itemVariant.As<Dictionary>();
                        uint? high = null;
                        uint? low = null;
                        foreach (var kv in packDict)
                        {
                            string k = kv.Key.AsString();
                            if (k == "$LongHalfUp") high = (uint)kv.Value;
                            if (k == "$LongHalfDown") low = (uint)kv.Value;
                        }
                        if (!high.HasValue || !low.HasValue)
                            throw new FormatException("long packed missing $LongHalfUp/$LongHalfDown");

                        ulong combined = ((ulong)high.Value << 32) | low.Value;
                        itemValue = (long)combined;
                    }
                    else if (itemVariant.VariantType == Variant.Type.Dictionary)
                    {
                        Dictionary itemDict = itemVariant.As<Dictionary>();
                        object itemObj = Activator.CreateInstance(elementType);
                        Populate(itemObj, itemDict, savedType);
                        itemValue = itemObj;
                    }
                    else
                    {
                        itemValue = ConvertVariant(itemVariant, elementType);
                    }
                    addMethod.Invoke(listInstance, new[] { itemValue });
                }
                binding.SetValue(target, listInstance);
            }
            else
            {
                object rawValue = ConvertVariant(val, memberType);
                binding.SetValue(target, rawValue);
            }
        }
    }

    // 放在类内部
    private sealed class MemberBinding
    {
        public string JsonKey { get; set; }
        public Type MemberType { get; set; }
        public Action<object, object> SetValue { get; set; }
    }


    private static object ConvertVariant(Variant variant, Type targetType)
    {
        if (targetType == typeof(int))
            return variant.AsInt32();
        if (targetType == typeof(long))
            return variant.AsInt64();
        if (targetType == typeof(uint))
            return (uint)variant.AsInt64();
        if (targetType == typeof(float))
            return (float)variant;
        if (targetType == typeof(string))
            return variant.AsString();
        if (targetType == typeof(bool))
            return variant.AsBool();

        throw new NotSupportedException($"UnknownType : {targetType.Name}");
    }
}
