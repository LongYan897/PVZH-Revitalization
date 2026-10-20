using Battle;
using Card;
using Card.Cmd;
using Godot;
using Logger;
using Pack;
using Play;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Phrases;

public enum Phrase
{
    /// <summary>
    /// 非战斗状态
    /// </summary>
    None,
    /// <summary>
    /// 僵尸主要回合
    /// </summary>
    Zombie,
    /// <summary>
    /// 植物主要回合
    /// </summary>
    Plant,
    /// <summary>
    /// 僵尸锦囊牌回合
    /// </summary>
    Trick,
    /// <summary>
    /// 战斗回合
    /// </summary>
    Fight
}
public static class PhraseManager
{
    /// <summary>
    /// 记录是否在战斗中
    /// </summary>
    public static bool IsInBattle { get; private set; } = false;
    /// <summary>
    /// 如果在战斗中则为正常的阶段 非战斗设置为Phrase.None
    /// </summary>
    public static Phrase Phrase { get; private set; } = Phrase.None;
    /// <summary>
    /// 记录战斗中的道路
    /// </summary>
    private static readonly List<Road> Roads =
    [
        new Road() {Index = 1,Type = RoadType.Height},
        new Road() {Index = 2,Type = RoadType.Ground},
        new Road() {Index = 3,Type = RoadType.Ground},
        new Road() {Index = 4,Type = RoadType.Ground},
        new Road() {Index = 5,Type = RoadType.Water},
    ];
    /// <summary>
    /// 当前的回合数
    /// </summary>
    private static int _turn;
    /// <summary>
    /// 当前的回合序号
    /// </summary>
    public static int Turn => _turn;
    /// <summary>
    /// 构建战斗的道路
    /// </summary>
    public static void BuildUp(Node node, Camp camp)
    {
        _turn = 1;
        var bol = camp == Camp.Zombie;
        foreach (var nRoad in node.GetChildren().OfType<NodeRoad>())
        {
            foreach (var road in Roads)
            {
                road.IsUp = bol;
                if (road.Index == nRoad.Index + 1)
                    nRoad.Bind(road);
            }
        }
    }
    /// <summary>
    /// 获取战斗中的道路
    /// </summary>
    /// <param name="Index">道路的编号 1 代表高地 5 代表水路 234 代表平地</param>
    /// <returns></returns>
    public static Road GetRoad(int Index)
    {
        if (!IsInBattle)
        {
            Log.Error("在非战斗场景下获取了道路");
            return null;
        }
        if (Index > 5 || Index <= 0)
        {
            Log.Error("道路数量只有五个 却尝试获取第六个以上或者第零个以下的道路");
            return null;
        }
        return Roads.First(r => r.Index == Index);
    }
    /// <summary>
    /// 下一个阶段
    /// </summary>
    /// <returns></returns>
    public static async Task NextPhrase(bool isAuto = false)
    {
        if (!IsInBattle) return;
        CardCmd.PushStop();
        PushProcessing();
        var plantSide = PlayerManager.HostPlayer == PlayerManager.GetPlayer(Camp.Plant);
        var entr = plantSide ? "plant" : "zombie";
        await CardCmd.TimingOnCards(null, Timing.BeforePhraseEnd, Phrase);
        await CardCmd.TimingOnCards(null, Timing.AfterPhraseEnd, Phrase);
        bool isBattle = Phrase == Phrase.Fight;
        var myTurn = PlayerManager.HostPlayer.Status.Hero.Camp.Switch(
                Phrase == Phrase.Plant,
                Phrase == Phrase.Zombie || Phrase == Phrase.Trick);
        if (((int)Phrase) >= 4)
        {
            Phrase = (Phrase)1;
        }
        else
        {
            Phrase += 1;
        }
        string ext = myTurn ? !isTurnCostNotEnough ? "Yellow" : "Green" : "";
        await spine.SetAnimationTask(0, isBattle ? $"{entr}BackCardTurn" : Phrase switch
        {
            Phrase.Plant => $"{entr}{ext}MovePlantTurn",
            Phrase.Trick => $"{entr}{ext}MoveTrickTurn",
            Phrase.Fight => $"{entr}{ext}MoveAttackTurn",
            Phrase.Zombie => $"{entr}StartZombieTurn",
            _ => ""
        });
        if (Phrase == Phrase.Zombie && isAuto)
        {
            isTurnCostNotEnough = false;
            await Task.WhenAll(
            PlayerManager.HostPlayer.Status.Hero.Cost.Gain(_turn, Variable.VariableReason.Self, null),
            (plantSide ? PlayerManager.GetPlayer(Camp.Zombie) : PlayerManager.GetPlayer(Camp.Plant)).Status.Hero.Cost.Gain(_turn, Variable.VariableReason.Self, null),
            PlayerManager.HostPlayer.Status.PlayCardAndDraw(),
            (plantSide ? PlayerManager.GetPlayer(Camp.Zombie) : PlayerManager.GetPlayer(Camp.Plant)).Status.PlayCardAndDraw()
            );
        }
        if (isBattle)
        {
            if (!plantSide)
                await spine.SetAnimationTask(0, $"zombieStartZombieTurn");
        }
        await CardCmd.TimingOnCards(null, Timing.BeforePhraseStart, Phrase);
        await NotifyPhraseChanged();
        if (Phrase == Phrase.Fight)
        {
            await LinesBattle();
        }
        await CardCmd.TimingOnCards(null, Timing.AfterPhraseStart, Phrase);
        if (Phrase == Phrase.Fight)
        {
            await Task.WhenAll(
                (plantSide ? PlayerManager.GetPlayer(Camp.Zombie) : PlayerManager.GetPlayer(Camp.Plant)).Status.Hero.Cost.Reset(null),
                PlayerManager.HostPlayer.Status.Hero.Cost.Reset(null)
                );
            _turn++;
        }
        PopProcessing();
        turnFirst = true;
        CardCmd.PopStop();
        if (Phrase == Phrase.Fight)
            await NextPhrase(true);
        await HintAi();
    }
    private static bool turnFirst = true;
    private static bool isTurnCostNotEnough = false;
    /// <summary>
    /// 获得当前回合的玩家
    /// </summary>
    public static Player CurrentTurnPlayer => IsInBattle ? Phrase switch
    {
        Phrase.Plant => PlayerManager.GetPlayer(Camp.Plant),
        _ => PlayerManager.GetPlayer(Camp.Zombie)
    } : null;
    private static int _processingDepth;

    public static void PushProcessing()
    {
        _processingDepth++;
    }

    public static void PopProcessing()
    {
        if (_processingDepth > 0) _processingDepth--;
    }

    private static bool IsProcessing => _processingDepth > 0;
    /// <summary>
    /// 手牌没有卡牌可以打出时
    /// </summary>
    /// <returns></returns>
    public static async Task HintCostNotEnough()
    {
        if (!IsInBattle) return;
        if (!turnFirst) return;
        CardCmd.PushStop();
        turnFirst = false;
        PushProcessing();
        var plantSide = PlayerManager.HostPlayer == PlayerManager.GetPlayer(Camp.Plant);
        var loop = plantSide ? Phrase switch
        {
            Phrase.Plant => "plantTurnProcessing",
            _ => ""
        } : Phrase switch
        {
            Phrase.Trick => "trickTurnProcessing",
            Phrase.Zombie => "zombieTurnProcessing",
            _ => ""
        };
        isTurnCostNotEnough = true;
        if (spine.HasAnimation(loop))
            await spine.SetAnimationTask(0, loop, true);
        CardCmd.PopStop();
        PopProcessing();
    }
    private static async void PressedNextPhrase(Button button)
    {
        if (IsProcessing) return;
        if (!CanPress()) return;
        button.MouseFilter = Control.MouseFilterEnum.Ignore;
        try
        {
            if (Phrase == Phrase.Fight) return;
            await NextPhrase();
        }
        finally
        {
            button.MouseFilter = Control.MouseFilterEnum.Stop;
        }
    }
    private static bool CanPress()
    {
        var plantSide = PlayerManager.HostPlayer == PlayerManager.GetPlayer(Camp.Plant);
        return plantSide
            ? Phrase == Phrase.Plant
            : Phrase == Phrase.Zombie || Phrase == Phrase.Trick;
    }
    /// <summary>
    /// 绑定阶段动画
    /// </summary>
    /// <param name="spineHandler"></param>
    public static async void SetUp(Button button, SpineHandler spineHandler)
    {
        IsInBattle = true;
        var plantSide = PlayerManager.HostPlayer == PlayerManager.GetPlayer(Camp.Plant);
        var entr = plantSide ? "plant" : "zombie";
        spine = spineHandler;
        spine.SetAnimation(0, $"{entr}StartZombieTurn", false);
        Phrase = Phrase.Zombie;
        button.Pressed += () => PressedNextPhrase(button);
        var p1 = PlayerManager.GetPlayer(Camp.Plant);
        var p2 = PlayerManager.GetPlayer(Camp.Zombie);
        p1.Status.Hero.SetUp();
        p2.Status.Hero.SetUp();
    }
    private static TaskCompletionSource _canPlaySignal = new();

    public static void NotifyCanPlayChanged()
    {
        if (_canPlaySignal.Task.IsCompleted)
            _canPlaySignal = new TaskCompletionSource();
        _canPlaySignal.TrySetResult();
    }

    public static async Task HintAi()
    {
        var ai = FindAiPlayer();
        if (ai == null) return;

        var Camp = ai.Status.Hero.Camp;
        var isTurnMe =
        (PhraseManager.Phrase == Phrase.Plant && Camp == Camp.Plant)
        || (PhraseManager.Phrase == Phrase.Zombie && Camp == Camp.Zombie)
        || (PhraseManager.Phrase == Phrase.Trick && Camp == Camp.Zombie);

        if (!isTurnMe) return;
        if (IsInBattle && CurrentTurnPlayer is AiPlayer aiPlayer)
            await aiPlayer.Tick();
    }
    public static async Task StartBattle()
    {
        var plantSide = PlayerManager.HostPlayer == PlayerManager.GetPlayer(Camp.Plant);
        await Task.WhenAll(
        PlayerManager.HostPlayer.Status.Hero.Cost.Gain(_turn, Variable.VariableReason.Self, null),
        (plantSide ? PlayerManager.GetPlayer(Camp.Zombie) : PlayerManager.GetPlayer(Camp.Plant)).Status.Hero.Cost.Gain(_turn, Variable.VariableReason.Self, null)
        );

        await HintAi();
    }

    private static readonly Dictionary<Phrase, List<(int Priority, Func<Task> Func)>> _phraseWaiters = new();

    /// <summary>
    /// 等待阶段切换到目标阶段，到达后执行回调
    /// </summary>
    /// <param name="phrase">目标阶段</param>
    /// <param name="onReached">到达后执行的回调</param>
    /// <param name="priority">优先级，越大越先执行，默认 0</param>
    public static async Task WaitForPhrase(Phrase phrase, Func<Task> onReached, int priority = 0)
    {
        if (Phrase == phrase)
        {
            await onReached();
            return;
        }

        if (!_phraseWaiters.TryGetValue(phrase, out var list))
        {
            list = new List<(int, Func<Task>)>();
            _phraseWaiters[phrase] = list;
        }
        list.Add((priority, onReached));
    }

    private static async Task NotifyPhraseChanged()
    {
        if (!_phraseWaiters.TryGetValue(Phrase, out var list) || list.Count == 0)
            return;

        _phraseWaiters.Remove(Phrase);
        var ordered = list
            .Select((item, index) => (item.Priority, item.Func, index))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.index)
            .Select(x => x.Func)
            .ToList();

        foreach (var func in ordered)
            await func();
    }
    private static SpineHandler spine;
    /// <summary>
    /// 开始战斗
    /// </summary>
    /// <returns></returns>
    private static async Task LinesBattle()
    {
        foreach (var r in Roads)
        {
            await r.Start();
        }
    }
    //ai部分
    /// <summary>
    /// 找到当前战斗中的 AI 玩家
    /// </summary>
    private static AiPlayer FindAiPlayer()
    {
        var plant = PlayerManager.GetPlayer(Camp.Plant);
        var zombie = PlayerManager.GetPlayer(Camp.Zombie);

        if (plant is AiPlayer pAi) return pAi;
        if (zombie is AiPlayer zAi) return zAi;
        return null;
    }
}