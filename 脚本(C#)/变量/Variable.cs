using Logger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Variable;

/// <summary>
/// 游戏中的变量
/// 可以获取该变量修改的历史
/// 建议凡是需要大量修改的均使用该类
/// </summary>
public interface IVariable
{
    /// <summary>
    /// 变量的标志 存储变量时该标志不应该重复
    /// </summary>
    string Sign { get; }
    /// <summary>
    /// 设置该变量的值
    /// </summary>
    /// <param name="value">要设置的值</param>
    /// <param name="reason">设置的原因</param>
    /// <returns>返回历史时期的顺序</returns>
    int Set(object value, VariableReason reason);
    /// <summary>
    /// 重置变量的值为基础值
    /// </summary>
    void Reset();
    /// <summary>
    /// 重置变量的值为某个历史时期的值 即回退(!!!回退会清除指定历史时期之后的历史)
    /// </summary>
    /// <param name="Index">对应时期的顺序 从0开始</param>
    void Rewind(int Index);
    /// <summary>
    /// 回退到最晚历史时期
    /// </summary>
    void Rewind();
    /// <summary>
    /// 回退但是不会删除历史记录
    /// </summary>
    /// <param name="Index">对应时期的顺序 从0开始</param>
    /// <param name="reason">回退的原因</param>
    void RewindReasonable(int Index, VariableReason reason);
    /// <summary>
    /// 回退到最晚历史时期但是不会删除历史记录
    /// </summary>
    /// <param name="reason">回退的原因</param>
    void RewindReasonable(VariableReason reason);
    /// <summary>
    /// 检查是否曾经历史包含某个原因
    /// </summary>
    /// <param name="reason">包含的原因</param>
    /// <returns>是否包含</returns>
    bool HasReasonHistory(VariableReason reason);
}
/// <summary>
/// 游戏中的变量<br/>
/// 可以获取该变量修改的历史<br/>
/// 建议凡是需要大量修改的均使用该类<br/>
/// </summary>
public class Variable<T> : IVariable
{
    /// <summary>
    /// 记录一次历史
    /// </summary>
    /// <param name="before">修改前的值</param>
    /// <param name="after">修改后的值</param>
    /// <param name="reason">原因</param>
    /// <returns>返回历史时期的顺序</returns>
    protected int Record(T before,T after,VariableReason reason)
    {
        int index = _history.Count;
        if (typeof(T).IsClass && typeof(T) != typeof(string) && before != null)
        {
            var method = typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            object rawClone = method.Invoke(before, null);
            _history.Add(new VariableHistory(index, (T)rawClone, after, reason));
        }
        else
            _history.Add(new VariableHistory(index, before, after, reason));
        return index;
    }
    /// <summary>
    /// 变量的构造
    /// </summary>
    /// <param name="sign">变量的标志</param>
    /// <param name="baseValue">变量的基础值</param>
    public Variable(string sign,T baseValue)
    {
        Sign = sign;
        Base = baseValue;
        Current = Base;
    }
    /// <summary>
    /// 变量的标志 存储变量时该标志不应该重复
    /// </summary>
    public string Sign { get; }
    /// <summary>
    /// 变量的基础值
    /// </summary>
    public T Base { get;protected set; }
    /// <summary>
    /// 变量的当前值
    /// </summary>
    public T Current {  get;protected set; }
    private readonly List<VariableHistory> _history = new();
    /// <summary>
    /// 变量修改的历史
    /// </summary>
    public IReadOnlyList<VariableHistory> History => _history;
    /// <summary>
    /// 设置该变量的值
    /// </summary>
    /// <param name="value">要设置的值</param>
    /// <param name="reason">设置的原因</param>
    /// <returns>返回历史时期的顺序</returns>
    public int Set(object value, VariableReason reason)
    {
        return Set(value, reason);
    }
    /// <summary>
    /// 设置该变量的值
    /// </summary>
    /// <param name="value">要设置的值</param>
    /// <param name="reason">设置的原因</param>
    /// <returns>返回历史时期的顺序</returns>
    public int Set(T value,VariableReason reason)
    {
        int index = Record(Current,value, reason);
        Current = value;
        return index;
    }
    /// <summary>
    /// 重置变量的值为基础值
    /// </summary>
    public void Reset()
    {
        Record(Current, Base, VariableReason.Reset);
        Current = Base;
    }
    /// <summary>
    /// 重置变量的值为某个历史时期的值 即回退(!!!回退会清除指定历史时期之后的历史)
    /// </summary>
    /// <param name="Index">对应时期的顺序 从0开始</param>
    public void Rewind(int Index)
    {
        if (Index >= _history.Count)
        {
            Log.Error($"Index : {Index} 已经大于该变量修改的历史总数 找不到对应的历史");
            return;
        }
        VariableHistory history = _history.First(his=>his.Index == Index);
        var before = (T)history.Before;
        Current = before;
        _history.RemoveAll(his=>his.Index >= Index);
    }
    /// <summary>
    /// 回退到最晚历史时期
    /// </summary>
    public void Rewind()
    {
        Rewind(_history.Count - 1);
    }
    /// <summary>
    /// 回退但是不会删除历史记录
    /// </summary>
    /// <param name="Index">对应时期的顺序 从0开始</param>
    /// <param name="reason">回退的原因</param>
    public void RewindReasonable(int Index,VariableReason reason)
    {
        if (Index >= _history.Count)
        {
            Log.Error($"Index : {Index} 已经大于该变量修改的历史总数 找不到对应的历史");
            return;
        }
        VariableHistory history = _history.First(his => his.Index == Index);
        var before = history.Before;
        Record(Current, (T)before, VariableReason.Reset);
        Current = (T)before;
    }
    /// <summary>
    /// 回退到最晚历史时期但是不会删除历史记录
    /// </summary>
    /// <param name="reason">回退的原因</param>
    public void RewindReasonable(VariableReason reason)
    {
        RewindReasonable(_history.Count - 1, reason);
    }
    /// <summary>
    /// 检查是否曾经历史包含某个原因
    /// </summary>
    /// <param name="reason">包含的原因</param>
    /// <returns>是否包含</returns>
    public bool HasReasonHistory(VariableReason reason)
    {
        if (_history.Any(his => his.Reason.HasFlag(reason)))
            return true;
        return false;
    }
    /// <summary>
    /// 检查是否已经被修改
    /// </summary>
    public bool HasChanged =>_history.Count > 0;
}
