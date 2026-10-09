using Battle;
using Card;
using Card.Cmd;
using Godot;
using Logger;
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
    /// 构建战斗的道路
    /// </summary>
    public static void BuildUp(Node node)
    {
        foreach (var nRoad in node.GetChildren().OfType<NodeRoad>())
        {
            foreach (var road in Roads)
            {
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
    public static async Task NextPhrase()
    {
        await CardCmd.TimingOnCards(null, Timing.BeforePhraseEnd, Phrase);
        await CardCmd.TimingOnCards(null, Timing.AfterPhraseEnd, Phrase);
        if (((int)Phrase) >= 4)
        {
            Phrase = (Phrase)1;
        }
        else
        {
            Phrase += 1;
        }
        await spine.SetAnimationTask(0, Phrase switch
        {
            Phrase.Plant => "plantMovePlantTurn",
            Phrase.Trick => "plantMovePlanTurn",
            Phrase.Fight => "plantMoveAttackTurn",
            Phrase.Zombie => "plantBackCardTurn"
        });
        await CardCmd.TimingOnCards(null, Timing.BeforePhraseStart, Phrase);
        if (Phrase == Phrase.Fight)
        {
            await LinesBattle();
        }
        await CardCmd.TimingOnCards(null, Timing.AfterPhraseStart, Phrase);
        if (Phrase == Phrase.Fight)
        {
            await NextPhrase();
        }
    }
    private static async void PressedNextPhrase(Button button)
    {
        button.MouseFilter = Control.MouseFilterEnum.Ignore;
        if (Phrase == Phrase.Fight)
            return;
        await NextPhrase();
        button.MouseFilter = Control.MouseFilterEnum.Stop;
    }
    /// <summary>
    /// 绑定阶段动画
    /// </summary>
    /// <param name="spineHandler"></param>
    public static void SetUp(Button button,SpineHandler spineHandler)
    {
        spine = spineHandler;
        spine.SetAnimation(0,"zombieStartZombieTurn",false);
        button.Pressed += () => PressedNextPhrase(button);
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
