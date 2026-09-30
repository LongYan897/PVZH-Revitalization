using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Card;
/// <summary>
/// 攻击力类型注册属性
/// </summary>
/// <param name="parser">这个参数用于从json中解析该AtkType</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class GStarTypeAttribute : Attribute
{
}
[GStarType]
/// <summary>
/// 攻击力类型
/// </summary>
public class StarType
{
    /// <summary>
    /// 等级图标
    /// </summary>
    public virtual string IconSkelPath { get; }

    /// <summary>
    /// 注册逻辑
    /// </summary>
    private static List<Func<string, (bool, StarType)>> Parsers;
    private static void GetStarTypes()
    {
        if (Parsers != null) return;
        Parsers = new();
        var types = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(AtkType)))
            .Where(t => t.GetCustomAttribute<GStarTypeAttribute>() != null)
            .ToList();

        foreach (var type in types)
        {
            var atr = type.GetCustomAttribute<GStarTypeAttribute>();
            if (atr != null)
            {
                var parseMethod = type.GetMethods()
                    .FirstOrDefault(m => m.Name == "Parse" && m.IsStatic && m.GetParameters().Length == 1);
                if (parseMethod == null) continue;

                Parsers.Add(str =>
                {
                    object raw = parseMethod.Invoke(null, new object[] { str });
                    return ((bool, StarType))raw;
                });
            }
        }
    }
    public static void Init() => GetStarTypes();
    /// <summary>
    /// 解析字符串为StarType<br/>
    /// Parser输出的第一个bool代表是否要解析该字符串,为false时直接跳过不使用第二个值<br/>
    /// Parser输出的第二个值为StarType<br/>
    /// 解析的时候统一格式为输入|StarType的名称:StarType的数值|<br/>
    /// 例:<br/>
    ///     输入 |致命|<br/>
    ///     代表致命<br/>
    /// /// </summary>
    /// <param name="parserString">解析字符串</param>
    /// <returns>得到的AtkType</returns>
    public static StarType Parser(string parserString)
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
