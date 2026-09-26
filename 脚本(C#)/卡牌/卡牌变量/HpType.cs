using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Card;

/// <summary>
/// 生命值类型注册属性
/// </summary>
/// <param name="parser">这个参数用于从json中解析该HpType</param>
[AttributeUsage(AttributeTargets.Class,AllowMultiple = false)]
public class GHpTypeAttribute : Attribute
{
}
[GHpType]
/// <summary>
/// 生命值类型
/// </summary>
public class HpType
{
    /// <summary>
    /// 生命图标
    /// </summary>
    public virtual Texture2D Icon { get; }
    /// <summary>
    /// 额外图标
    /// </summary>
    public virtual Texture2D AddtiveIcon { get; }
    /// <summary>
    /// 持续的生命值效果
    /// </summary>
    public Buff Action { get; set; }

    /// <summary>
    /// 注册逻辑
    /// </summary>
    private static List<Func<string, (bool,HpType)>> Parsers;
    private static void GetHpTypes()
    {
        if (Parsers != null) return;
        Parsers = new();
        var types = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t=>t.IsSubclassOf(typeof(HpType)))
            .Where(t=>t.GetCustomAttribute<GHpTypeAttribute>() != null)
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
            if (parser(parserString).Item1)
                return parser(parserString).Item2;
        }
        return null;
    }
}
