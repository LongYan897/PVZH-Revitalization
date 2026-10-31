using Godot;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Card;

/// <summary>
/// 生命值类型注册属性
/// </summary>
/// <param name="parser">这个参数用于从json中解析该HpType</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class GHpTypeAttribute : Attribute
{
}
[GHpType]
/// <summary>
/// 生命值类型
/// </summary>
public abstract class HpType
{
    public Texture2D Icon => IconPath != null ? IconPath.Load<Texture2D>() : null;
    /// <summary>
    /// 生命动画图标
    /// </summary>
    public abstract string IconSkelPath { get; }
    /// <summary>
    /// 生命图标
    /// </summary>
    public abstract string IconPath { get; }
    /// <summary>
    /// 生命额外标签(显示在卡牌的描述上,支持所有CardString描述的语法)
    /// </summary>
    public abstract string ExLabel { get; }
    /// <summary>
    /// 血量类型给予时的Buff
    /// </summary>
    public abstract Buff Buff { get; }
    /// <summary>
    /// 生命值类型的执行优先级，数值越大越先执行
    /// </summary>
    public virtual int Priority => Buff?.Priority ?? int.MinValue;
    /// <summary>
    /// 附加到战斗卡牌时用于增加过滤条件
    /// 比如锦囊免疫等效果
    /// </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    public virtual bool ExtraFilter(CardModel card)
    {
        return true;
    }

    public virtual bool IsMerged => false;

    public virtual IReadOnlyList<HpType> Flatten() => new[] { this };

    public static HpType Merge(params HpType[] hpTypes)
        => Merge((IEnumerable<HpType>)hpTypes);

    public static HpType Merge(IEnumerable<HpType> hpTypes)
    {
        if (hpTypes == null) return null;

        var flat = hpTypes
            .Where(t => t != null)
            .SelectMany(t => t.Flatten())
            .Distinct()
            .OrderByDescending(t => t.Priority)
            .ToList();

        return flat.Count switch
        {
            0 => null,
            1 => flat[0],
            _ => new MergedHpType(flat)
        };
    }

    public HpType MergeWith(params HpType[] others)
        => Merge(new[] { this }.Concat(others ?? Array.Empty<HpType>()));

    /// <summary>
    /// 注册逻辑
    /// </summary>
    private static List<Func<string, (bool, HpType)>> Parsers;
    private static void GetHpTypes()
    {
        if (Parsers != null) return;
        Parsers = new();
        var types = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(HpType)))
            .Where(t => t.GetCustomAttribute<GHpTypeAttribute>() != null)
            .ToList();

        foreach (var type in types)
        {
            var atr = type.GetCustomAttribute<GHpTypeAttribute>();
            if (atr != null)
            {
                var parseMethod = type.GetMethods()
                    .FirstOrDefault(m => m.Name == "Parse" && m.IsStatic && m.GetParameters().Length == 1);
                if (parseMethod == null) continue;

                Parsers.Add(str =>
                {
                    object raw = parseMethod.Invoke(null, new object[] { str });
                    return ((bool, HpType))raw;
                });
            }
        }
    }
    public static void Init() => GetHpTypes();

    /// <summary>
    /// 解析字符串为HpType<br/>
    /// Parser输出的第一个bool代表是否要解析该字符串,为false时直接跳过不使用第二个值<br/>
    /// Parser输出的第二个值为HpType<br/>
    /// 解析的时候统一格式为输入|HpType的名称:HpType的数值|<br/>
    /// 例:<br/>
    ///     输入 |装甲:2|<br/>
    ///     代表装甲2<br/>
    /// </summary>
    /// <param name="parserString">解析字符串</param>
    /// <returns>得到的HpType</returns>
    public static HpType Parser(string parserString)
    {
        if (Parsers == null) return null;
        foreach (var parser in Parsers)
        {
            if (parser == null) continue;
            var parsed = parser(parserString);
            if (parsed.Item1)
                return parsed.Item2;
        }
        return null;
    }
}

/// <summary>
/// 合并的生命值类型
/// </summary>
public sealed class MergedHpType(IEnumerable<HpType> types) : HpType
{
    public const string MergedIconSkelPath = "res://icons/hp_merged.tres";

    private readonly List<HpType> _types = types?
        .Where(t => t != null)
        .OrderByDescending(t => t.Priority)
        .ToList() ?? new List<HpType>();

    private Buff iBuff;

    /// <summary>包含的子生命值类型</summary>
    public IReadOnlyList<HpType> HpTypes => _types;

    public override bool IsMerged => true;

    public override int Priority => _types.Count > 0 ? _types[0].Priority : int.MinValue;

    public override IReadOnlyList<HpType> Flatten()
        => _types.SelectMany(t => t.Flatten()).Distinct().ToList();

    public override string IconSkelPath => MergedIconSkelPath;

    public override string ExLabel =>
        string.Join("，", _types.Select(t => t.ExLabel).Where(s => !string.IsNullOrEmpty(s)));

    public override Buff Buff => iBuff ??= BuildBuff(_types);

    public override string IconPath => null;

    public override bool ExtraFilter(CardModel card)
        => _types.All(t => t.ExtraFilter(card));

    private static Buff BuildBuff(IReadOnlyList<HpType> types)
    {
        var buffs = types
            .Select(t => t.Buff)
            .Where(b => b != null)
            .ToList();

        return buffs.Count switch
        {
            0 => null,
            1 => buffs[0],
            _ => new MergedBuff(buffs),
        };
    }
}