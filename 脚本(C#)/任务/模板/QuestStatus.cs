namespace Quest;

/// <summary>
/// 任务状态
/// </summary>
public enum QuestState
{
    /// <summary>
    /// 未接取
    /// </summary>
    Pending,

    /// <summary>
    /// 已接取，正在收听
    /// </summary>
    Accepted,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed,
}