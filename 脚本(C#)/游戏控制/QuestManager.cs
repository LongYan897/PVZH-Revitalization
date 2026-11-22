using System.Collections.Generic;

namespace Quest;

/// <summary>
/// 任务管理器<br/>
/// 记录当前已接取、正在收听的任务，最多 <see cref="MaxAccepted"/> 个。
/// </summary>
public static class QuestManager
{
    /// <summary>
    /// 同时收听的任务上限
    /// </summary>
    public const int MaxAccepted = 5;

    private static readonly List<Quest> _accepted = new();

    /// <summary>
    /// 当前已接取的任务
    /// </summary>
    public static IReadOnlyList<Quest> Accepted => _accepted;

    /// <summary>
    /// 是否还能再接取任务
    /// </summary>
    public static bool CanAccept => _accepted.Count < MaxAccepted;

    /// <summary>
    /// 尝试加入收听列表
    /// </summary>
    /// <param name="quest">任务</param>
    /// <returns>是否成功</returns>
    internal static bool TryAccept(Quest quest)
    {
        if (quest == null) return false;
        if (_accepted.Count >= MaxAccepted) return false;
        if (_accepted.Contains(quest)) return false;
        _accepted.Add(quest);
        return true;
    }

    /// <summary>
    /// 从收听列表移除
    /// </summary>
    /// <param name="quest">任务</param>
    internal static void Remove(Quest quest)
    {
        if (quest == null) return;
        _accepted.Remove(quest);
    }
}