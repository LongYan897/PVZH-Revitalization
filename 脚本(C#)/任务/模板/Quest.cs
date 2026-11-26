using Battle;
using Card;
using Play;
using Quest.String;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Quest;

/// <summary>
/// 任务(继承用)<br/>
/// 构造只加载元数据，接取(Accept)后才开始收听，完成或放弃时停止收听。<br/>
/// 目标(Condition)全部由子类自己在构造时给出；<br/>
/// 名称/描述/特别描述/奖励描述/结构化奖励由基类按 <see cref="object.GetType"/> 的名字从 quests.json 自动加载。<br/>
/// 总进度默认取所有目标进度的平均值，全部目标完成才算任务完成。<br/>
/// </summary>
public abstract class Quest : IBattleHook
{
    private readonly List<QuestHooker> _hookers = new();

    /// <summary>
    /// 任务状态
    /// </summary>
    public QuestState State { get; private set; } = QuestState.Pending;

    /// <summary>
    /// 是否已接取、正在收听
    /// </summary>
    public bool IsAccepted => State == QuestState.Accepted;

    /// <summary>
    /// 构造任务(构造时只加载元数据，接取后才开始收听)
    /// </summary>
    /// <param name="conditions">要并行监听的所有目标</param>
    protected Quest(params QuestCondition[] conditions)
    {
        Conditions = conditions;
        foreach (var condition in conditions)
        {
            _hookers.Add(new QuestHooker(this, condition));
            condition.ProgressChanged += OnProgressChanged;
            condition.Completed += OnConditionCompleted;
        }

        var str = QuestString.Loading(GetType().Name);
        Id = str.Id;
        Title = str.Title;
        Description = str.Description;
        Flavor = str.Flavor;
        RewardText = str.RewardDes;
        Rewards = str.Rewards;
        Camp = str.Camp;
        Type = str.Type;
    }

    /// <summary>
    /// 接取任务：从 Pending 转为 Accepted，并加入收听<br/>
    /// 当前收听任务达到 <see cref="QuestManager.MaxAccepted"/> 时接取失败
    /// </summary>
    /// <returns>是否接取成功</returns>
    public bool Accept()
    {
        if (State != QuestState.Pending) return false;
        if (!QuestManager.TryAccept(this)) return false;
        State = QuestState.Accepted;
        HookManager.Register(this);
        return true;
    }

    /// <summary>
    /// 放弃任务：从 Accepted 回到 Pending，停止收听并清零所有目标进度
    /// </summary>
    public void Cancel(bool resetQuest = false)
    {
        if (State != QuestState.Accepted) return;
        HookManager.Unregister(this);
        QuestManager.Remove(this);
        if (resetQuest)
            foreach (var condition in Conditions)
                condition.Reset();
        State = QuestState.Pending;
    }

    /// <summary>
    /// 停止收听(完成时自动调用，也可手动调用)：Accepted → Completed
    /// </summary>
    public void End()
    {
        if (State != QuestState.Accepted) return;
        HookManager.Unregister(this);
        QuestManager.Remove(this);
        State = QuestState.Completed;
    }

    /// <summary>
    /// 序列化代码
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// 描述(支持 _XXX_ / =XXX= / =XXX={N} / $XXX$ / _#ABC#XXX_ / ==XXX== 等关键字语法)
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 特别描述(支持与 <see cref="Description"/> 相同的关键字语法)
    /// </summary>
    public string Flavor { get; }

    /// <summary>
    /// 奖励描述(仅用于 UI 显示的文本，支持与 <see cref="Description"/> 相同的关键字语法)
    /// </summary>
    public string RewardText { get; }

    /// <summary>
    /// 阵营(null 表示无阵营任务)
    /// </summary>
    public Camp? Camp { get; private set; }

    /// <summary>
    /// 任务类别
    /// </summary>
    public QuestType Type { get; private set; } = QuestType.None;

    /// <summary>
    /// 结构化奖励列表(实际发放用)
    /// </summary>
    public IReadOnlyList<Reward> Rewards { get; }

    /// <summary>
    /// 任务的所有目标
    /// </summary>
    public IReadOnlyList<QuestCondition> Conditions { get; }

    /// <summary>
    /// 任务的总进度(0~1)，默认取所有目标的平均值
    /// </summary>
    public virtual float Progress =>
        Conditions.Count == 0 ? 1f : (float)Conditions.Average(c => c.Progress);

    /// <summary>
    /// 任务是否完成(所有目标完成)
    /// </summary>
    public bool IsCompleted => Conditions.All(c => c.IsCompleted);

    /// <summary>
    /// 总进度变化时触发
    /// </summary>
    public event Action<Quest, float> ProgressChanged;

    /// <summary>
    /// 任务完成时触发
    /// </summary>
    public event Action<Quest> Completed;

    /// <summary>
    /// 任务的监听者
    /// </summary>
    protected virtual List<QuestHooker> Hookers => _hookers;

    /// <summary>
    /// 实现 IBattleHook 的监听者列表
    /// </summary>
    public List<Hooker> Hooks => [.. Hookers];

    /// <summary>
    /// 领取任务奖励，把 <see cref="Rewards"/> 逐条应用到玩家资产<br/>
    /// 仅在任务完成且尚未领取时生效
    /// </summary>
    /// <param name="player">目标玩家</param>
    /// <returns>是否成功领取</returns>
    public bool Claim(Player player)
    {
        if (player == null) return false;
        if (!IsCompleted) return false;
        if (_claimed) return false;
        _claimed = true;
        foreach (var reward in Rewards)
            reward?.ApplyTo(player.Asset);
        return true;
    }

    private bool _claimed;

    /// <summary>
    /// 子目标进度变化时，汇总后触发总进度
    /// </summary>
    /// <param name="condition">发生变化的子目标</param>
    /// <param name="progress">子目标当前的进度</param>
    private void OnProgressChanged(QuestCondition condition, float progress)
        => ProgressChanged?.Invoke(this, Progress);

    /// <summary>
    /// 子目标完成时，全部完成则停止监听并触发完成事件
    /// </summary>
    /// <param name="condition">完成的子目标</param>
    private void OnConditionCompleted(QuestCondition condition)
    {
        if (!IsCompleted) return;
        End();
        Completed?.Invoke(this);
    }

    /// <summary>
    /// 任务监听(不可继承)
    /// </summary>
    protected sealed class QuestHooker : Hooker
    {
        private static readonly Timing Rejected = (Timing)(-1);

        private readonly Quest _owner;
        private readonly QuestCondition _condition;

        /// <summary>
        /// 构造任务监听者
        /// </summary>
        /// <param name="owner">所属任务</param>
        /// <param name="condition">监听的子目标</param>
        public QuestHooker(Quest owner, QuestCondition condition)
        {
            _owner = owner;
            _condition = condition;
        }

        /// <summary>
        /// 按 condition 的 Timings 过滤时点，只有关心的时点才放行
        /// </summary>
        /// <param name="hookedTiming">钩子传来的时点</param>
        /// <param name="card">触发时点的卡牌</param>
        /// <param name="timing">放行的时点</param>
        /// <param name="parameters">触发时点的参数</param>
        protected override void Hooked(
            Timing hookedTiming,
            CardModel card,
            out Timing timing,
            params object[] parameters)
        {
            timing = _condition.Timings.Contains(hookedTiming) ? hookedTiming : Rejected;
        }

        /// <summary>
        /// 命中的时点交给 condition 推进进度
        /// </summary>
        /// <param name="hookedTiming">钩子传来的时点</param>
        /// <param name="card">触发时点的卡牌</param>
        /// <param name="parameters">触发时点的参数</param>
        protected override void Hooking(
            Timing hookedTiming,
            CardModel card,
            params object[] parameters)
        {
            if (_owner.IsCompleted) return;
            _condition.TryAdvance(hookedTiming, card, parameters);
        }
    }
}