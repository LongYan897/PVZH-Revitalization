using Battle;
using Battle.Entity;
using Card.String;
using Logger;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Target;
using Variable;
using Variable.Special;

namespace Card;

/// <summary>
/// 单位类型的卡牌
/// </summary>
public class FighterCardModel : CardModel
{
    /// <summary>
    /// 从CardString加载卡牌数据
    /// </summary>
    /// <param name="cardString">目标String</param>
    /// <returns>加载出来的卡牌</returns>
    protected override CardModel LoadData(CardString cardString)
    {
        Atk = new(this,"Attack", cardString.Attack);
        AtkType = new(this,"AttackType", cardString.AtkType);
        Hp = new(this,"Health", cardString.Health);
        HpType = new(this,"HealthType",cardString.HpType);
        return LoadCustomData(cardString);
    }
    /// <summary>
    /// 检测能否被Card当作对象
    /// </summary>
    /// <param name="cardModel"></param>
    /// <returns></returns>
    protected virtual bool CanTargetedBy(CardModel cardModel) { return true; }
    /// <summary>
    /// 检测能否被Card当作对象
    /// </summary>
    /// <param name="cardModel"></param>
    /// <returns></returns>
    public bool CanbeTarget(CardModel cardModel)
    {
        bool hpbol = HpType.Current.ExtraFilter(cardModel);
        return hpbol && CanTargetedBy(cardModel);
    }
    /// <summary>
    /// 播放单位的动画
    /// </summary>
    /// <param name="name">动画名称</param>
    /// <returns></returns>
    protected async Task PlayFighterAnimation(string name, bool returnToIdle = true)
    {
        var fighter = Fighter.GetNode(this);
        await fighter.PlayAnimation(name, returnToIdle);
    }
    /// <summary>
    /// 子类加载卡牌数据
    /// </summary>
    /// <param name="cardString">目标String</param>
    /// <returns>加载出来的卡牌</returns>
    protected virtual CardModel LoadCustomData(CardString cardString) { return this; }
    /// <summary>
    /// 卡牌的攻击力
    /// </summary>
    public CardIntVariable Atk {  get;private set; }
    /// <summary>
    /// 卡牌的攻击类型
    /// </summary>
    public CardVariable<AtkType> AtkType {  get; private set; }
    /// <summary>
    /// 卡牌的生命值
    /// </summary>
    public CardIntVariable Hp { get; private set; }
    /// <summary>
    /// 卡牌的生命值类型
    /// </summary>
    public CardVariable<HpType> HpType { get; private set; }
    /// <summary>
    /// 在战斗开始后(攻击之前)
    /// </summary>
    /// <param name="road">所处的战斗道路</param>
    /// <returns></returns>
    public virtual Task AfterLaneStart(Road road) { return Task.CompletedTask; }
    /// <summary>
    /// 在战斗开始前(当战斗进行到这条线时优先执行)
    /// </summary>
    /// <param name="road">所处的战斗道路</param>
    /// <returns></returns>
    public virtual Task BeforeLaneStart(Road road) { return Task.CompletedTask; }
    /// <summary>
    /// 被打出后
    /// </summary>
    /// <param name="road">所处的战斗道路</param>
    /// <returns></returns>
    public virtual Task AfterPlayed(Road road) {  return Task.CompletedTask; }
    public override async Task Changed<T>(string key,T valueAfter, T valueBefore)
    {
        if (key == "Health")
        {
            await PlayFighterAnimation("hurt");
            if (Hp.Current <= 0)
                await Fighter.GetNode(this).Die();
        }
        await ChangedMe<T>(key, valueAfter, valueBefore);
    }
    protected virtual Task ChangedMe<T>(string key, T valueAfter, T valueBefore) => Task.CompletedTask;
    public override async Task Play(ITarget target)
    {
        await DestroyMe();
    }
}
