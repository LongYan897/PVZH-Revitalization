using Battle;
using Card;
using Card.Cmd;
using Godot;
using Logger;
using Pack;
using Play;
using Spine;
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
        isProcessing = true;
        var plantSide = PlayerManager.HostPlayer == PlayerManager.GetPlayer(Camp.Plant);
        var entr = plantSide ? "plant" : "zombie";
        await CardCmd.TimingOnCards(null, Timing.BeforePhraseEnd, Phrase);
        await CardCmd.TimingOnCards(null, Timing.AfterPhraseEnd, Phrase);
        bool isBattle = Phrase == Phrase.Fight;
        if (((int)Phrase) >= 4)
        {
            Phrase = (Phrase)1;
        }
        else
        {
            Phrase += 1;
        }
        string ext = !isTurnCostNotEnough ? "Yellow" : "Green";
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
            await NextPhrase(true);
        }
        isProcessing = false;
        turnFirst = true;
        CardCmd.PopStop();
    }
    private static bool turnFirst = true;
    private static bool isTurnCostNotEnough = false;
    private static bool isProcessing = false;
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
        isProcessing = true;
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
        isProcessing = false;
        isTurnCostNotEnough = true;
        if (spine.HasAnimation(loop))
            await spine.SetAnimationTask(0, loop, true);
        CardCmd.PopStop();
    }
    private static async void PressedNextPhrase(Button button)
    {
        if (isProcessing) return;
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
    public static async Task StartBattle()
    {
        var plantSide = PlayerManager.HostPlayer == PlayerManager.GetPlayer(Camp.Plant);
        await Task.WhenAll(
        PlayerManager.HostPlayer.Status.Hero.Cost.Gain(_turn, Variable.VariableReason.Self, null),
        (plantSide ? PlayerManager.GetPlayer(Camp.Zombie) : PlayerManager.GetPlayer(Camp.Plant)).Status.Hero.Cost.Gain(_turn, Variable.VariableReason.Self, null)
        );
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
}
