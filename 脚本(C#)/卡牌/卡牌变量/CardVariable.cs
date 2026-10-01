
using Card.Cmd;
using System.Linq;
using System.Threading.Tasks;
using Variable;
using Variable.Special;

namespace Card;

/// <summary>
/// 卡牌的变量(卡牌必须使用该变量,需要用该变量监听卡牌修改事件)
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="cardModel"></param>
/// <param name="sign"></param>
/// <param name="obj"></param>
public class CardVariable<T>(CardModel cardModel, string sign, T obj) : ICardVariable
{
    private CardModel card = cardModel;
    private Variable<T> variable = new Variable<T>(sign, obj);
    public T Current => variable.Current;
    public bool HasChanged => variable.HasChanged;
    /// <summary>
    /// 设置值
    /// </summary>
    /// <param name="value"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    public async Task<int> Set(T value, VariableReason reason)
    {
        var val = variable.Current;
        int v = variable.Set(value, reason);
        await CardCmd.ChangedCard(card, sign, val, value);
        return v;
    }
    /// <summary>
    /// 回溯值
    /// </summary>
    /// <param name="Index">回溯的历史时期</param>
    /// <returns></returns>
    public async Task Rewind(int Index)
    {
        VariableHistory history = variable.GetHistory().First(his => his.Index == Index);
        var old = variable.Current;
        var value = history.Before;
        variable.Rewind(Index);
        await CardCmd.ChangedCard(card, sign, old, value);
    }
    /// <summary>
    /// 回溯值(不清空历史)
    /// </summary>
    /// <param name="Index">回溯的历史时期</param>
    /// <param name="reason">回溯的原因</param>
    /// <returns></returns>
    public async Task RewindReasonable(int Index, VariableReason reason)
    {
        VariableHistory history = variable.GetHistory().First(his => his.Index == Index);
        var old = variable.Current;
        var value = history.Before;
        variable.RewindReasonable(Index, reason);
        await CardCmd.ChangedCard(card, sign, old, value);
    }

    /// <summary>
    /// 重置值
    /// </summary>
    /// <returns></returns>
    public async Task Reset()
    {
        var old = variable.Current;
        variable.Reset();
        await CardCmd.ChangedCard(card, sign, old, variable.Current);
    }
}

public interface ICardVariable
{
}

/// <summary>
/// 卡牌的整数变量(卡牌必须使用该变量,需要用该变量监听卡牌修改事件)
/// </summary>
/// <param name="cardModel">监听的卡牌</param>
/// <param name="sign">监听的信号</param>
/// <param name="baseValue">基础值</param>
/// <param name="canNegative">是否可以为负数</param>
public class CardIntVariable(CardModel cardModel, string sign, int baseValue,bool canNegative = false)
{
    private CardModel card = cardModel;
    private IntVariable variable = new IntVariable(sign, baseValue);
    public int Current => variable.Current;
    public bool HasChanged => variable.HasChanged;
    private bool canNegative = canNegative;
    /// <summary>
    /// 增加值
    /// </summary>
    /// <param name="value">要增加的值</param>
    /// <param name="reason">原因</param>
    public async Task Gain(int value, VariableReason reason)
    {
        var old = variable.Current + value;
        variable.Gain(value, reason);
        await CardCmd.ChangedCard(card, sign, old, value);
    }
    /// <summary>
    /// 减少值
    /// </summary>
    /// <param name="value">要减少的值</param>
    /// <param name="reason">原因</param>
    public async Task Lose(int value, VariableReason reason)
    {
        var old = variable.Current - value;
        if (!canNegative && old < 0)
        {
            old = 0;
            value = variable.Current;
        }
        variable.Lose(value, reason);
        await CardCmd.ChangedCard(card, sign, old, value);
    }
    /// <summary>
    /// 设置值
    /// </summary>
    /// <param name="value"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    public async Task<int> Set(int value, VariableReason reason)
    {
        var val = variable.Current;
        int v = variable.Set(value, reason);
        await CardCmd.ChangedCard(card, sign, val, value);
        return v;
    }

    /// <summary>
    /// 回溯值
    /// </summary>
    /// <param name="Index">回溯的历史时期</param>
    /// <returns></returns>
    public async Task Rewind(int Index)
    {
        VariableHistory history = variable.GetHistory().First(his => his.Index == Index);
        var old = variable.Current;
        var value = history.Before;
        variable.Rewind(Index);
        await CardCmd.ChangedCard(card, sign, old, value);
    }

    /// <summary>
    /// 回溯值(不清空历史)
    /// </summary>
    /// <param name="Index">回溯的历史时期</param>
    /// <param name="reason">回溯的原因</param>
    /// <returns></returns>
    public async Task RewindReasonable(int Index, VariableReason reason)
    {
        VariableHistory history = variable.GetHistory().First(his => his.Index == Index);
        var old = variable.Current;
        var value = history.Before;
        variable.RewindReasonable(Index, reason);
        await CardCmd.ChangedCard(card, sign, old, value);
    }
    /// <summary>
    /// 重置值
    /// </summary>
    /// <returns></returns>
    public async Task Reset()
    {
        var old = variable.Current;
        variable.Reset();
        await CardCmd.ChangedCard(card, sign, old, variable.Current);
    }
    /// <summary>
    /// 是否是相较于Base的值减少
    /// </summary>
    public bool NegitiveChanged => variable.NegitiveChanged;
    /// <summary>
    /// 是否是相较于Base的值增加
    /// </summary>
    public bool PositiveChanged => variable.PositiveChanged;
}
