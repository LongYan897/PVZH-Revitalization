using System;

namespace Pack;


/// <summary>
/// 用于给卡牌描述的URL提供自定义参数（仅支持 static 方法）<br/>
/// 方法签名必须为：static string Xxx(CardModel model)<br/>
/// 占位符 {#/&amp;:方法名} 会调用该方法，用返回值替换<br/>
/// <para>AcceptEffects：</para>
/// - true（默认）：接受 <c>#</c> / <c>&amp;</c> 带来的额外效果，<c>#</c> 会包成 <c>$值$</c>，<c>&amp;</c> 直接输出<br/>
/// - false：忽略此效果，无论 <c>#</c> 还是 <c>&amp;</c> 都输出纯文本<br/>
/// 无论哪种，占位符必须声明对应分隔符（如 <c>{#/&amp;:Camp}</c>）才会被替换<br/>
/// 返回 null 时占位符原样保留；n / x 是保留字，不会被覆盖
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class UrlParamsAttribute : Attribute
{
    public bool AcceptEffects { get; }
    public UrlParamsAttribute(bool acceptEffects = true) => AcceptEffects = acceptEffects;
}

/// <summary>
/// 用于给卡牌描述的URL设置描述<br/>
/// 比如<br/>
/// 我希望给致命设置描述<br/>
/// 那么可以在某个类里面写<br/>
/// [URL]<br/>
/// public string Lethal => "攻击到的单位立即死亡"<br/>
/// 这个就代表了 _Lethal_ 这个标签点开后显示 "攻击到的单位立即死亡"<br/>
/// static的属性也可以这样注册<br/>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class URLAttribute : Attribute
{

}