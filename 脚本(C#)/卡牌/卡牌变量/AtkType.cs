using Battle;
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
public class AtkType
{
    /// <summary>
    /// 攻击力图标
    /// </summary>
    public virtual string IconSkelPath { get; }
    /// <summary>
    /// 攻击力额外标签(显示在卡牌的描述上,支持所有CardString描述的语法)
    /// </summary>
    public virtual string ExLabel { get; } = null;
    /// <summary>
    /// 攻击类型赋予的效果
    /// </summary>
    public Buff Buff { get; init; }
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
