
namespace Variable.Special;

/// <summary>
/// 整数变量
/// </summary>
public class IntVariable(string sign,int baseValue) : Variable<int>(sign,baseValue)
{
    /// <summary>
    /// 增加值
    /// </summary>
    /// <param name="value">要增加的值</param>
    /// <param name="reason">原因</param>
    public void Gain(int value,VariableReason reason)
    {
        Record(Current, Current + value, reason);
        Current = Current + value;
    }
    /// <summary>
    /// 减少值
    /// </summary>
    /// <param name="value">要减少的值</param>
    /// <param name="reason">原因</param>
    public void Lose(int value,VariableReason reason)
    {
        Record(Current, Current - value, reason);
        Current = Current - value;
    }
    /// <summary>
    /// 是否是相较于Base的值减少
    /// </summary>
    public bool NegitiveChanged => HasChanged && Current - Base < 0;
    /// <summary>
    /// 是否是相较于Base的值增加
    /// </summary>
    public bool PositiveChanged => HasChanged && Current - Base > 0;
}
