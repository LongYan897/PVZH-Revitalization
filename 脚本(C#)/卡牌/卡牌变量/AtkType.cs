using Godot;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 攻击力类型注册属性
/// </summary>
/// <param name="parser">这个参数用于从json中解析该AtkType</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class GAtkTypeAttribute : Attribute
{
}

[GAtkType]
/// <summary>
/// 攻击力类型
/// </summary>
public abstract class AtkType
{
    public AtkType Clone() => MemberwiseClone() as AtkType;
    public Texture2D Icon => IconPath != null ? IconPath.Load<Texture2D>() : null;
    /// <summary>
    /// 攻击力动画图标
    /// </summary>
    public abstract string IconSkelPath { get; }
    /// <summary>
    /// 攻击力图标
    /// </summary>
    public abstract string IconPath { get; }
    /// <summary>
    /// 攻击力额外标签(显示在卡牌的描述上,支持所有CardString描述的语法)
    /// </summary>
    public abstract string ExLabel { get; }
    /// <summary>
    /// 攻击类型赋予的效果
    /// </summary>
    public abstract Buff Buff { get; }
    /// <summary>
    /// 攻击类型的执行优先级，数值越大越先执行
    /// </summary>
    public virtual int Priority => Buff?.Priority ?? int.MinValue;

    public virtual bool IsMerged => false;

    public virtual IReadOnlyList<AtkType> Flatten() => new[] { this };

    public static AtkType Merge(params AtkType[] atkTypes)
        => Merge((IEnumerable<AtkType>)atkTypes);

    public static AtkType Merge(IEnumerable<AtkType> atkTypes)
    {
        if (atkTypes == null) return null;

        var flat = atkTypes
            .Where(t => t != null)
            .SelectMany(t => t.Flatten())
            .Distinct()
            .OrderByDescending(t => t.Priority)
            .ToList();

        return flat.Count switch
        {
            0 => null,
            1 => flat[0],
            _ => new MergedAtkType(flat)
        };
    }

    public AtkType MergeWith(params AtkType[] others)
        => Merge(new[] { this }.Concat(others ?? Array.Empty<AtkType>()));

    /// <summary>
    /// 注册逻辑
    /// </summary>
    private static List<Func<string, (bool, AtkType)>> Parsers;
    private static void GetAtkTypes()
    {
        if (Parsers != null) return;
        Parsers = new();
        var types = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(AtkType)))
            .Where(t => t.GetCustomAttribute<GAtkTypeAttribute>() != null)
            .ToList();

        foreach (var type in types)
        {
            var atr = type.GetCustomAttribute<GAtkTypeAttribute>();
            if (atr != null)
            {
                var parseMethod = type.GetMethods()
                    .FirstOrDefault(m => m.Name == "Parse" && m.IsStatic && m.GetParameters().Length == 1);
                if (parseMethod == null) continue;

                Parsers.Add(str =>
                {
                    object raw = parseMethod.Invoke(null, new object[] { str });
                    return ((bool, AtkType))raw;
                });
            }
        }
    }
    public static void Init() => GetAtkTypes();
    /// <summary>
    /// 解析字符串为AtkType<br/>
    /// Parser输出的第一个bool代表是否要解析该字符串,为false时直接跳过不使用第二个值<br/>
    /// Parser输出的第二个值为AtkType<br/>
    /// 解析的时候统一格式为输入|AtkType的名称:AtkType的数值|<br/>
    /// 例:<br/>
    ///     输入 |致命|<br/>
    ///     代表致命<br/>
    /// /// </summary>
    /// <param name="parserString">解析字符串</param>
    /// <returns>得到的AtkType</returns>
    public static AtkType Parser(string parserString)
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
/// 合并的攻击类型
/// </summary>
public sealed class MergedAtkType(IEnumerable<AtkType> types) : AtkType
{
    public const string MergedIconSkelPath = "res://素材(C#)/文本图片/Icon_Multi.png";

    private readonly List<AtkType> _types = types?
        .Where(t => t != null)
        .OrderByDescending(t => t.Priority)
        .ToList() ?? new List<AtkType>();

    private Buff iBuff;

    /// <summary>包含的子攻击类型</summary>
    public IReadOnlyList<AtkType> AtkTypes => _types;

    public override bool IsMerged => true;

    public override int Priority => _types.Count > 0 ? _types[0].Priority : int.MinValue;

    public override IReadOnlyList<AtkType> Flatten()
        => _types.SelectMany(t => t.Flatten()).Distinct().ToList();

    public override string IconSkelPath => MergedIconSkelPath;

    public override string ExLabel =>
        string.Join("，", _types.Select(t => t.ExLabel).Where(s => !string.IsNullOrEmpty(s)));

    public override Buff Buff => iBuff ??= BuildBuff(_types);

    public override string IconPath => null;

    private static Buff BuildBuff(IReadOnlyList<AtkType> types)
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