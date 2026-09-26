
namespace Variable;

/// <summary>
/// 变量的历史
/// </summary>
public class VariableHistory(int Index,object Before,object After,VariableReason Reason)
{
    /// <summary>
    /// 历史的顺序 越大代表修改事件越晚
    /// </summary>
    public readonly int Index = Index;
    /// <summary>
    /// 该历史时期修改变量前的值
    /// </summary>
    public readonly object Before = Before;
    /// <summary>
    /// 该历史时期修改变量后的值
    /// </summary>
    public readonly object After = After;
    /// <summary>
    /// 该历史时期修改变量的原因
    /// </summary>
    public readonly VariableReason Reason = Reason;
}
