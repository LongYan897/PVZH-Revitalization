using Logger;
using System;
using System.Threading.Tasks;
using Variable;

namespace Card;

/// <summary>
/// 卡牌效果的全局类
/// 建议如果是致命这种全局统一且常用的效果
/// 可以新建类继承Buff比较方便
/// </summary>
/// <param name="action">对目标卡牌的操作</param>
public class Buff(Func<CardModel,Task> action)
{
    /// <summary>
    /// 操作的卡牌
    /// </summary>
    private readonly Variable<CardModel> _card = new("Base",null);
    /// <summary>
    /// 对目标卡牌的操作
    /// </summary>
    public readonly Func<CardModel,Task> Action = action;
    /// <summary>
    /// 操作卡牌
    /// </summary>
    /// <param name="cardModel">操作的对象</param>
    /// <param name="reason">操作的原因</param>
    public async Task Result(CardModel cardModel,VariableReason reason)
    {
        if (Action == null)
        {
            Log.Error("Buff不能没有Action变量或者Action是null");
            return;
        }
        if (_card.Current != null) return;
        _card.Set(cardModel.Clone(),reason);
        await Action(cardModel);
    }
    /// <summary>
    /// 撤销效果
    /// </summary>
    /// <returns>返回撤销效果后的卡牌</returns>
    public CardModel Withdraw()
    {
        var card = _card.Current;
        _card.Reset();
        return card;
    }
}
