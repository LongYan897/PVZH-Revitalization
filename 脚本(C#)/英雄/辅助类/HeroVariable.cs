using Card;
using Card.Cmd;
using Hero.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Variable;

namespace Hero;
public class HeroVariable<T>(HeroModel hero,string sign,T baseValue)
{
    public HeroModel hero { get; init; } = hero;
    private Variable<T> variable => new Variable<T>(sign,baseValue);
    public T Current => variable.Current;
    public bool HasChanged => variable.HasChanged;
    public string Key => variable.Sign;
    /// <summary>
    /// 设置值
    /// </summary>
    /// <param name="value">设置的值</param>
    /// <param name="reason">设置的原因</param>
    /// <param name="card">调用函数的卡牌</param>
    /// <param name="variableType">更改的英雄变量枚举值</param>
    /// <param name="impactType">效果影响类型</param>
    /// <returns></returns>
    public async Task<int> Set(T value, VariableReason reason, CardModel card, VariableType variableType,ImpactType impactType)
    {
        Variable<T> v = new Variable<T>(sign,baseValue);
        await CardCmd.TimingOnCards(null,Timing.ModifyHeroChanged,value,card,reason,variableType,impactType,v);
        await CardCmd.TimingOnCards(null,Timing.BeforeHeroChanged,v.Current,card,reason,variableType,impactType);
        int val = variable.Set(v.Current, reason);
        await CardCmd.TimingOnCards(null, Timing.OnHeroChanged, v.Current, card,reason, variableType, impactType);
        await CardCmd.TimingOnCards(null, Timing.AfterHeroChanged, v.Current, card,reason, variableType, impactType);
        return val;
    }
    /// <summary>
    /// 回溯值
    /// </summary>
    /// <param name="Index">回溯的历史时期</param>
    /// <param name="card">调用函数的卡牌</param>
    /// <param name="variableType">更改的英雄变量枚举值</param>
    /// <param name="impactType">效果影响类型</param>
    /// <returns></returns>
    public async Task Rewind(int Index,CardModel card,VariableType variableType,ImpactType impactType)
    {
        VariableHistory history = variable.GetHistory().First(his => his.Index == Index);
        var old = variable.Current;
        var value = history.Before;
        Variable<T> v = new Variable<T>(sign,(T)value);
        await CardCmd.TimingOnCards(null, Timing.ModifyHeroChanged, card, VariableReason.Reset, variableType, impactType, v);
        await CardCmd.TimingOnCards(null, Timing.BeforeHeroChanged, v.Current, card, VariableReason.Reset,variableType, impactType);
        variable.Rewind(Index);
        await CardCmd.TimingOnCards(null, Timing.OnHeroChanged, v.Current, card, VariableReason.Reset, variableType, impactType);
        await CardCmd.TimingOnCards(null, Timing.AfterHeroChanged, v.Current, card, VariableReason.Reset, variableType, impactType);
    }
    /// <summary>
    /// 回溯值(不清空历史)
    /// </summary>
    /// <param name="Index">回溯的历史时期</param>
    /// <param name="reason">回溯的原因</param>
    /// <param name="card">调用函数的卡牌</param>
    /// <param name="variableType">更改的英雄变量枚举值</param>
    /// <param name="impactType">效果影响类型</param>
    /// <returns></returns>
    public async Task RewindReasonable(int Index, VariableReason reason, CardModel card,VariableType variableType,ImpactType impactType)
    {
        VariableHistory history = variable.GetHistory().First(his => his.Index == Index);
        var old = variable.Current;
        var value = history.Before;
        Variable<T> v = new Variable<T>(sign, (T)value);
        await CardCmd.TimingOnCards(null, Timing.ModifyHeroChanged, card, reason, variableType, impactType, v);
        await CardCmd.TimingOnCards(null, Timing.BeforeHeroChanged, v.Current, card, reason, variableType, impactType);
        variable.RewindReasonable(Index, reason);
        await CardCmd.TimingOnCards(null, Timing.OnHeroChanged, v.Current, card, reason, variableType, impactType);
        await CardCmd.TimingOnCards(null, Timing.AfterHeroChanged, v.Current, card, reason, variableType, impactType);
    }

    /// <summary>
    /// 重置值
    /// </summary>
    /// <param name="card">调用函数的卡牌</param>
    /// <param name="variableType">更改的英雄变量枚举值</param>
    /// <param name="impactType">效果影响类型</param>
    /// <returns></returns>
    public async Task Reset(CardModel card,VariableType variableType,ImpactType impactType)
    {
        var old = variable.Current;
        Variable<T> v = new Variable<T>(sign, (T)old);
        await CardCmd.TimingOnCards(null, Timing.ModifyHeroChanged, card, VariableReason.Reset, variableType, impactType,v);
        await CardCmd.TimingOnCards(null, Timing.BeforeHeroChanged,v.Current, card, VariableReason.Reset, variableType, impactType);
        variable.Reset();
        await CardCmd.TimingOnCards(null, Timing.OnHeroChanged, v.Current, card, VariableReason.Reset, variableType, impactType);
        await CardCmd.TimingOnCards(null, Timing.AfterHeroChanged, v.Current, card, VariableReason.Reset, variableType, impactType);
    }
}
/// <summary>
/// 用于分辨更改的英雄变量
/// </summary>
public enum VariableType
{
    /// <summary>
    /// 血量
    /// </summary>
    Health,
    /// <summary>
    /// 血上限
    /// </summary>
    MaxHealth,
    /// <summary>
    /// 护盾值
    /// </summary>
    Shield
}
/// <summary>
/// 用于区分效果影响
/// </summary>
public enum ImpactType
{
    /// <summary>
    /// 中性效果
    /// </summary>
    Netural,
    /// <summary>
    /// 反面效果
    /// </summary>
    Opposition,
    /// <summary>
    /// 正面效果
    /// </summary>
    Benefit
}