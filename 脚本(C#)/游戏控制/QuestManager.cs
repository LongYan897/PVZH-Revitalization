using System.Collections.Generic;
using System.Linq;

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

    /// <summary>
    /// 取消拒绝任务收听
    /// </summary>
    public static void DeleteBanQuest()
    {
        foreach (var qu in _ready.ToList())
        {
            if (qu.Accept())
                _ready.Remove(qu);
        }
    }

    /// <summary>
    /// 拒绝任务收听
    /// </summary>
    public static void BanQuest()
    {
        foreach (var qu in Accepted)
        {
            qu.Cancel(false);
            _ready.Add(qu);
        }
    }

    private static readonly List<Quest> _accepted = new();

    private static readonly List<Quest> _ready = new();
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
    public static bool TryAccept(Quest quest)
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
    public static void Remove(Quest quest)
    {
        if (quest == null) return;
        _accepted.Remove(quest);
    }
}