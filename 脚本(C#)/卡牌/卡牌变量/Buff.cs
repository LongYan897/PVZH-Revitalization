using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 挂在卡牌上的效果
/// 不存目标引用，card 由时点回调传入，便于转移
/// </summary>
public abstract class Buff
{
    /// <summary>
    /// 能否被转移到其他卡牌上
    /// </summary>
    public virtual bool IsTransferable => true;
    /// <summary>
    /// 关心的时点
    /// </summary>
    public abstract Timing[] Timings { get; }

    /// <summary>
    /// 时点回调
    /// </summary>
    /// <param name="timing">当前时点</param>
    /// <param name="card">当前挂载的卡牌</param>
    /// <param name="parameters">传入的参数（按照时点类型传参）</param>
    public abstract Task OnTiming(Timing timing, CardModel card, params object[] parameters);
}

/// <summary>
/// 可自定义时点的 Buff，供 Mod 使用
/// </summary>
public class CustomBuff : Buff
{
    private readonly Timing[] _timings;
    private readonly Func<Timing, CardModel, object[], Task> _callback;

    public CustomBuff(Timing[] timings, Func<Timing, CardModel, object[], Task> callback)
    {
        _timings = timings ?? Array.Empty<Timing>();
        _callback = callback;
    }

    public override Timing[] Timings => _timings;

    public override Task OnTiming(Timing timing, CardModel card, params object[] parameters)
    {
        if (_callback == null)
            return Task.CompletedTask;
        return _callback(timing, card, parameters);
    }
}

/// <summary>
/// 效果关心的时点
/// </summary>
public enum Timing
{
    /// <summary>
    /// 当卡牌进化时
    /// 参数：[0] FighterCardModel 被进化的卡牌 [1] FighterCardModel 进化的卡牌
    /// </summary>
    WhenEvolve,
    /// <summary>
    /// 当卡牌进化时
    /// 参数：[0] FighterCardModel 被融合的卡牌 [1] FighterCardModel 融合的卡牌
    /// </summary>
    WhenFuse,
    /// <summary>
    /// 效果/类型被赋予卡牌时
    /// 参数：[0] VariableReason 赋予原因
    /// </summary>
    WhenApply,

    /// <summary>
    /// 效果/类型被移除时
    /// 参数：[0] VariableReason 移除原因
    /// </summary>
    WhenRemove,

    /// <summary>
    /// 卡牌被克隆后
    /// 参数：[0] CardModel 新卡实例
    /// </summary>
    WhenCloned,

    /// <summary>
    /// 卡牌改变后
    /// 参数：[0] CardModel 旧卡，[1] CardModel 新卡
    /// </summary>
    WhenTransformed,

    /// <summary>
    /// 卡牌打出前
    /// 参数：[0] ITarget 打出目标
    /// </summary>
    BeforePlay,

    /// <summary>
    /// 卡牌打出时
    /// 参数：[0] ITarget 打出目标
    /// </summary>
    OnPlay,

    /// <summary>
    /// 卡牌打出后
    /// 参数：[0] ITarget 打出目标
    /// </summary>
    AfterPlay,

    /// <summary>
    /// 任意卡牌被打出后
    /// 参数：[0] CardModel 被打出的卡，[1] ITarget 打出目标
    /// </summary>
    AfterCardPlayed,

    /// <summary>
    /// 回合开始前
    /// 参数：[0] Phrase 当前回合
    /// </summary>
    BeforeTurnStart,

    /// <summary>
    /// 回合开始后
    /// 参数：[0] Phrase 当前回合
    /// </summary>
    AfterTurnStart,

    /// <summary>
    /// 回合结束前
    /// 参数：[0] Phrase 当前回合
    /// </summary>
    BeforeTurnEnd,

    /// <summary>
    /// 回合结束后
    /// 参数：[0] Phrase 当前回合
    /// </summary>
    AfterTurnEnd,

    /// <summary>
    /// 抽牌时
    /// 参数：[0] CardModel 抽到的卡
    /// </summary>
    OnDraw,

    /// <summary>
    /// 该线战斗开始前
    /// 参数：[0] Road 该线
    /// </summary>
    BeforeLaneStart,

    /// <summary>
    /// 该线战斗开始后
    /// 参数：[0] Road 该线
    /// </summary>
    AfterLaneStart,

    /// <summary>
    /// 该线战斗结束前
    /// 参数：[0] Road 该线
    /// </summary>
    BeforeLaneEnd,

    /// <summary>
    /// 该线战斗结束后
    /// 参数：[0] Road 该线
    /// </summary>
    AfterLaneEnd,

    /// <summary>
    /// 该线单位进出 / 环境改变后
    /// 参数：[0] Road 该线
    /// </summary>
    AfterRoadChanged,
    /// <summary>
    /// 在阶段开始后
    /// 参数：[0] Phrase 开始的阶段
    /// </summary>
    AfterPhraseStart,
    /// <summary>
    /// 在阶段结束后
    /// 参数：[0] Phrase 结束的阶段
    /// </summary>
    AfterPhraseEnd,

    /// <summary>
    /// 攻击宣告前
    /// 参数：[0] AtkStack 攻击上下文
    /// </summary>
    BeforeAttack,
    /// <summary>
    /// 在阶段开始前
    /// 参数：[0] Phrase 开始的阶段
    /// </summary>
    BeforePhraseStart,

    /// <summary>
    /// 在阶段结束前
    /// 参数：[0] Phrase 结束的阶段
    /// </summary>
    BeforePhraseEnd,
    /// <summary>
    /// 攻击时
    /// 参数：[0] AtkStack 攻击上下文
    /// </summary>
    OnAttack,

    /// <summary>
    /// 修改攻击目标
    /// 参数：[0] AtkStack 攻击上下文
    /// </summary>
    ModifyTarget,

    /// <summary>
    /// 修改伤害
    /// 参数：[0] AtkStack 攻击上下文
    /// </summary>
    ModifyDamage,

    /// <summary>
    /// 修改治疗量
    /// 参数：[0] Variable<int> 治疗量 [1] FighterCardModel/HeroModel 获得治疗的单位
    /// </summary>
    ModifyHeal,

    /// <summary>
    /// 修改获得最大血上限
    /// 参数：[0] Variable<int> 获得最大血上限的量 [1] FighterCardModel/HeroModel 获得最大血上限的单位
    /// </summary>
    ModifyGainMaxHp,

    /// <summary>
    /// 攻击结算后
    /// 参数：[0] AtkStack 攻击上下文
    /// </summary>
    AfterAttack,

    /// <summary>
    /// 对目标的效果
    /// 参数：[0] AtkStack 攻击上下文
    /// </summary>
    ApplyTarget,

    /// <summary>
    /// 被攻击时
    /// 参数：[0] AtkStack 攻击上下文
    /// </summary>
    WhenAttacked,

    /// <summary>
    /// 修改受到的伤害
    /// 参数：[0] AtkStack 伤害上下文(目标一般是自己)
    /// </summary>
    ModifyCardDamage,

    /// <summary>
    /// 被攻击后
    /// 参数：[0] AtkStack 攻击上下文
    /// </summary>
    AfterAttacked,

    /// <summary>
    /// 受到伤害后
    /// 参数：[0] AtkStack 伤害上下文(目标一般是自己)
    /// </summary>
    OnDamaged,

    /// <summary>
    /// 被治疗后
    /// 参数：[0] int 治疗量 [1] FighterCardModel/HeroModel 获得治疗的单位
    /// </summary>
    OnHealed,

    /// <summary>
    /// 死亡时
    /// 参数：[0] AtkStack 伤害上下文(目标一般是自己)
    /// </summary>
    OnDeath,
    /// <summary>
    /// 被战斗击杀时 [0] AtkStack 伤害上下文(目标一般是自己)
    /// </summary>
    OnKilled,

    /// <summary>
    /// 当卡牌变量被修改时
    /// 参数：[0] string key，[1] object valueAfter，[2] object valueBefore [3] VariableReason 原因
    /// </summary>
    OnVariableChanged,
    /// <summary>
    /// 单位进场时
    /// 参数：[0] Road 该线，[1] FighterCardModel 进场的单位
    /// </summary>
    OnFighterEnter,

    /// <summary>
    /// 单位离场时
    /// 参数：[0] Road 该线，[1] FighterCardModel 离场的单位
    /// </summary>
    OnFighterExit,
    /// <summary>
    /// 英雄变量改变时
    /// 参数：[0] Value 更改的数值, 
    /// [1] VariableReason 更改的原因 
    /// [2] CardModel 造成影响的卡牌 
    /// [3] HeroModel 英雄对象
    /// [4] VariableType 英雄变量类型 
    /// [5] ImpactType 影响的好坏
    /// </summary>
    OnHeroChanged,
    /// <summary>
    /// 英雄变量即将改变时
    /// 参数：[0] Value 更改的数值, 
    /// [1] VariableReason 更改的原因 
    /// [2] CardModel 造成影响的卡牌 
    /// [3] HeroModel 英雄对象
    /// [4] VariableType 英雄变量类型 
    /// [5] ImpactType 影响的好坏
    /// </summary>
    BeforeHeroChanged,
    /// <summary>
    /// 英雄变量改变后
    /// 参数：[0] Value 更改的数值, 
    /// [1] VariableReason 更改的原因 
    /// [2] CardModel 造成影响的卡牌 
    /// [3] HeroModel 英雄对象
    /// [4] VariableType 英雄变量类型 
    /// [5] ImpactType 影响的好坏
    /// </summary>
    AfterHeroChanged,
    /// <summary>
    /// 英雄变量改变时
    /// 参数：
    /// [0] VariableReason 更改的原因 
    /// [1] CardModel 造成影响的卡牌 
    /// [2] HeroModel 英雄对象
    /// [3] VariableType 英雄变量类型 
    /// [4] ImpactType 影响的好坏
    /// [5] HeroVariable 预计的在调用OnHeroChanged时的变量
    /// </summary>
    ModifyHeroChanged

    /// <summary>
    /// 尝试阻止单位攻击
    /// 参数：[0] Variable<bool> 是否阻止本次攻击 [1] FighterCardModel 要攻击的单位
    /// </summary>
    TryStopAttack,

    /// <summary>
    /// 尝试阻止单位恢复生命
    /// 参数：[0] Variable<bool> 是否阻止本次治疗 [1] FighterCardModel/HeroModel 要治疗的单位
    /// </summary>
    TryStopHeal,

    /// <summary>
    /// 尝试阻止单位获得最大血上限
    /// 参数：[0] Variable<bool> 是否阻止本次获得最大血上限 [1] FighterCardModel/HeroModel 要获得最大血上限的单位
    /// </summary>
    TryStopGainMaxHp,
}