using Card;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Quest;

/// <summary>
/// 任务目标<br/>
/// 一个任务可以同时拥有多个目标，目标之间并行推进、互不阻塞。<br/>
/// 每个目标声明自己关心的时点，命中时由 Advance 判断，<br/>
/// 命中后由 AmountFrom 决定推进多少进度。<br/>
/// 该类不可继承，行为完全由构造时传入的委托决定；委托不依赖 Quest，<br/>
/// 需要读取任务状态时由 Quest 子类在构造时用闭包捕获。<br/>
/// </summary>
public sealed class QuestCondition
{
    /// <summary>
    /// 重置进度(放弃任务时使用)
    /// </summary>
    internal void Reset()
    {
        Current = 0;
    }
    /// <summary>
    /// 当前的进度
    /// </summary>
    public int Current { get; private set; }

    /// <summary>
    /// 目标进度
    /// </summary>
    public int Target { get; init; } = 1;

    /// <summary>
    /// 目标类型标识(仅用于显示/标识，不决定行为)
    /// </summary>
    public string Type { get; init; }

    /// <summary>
    /// 该目标关心哪些时点
    /// </summary>
    public IReadOnlyCollection<Timing> Timings { get; init; } = [];

    /// <summary>
    /// 该目标的显示文本
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// 变量名(用于描述中的 =Sign={N})
    /// </summary>
    public string Sign { get; init; }

    /// <summary>
    /// 命中判定，返回 true 表示本次时点算命中并推进进度
    /// </summary>
    public Func<Timing, CardModel, object[], bool> Advance { get; init; } = (_, _, _) => true;

    /// <summary>
    /// 命中后推进多少进度(null 表示每次推进 1)
    /// </summary>
    public Func<object[], int> AmountFrom { get; init; }

    /// <summary>
    /// 完成百分比(0~1)
    /// </summary>
    public float Progress => Target <= 0 ? 1f : Math.Clamp((float)Current / Target, 0f, 1f);

    /// <summary>
    /// 是否完成
    /// </summary>
    public bool IsCompleted => Current >= Target;

    /// <summary>
    /// 进度变化时触发
    /// </summary>
    public event Action<QuestCondition, float> ProgressChanged;

    /// <summary>
    /// 完成时触发
    /// </summary>
    public event Action<QuestCondition> Completed;

    /// <summary>
    /// 尝试推进一次进度
    /// </summary>
    /// <param name="timing">当前触发的时点</param>
    /// <param name="card">触发时点的卡牌</param>
    /// <param name="parameters">触发时点的参数</param>
    /// <returns>是否命中目标</returns>
    internal bool TryAdvance(Timing timing, CardModel card, object[] parameters)
    {
        if (IsCompleted) return false;
        if (!Advance(timing, card, parameters)) return false;
        AdvanceBy(AmountFrom == null ? 1 : AmountFrom(parameters));
        return true;
    }

    /// <summary>
    /// 推进进度
    /// </summary>
    /// <param name="amount">进度增量</param>
    private void AdvanceBy(int amount)
    {
        if (amount <= 0) return;
        int old = Current;
        Current = Math.Min(Current + amount, Target);
        if (Current == old) return;
        ProgressChanged?.Invoke(this, Progress);
        if (IsCompleted) Completed?.Invoke(this);
    }
}