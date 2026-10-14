using Card;
using Card.Cmd;
using Hero.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Variable;
using Variable.Special;

namespace Hero;
/// <summary>
/// 英雄数值的变量
/// </summary>
/// <param name="hero">英雄</param>
/// <param name="sign">名称</param>
/// <param name="baseValue">基础值</param>
public class HeroIntVariable(HeroModel hero,string sign,int baseValue)
{
    private HeroModel hero { get; init; } = hero;
    private readonly IntVariable variable = new IntVariable(sign,baseValue);
    public int Current => variable.Current;
    public int Base => variable.Base;
    public bool HasChanged => variable.HasChanged;
    public bool NegetiveChanged => variable.NegitiveChanged;
    public bool PositiveChanged => variable.PositiveChanged;
    public string Key => variable.Sign;
    /// <summary>
    /// 减少值
    /// </summary>
    /// <param name="value">减少的值</param>
    /// <param name="reason">减少的原因</param>
    /// <param name="card">调用函数的卡牌</param>
    /// <param name="variableType">更改的英雄变量枚举值</param>
    /// <param name="impactType">效果影响类型</param>
    /// <returns></returns>
    public async Task Lose(int value, VariableReason reason, CardModel card,params object[] objects)
    {
        var old = variable.Current;
        IntVariable v = new("Lose", value);
        await CardCmd.TimingOnCards(null, Timing.ModifyHeroChanged, variable.Sign, value, card, reason, v);
        await CardCmd.TimingOnCards(null, Timing.BeforeHeroChanged, variable.Sign, v.Current, card, reason);
        variable.Lose(v.Current, reason);
        await hero.Changed(sign,old,variable.Current, Math.Abs(v.Current), reason,card,objects);
        await CardCmd.TimingOnCards(null, Timing.OnHeroChanged, variable.Sign, v.Current, card, reason);
        await CardCmd.TimingOnCards(null, Timing.AfterHeroChanged, variable.Sign, v.Current, card, reason);
    }
    /// <summary>
    /// 增加值
    /// </summary>
    /// <param name="value">增加的值</param>
    /// <param name="reason">增加的原因</param>
    /// <param name="card">调用函数的卡牌</param>
    /// <param name="variableType">更改的英雄变量枚举值</param>
    /// <param name="impactType">效果影响类型</param>
    /// <returns></returns>
    public async Task Gain(int value, VariableReason reason, CardModel card, params object[] objects)
    {
        var old = variable.Current;
        IntVariable v = new("Gain",value);
        await CardCmd.TimingOnCards(null, Timing.ModifyHeroChanged, variable.Sign, value, card, reason, v);
        await CardCmd.TimingOnCards(null, Timing.BeforeHeroChanged, variable.Sign, v.Current, card, reason);
        variable.Gain(v.Current, reason);
        await hero.Changed(sign, old, variable.Current, Math.Abs(v.Current), reason, card, objects);
        await CardCmd.TimingOnCards(null, Timing.OnHeroChanged, variable.Sign, v.Current, card, reason);
        await CardCmd.TimingOnCards(null, Timing.AfterHeroChanged, variable.Sign, v.Current, card, reason);
    }
    /// <summary>
    /// 设置值
    /// </summary>
    /// <param name="value">设置的值</param>
    /// <param name="reason">设置的原因</param>
    /// <param name="card">调用函数的卡牌</param>
    /// <param name="variableType">更改的英雄变量枚举值</param>
    /// <param name="impactType">效果影响类型</param>
    /// <returns></returns>
    public async Task<int> Set(int value, VariableReason reason, CardModel card, params object[] objects)
    {
        var old = variable.Current;
        IntVariable v = new("Set", value);
        await CardCmd.TimingOnCards(null,Timing.ModifyHeroChanged, variable.Sign, value,card,reason,v);
        await CardCmd.TimingOnCards(null,Timing.BeforeHeroChanged, variable.Sign, v.Current,card,reason);
        int val = variable.Set(v.Current, reason);
        await hero.Changed(sign, old, variable.Current, Math.Abs(v.Current), reason, card, objects);
        await CardCmd.TimingOnCards(null, Timing.OnHeroChanged, variable.Sign, v.Current, card,reason);
        await CardCmd.TimingOnCards(null, Timing.AfterHeroChanged, variable.Sign, v.Current, card,reason);
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
    public async Task Rewind(int Index,CardModel card, params object[] objects)
    {
        var old = variable.Current;
        VariableHistory history = variable.GetHistory().First(his => his.Index == Index);
        var value = history.Before;
        IntVariable v = new("Rewind",(int)value);
        await CardCmd.TimingOnCards(null, Timing.ModifyHeroChanged, variable.Sign, card, VariableReason.Reset, v);
        await CardCmd.TimingOnCards(null, Timing.BeforeHeroChanged, variable.Sign, v.Current, card, VariableReason.Reset);
        variable.Rewind(Index);
        await hero.Changed(sign, old, variable.Current, Math.Abs(v.Current), VariableReason.Reset, card, objects);
        await CardCmd.TimingOnCards(null, Timing.OnHeroChanged, variable.Sign, v.Current, card, VariableReason.Reset);
        await CardCmd.TimingOnCards(null, Timing.AfterHeroChanged, variable.Sign, v.Current, card, VariableReason.Reset);
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
    public async Task RewindReasonable(int Index, VariableReason reason, CardModel card, params object[] objects)
    {
        var old = variable.Current;
        VariableHistory history = variable.GetHistory().First(his => his.Index == Index);
        var value = history.Before;
        IntVariable v = new("Rewind",(int)value);
        await CardCmd.TimingOnCards(null, Timing.ModifyHeroChanged, variable.Sign, card, reason, v);
        await CardCmd.TimingOnCards(null, Timing.BeforeHeroChanged, variable.Sign, v.Current, card, reason);
        variable.RewindReasonable(Index, reason);
        await hero.Changed(sign, old, variable.Current, Math.Abs(v.Current), reason, card, objects);
        await CardCmd.TimingOnCards(null, Timing.OnHeroChanged, variable.Sign, v.Current, card, reason);
        await CardCmd.TimingOnCards(null, Timing.AfterHeroChanged, variable.Sign, v.Current, card, reason);
    }

    /// <summary>
    /// 重置值
    /// </summary>
    /// <param name="card">调用函数的卡牌</param>
    /// <param name="variableType">更改的英雄变量枚举值</param>
    /// <param name="impactType">效果影响类型</param>
    /// <returns></returns>
    public async Task Reset(CardModel card, params object[] objects)
    {
        var old = variable.Current;
        await CardCmd.TimingOnCards(null, Timing.BeforeHeroChanged, variable.Sign, variable.Current - variable.Base, card, VariableReason.Reset);
        variable.Reset();
        await hero.Changed(sign, old, variable.Current, Math.Abs(old - variable.Current), VariableReason.Reset, card, objects);
        await CardCmd.TimingOnCards(null, Timing.OnHeroChanged, variable.Sign, variable.Current - variable.Base, card, VariableReason.Reset);
        await CardCmd.TimingOnCards(null, Timing.AfterHeroChanged, variable.Sign, variable.Current - variable.Base, card, VariableReason.Reset);
    }
}