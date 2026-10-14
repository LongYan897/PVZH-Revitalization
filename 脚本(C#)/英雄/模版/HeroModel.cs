using Battle;
using Battle.Entity;
using Card;
using Card.Cmd;
using Controller;
using Godot;
using Hero.String;
using Play;
using Scene;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Variable;

namespace Hero.Model;
/// <summary>
/// 英雄模板(继承用)
/// </summary>
public abstract class HeroModel
{
    /// <summary>
    /// 超能力
    /// </summary>
    public IEnumerable<CardModel> SuperPower { get; private set; }
    /// <summary>
    /// 动画路径
    /// </summary>
    public string AnimPath { get; private set; }
    /// <summary>
    /// 阵营
    /// </summary>
    public Camp Camp {  get; private set; }
    /// <summary>
    /// 当前血量
    /// </summary>
    public HeroIntVariable Health { get; private set; }
    /// <summary>
    /// 最大血量
    /// </summary>
    public HeroIntVariable MaxHealth { get; private set; }
    /// <summary>
    /// 当前超能格挡值
    /// </summary>
    public HeroIntVariable Shield { get; private set; }
    /// <summary>
    /// 当前阳光/脑子数
    /// </summary>
    public HeroIntVariable Cost { get; private set; }
    /// <summary>
    /// 英雄数据
    /// </summary>
    public HeroString Data {  get; private set; }
    /// <summary>
    /// 英雄变量被改变
    /// </summary>
    /// <returns></returns>
    public async Task Changed(string sign,int before,int after,int delta,VariableReason reason,CardModel cardModel,params object[] objects)
    {
        if (reason == VariableReason.None) return;
        if (sign == "MaxHealth")
        {
            if (Health.Current > after)
            {
                await Health.Set(after,VariableReason.None,null);
            }
        }
        else if (sign == "Cost")
        {
            if (after - before > 0)
            {
                await BattleSubMenuScene.PlayCostGain(PlayerManager.GetFromHero(this), delta);
            }
            else if (after - before < 0)
                await BattleSubMenuScene.PlayCostLose(PlayerManager.GetFromHero(this), delta);
        }
    }
    private async void PlayAnimation(string name, bool returnToIdle, int track = 0)
    {
        await HeroInstance.GetNode(this).PlayAnimation(name,returnToIdle,track);
    }
    private async Task PlayAnimationTask(string name,bool returnToIdle, int track = 0)
    {
        await HeroInstance.GetNode(this).PlayAnimation(name, returnToIdle, track);
    }
    private IAttackable Targeted()
    {
        return HeroInstance.GetNode(this);
    }
    /// <summary>
    /// 统一伤害入口
    /// </summary>
    /// <param name="amount">伤害量</param>
    /// <param name="reason">原因</param>
    /// <param name="stack">攻击上下文，非攻击伤害传 null</param>
    public async Task ApplyDamage(
    int amount,
    Variable.VariableReason reason,
    CardModel cardModel = null,
    AtkStack stack = null)
    {
        Random random = new();
        int shield = random.Next(1,4);
        Variable<bool> stop = new("HeroDamaged", false);
        stack ??= new(null, [Targeted()], null, amount);
        await CardCmd.TimingOnCards(null, Timing.TryStopHeroTakeDamage, stop,this);
        PlayAnimation("hit", true);
        await BattleSubMenuScene.PlayHurt(PlayerManager.GetFromHero(this), amount, shield, cardModel);
        if (shield + Shield.Current >= 8)
        {
            await Shield.Reset(null);
            return;
        }
        if (stop.Current) return;
        await CardCmd.TimingOnCards(null, Timing.ModifyCardDamage, stack);
        await Health.Lose(stack.FinalDamage, reason, cardModel ?? stack.Attacker);
        await CardCmd.TimingOnCards(null, Timing.OnDamaged, stack);
        await Shield.Gain(shield, Variable.VariableReason.Damaged, cardModel);

        if (Health.Current <= 0)
        {
            
        }
    }
    /// <summary>
    /// 加载英雄
    /// </summary>
    /// <typeparam name="T">英雄类</typeparam>
    /// <returns></returns>
    public static HeroModel Load<T>() where T : HeroModel,new()
    {
        return Load<T>(HeroString.Loading(typeof(T).Name));
    }
    private static T Load<T>(HeroString heroString) where T : HeroModel,new()
    {
        T model = new T
        {
            SuperPower = heroString.SuperPower,
            Camp = heroString.Camp,
            AnimPath = heroString.AnimationPath,
            Data = heroString
        };
        return model;
    }
    /// <summary>
    /// 为战斗初始化
    /// </summary>
    public void SetUp()
    {
        var model = this;
        model.Cost = new(model, "Cost", 0);
        model.Shield = new(model, "Shield", 0);
        model.MaxHealth = new(model, "MaxHealth", 20);
        model.Health = new(model, "Health", 20);
    }
    /// <summary>
    /// 通过派系和阵营查找所有相关英雄的类
    /// </summary>
    /// <param name="camp"></param>
    /// <param name="category"></param>
    /// <returns></returns>
    public static List<Type> SeekHeroModel(string camp = null,string category = null) 
    {
        List<Type> heroModels = new List<Type>();
        List<Type> types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(HeroModel))).ToList();
        foreach(Type type in types)
        {
            string name = type.Name;
            if (camp == null || HeroString.GetData(name, "Camp").AsString() == camp && (category == null || HeroString.GetData(name, "Category").AsStringArray().Contains(category)))
            {
                heroModels.Add(type);
            }
        }
        return heroModels;
    }
}
