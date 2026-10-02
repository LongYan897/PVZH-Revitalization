using Battle.Entity;
using Card.Cmd;
using Card.String;
using Controller;
using Godot;
using Logger;
using Pack;
using Phrases;
using Play;
using Spine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Target;
using Variable;

namespace Card;

/// <summary>
/// 卡牌模板(继承用)
/// </summary>
public class CardModel
{
    public virtual Vector2 Size => new(1, 1);
    public virtual Vector2 Position => new(0, 0);
    public Player Player { get; private set; }
    public static readonly List<CardModel> AllCards = TemplateEntries.Values.ToList();
    private static readonly Dictionary<string, CardModel> TemplateEntries = new();
    /// <summary>
    /// 按照名称获取卡牌模板
    /// </summary>
    public static CardModel GetTemplate(string title)
    {
        if (TemplateEntries.TryGetValue(title, out var template))
            return template.Clone();
        return null;
    }

    /// <summary>
    /// 是否存在该名称的卡牌模板
    /// </summary>
    public static bool HasTemplate(string title)
    {
        return TemplateEntries.ContainsKey(title);
    }
    /// <summary>
    /// 卡牌的模板(用于获取卡牌数据)(不要修改)
    /// </summary>
    public static CardModel GetTemplate<T>() where T : CardModel
    {
        return Load<T>();
    }
    /// <summary>
    /// 初始化卡牌基类
    /// </summary>
    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var loadMethod = typeof(CardModel)
            .GetMethod(nameof(Load), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);

        foreach (var type in assembly.GetTypes())
        {
            if (!typeof(CardModel).IsAssignableFrom(type)) continue;
            if (type.IsAbstract) continue;
            if (type == typeof(CardModel)) continue;
            if (typeof(FighterCardModel) == type) continue;

            try
            {
                var cardString = CardString.Loading(type.Name);
                var loadData = typeof(CardModel)
                    .GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static);

                var generic = loadData.MakeGenericMethod(type);
                var model = (CardModel)generic.Invoke(null, new object[] { cardString, null });
                if (!string.IsNullOrEmpty(model.Title))
                    TemplateEntries[model.Title] = model;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + ex.StackTrace);
            }
        }
    }
    /// <summary>
    /// 刷新卡牌的节点
    /// </summary>
    public void Fresh()
    {
        NodeCard.GetNode(this)?.Fresh();
        if (this is FighterCardModel fighter)
            Fighter.GetNode(fighter)?.CardTryFresh(this);
        CardDes.instance?.CardTryFresh(this);
    }
    public async Task DestroyMe()
    {
        await Destroy();
    }
    /// <summary>
    /// 是否能把友军当作目标
    /// </summary>
    public virtual bool FriendTarget => false;
    /// <summary>
    /// 卡牌费用图片
    /// </summary>
    public Texture2D CampCostIcon
    {
        get
        {
            return Camp switch
            {
                Camp.Plant => GD.Load<Texture2D>("res://素材/卡牌属性图片/inhnd_sun_gb.png"),
                Camp.Zombie => GD.Load<Texture2D>("res://素材/卡牌属性图片/inhnd_brains.png"),
                _ => GD.Load<Texture2D>("res://素材/卡牌属性图片/inhnd_sun_gb.png"),
            };
        }
    }
    protected async Task PlayInstantAnimation(string animName, ITarget target, int track = 0)
    {
        var node = Main.Animator;
        var sp = SpineHandler.Get();
        sp.Position = ((Control)(GodotObject)target).GlobalPosition;
        sp.Scale = new(0.7f, 0.7f);
        node.AddChild(sp);
        sp.LoadSkeletonData(AnimationPath);
        await sp.SetAnimationAndFreeOnEndTask(track, animName, false);
    }
    protected async Task PlayLoopAnimation(string animName, ITarget target,int track = 0)
    {
        var node = Main.Animator;
        var sp = SpineHandler.Get();
        sp.Position = ((Control)(GodotObject)target).GlobalPosition;
        sp.Scale = new(0.7f, 0.7f);
        node.AddChild(sp);
        sp.LoadSkeletonData(AnimationPath);
        await sp.SetAnimationTask(track, animName, true);
    }
    public static CardModel Load<T>() where T : CardModel
    {
        var model = Load<T>(CardString.Loading(typeof(T).Name));
        CardCmd.Add(model);
        return model;
    }
    /// <summary>
    /// 从CardString加载卡牌数据
    /// </summary>
    /// <param name="cardString">目标String</param>
    /// <returns>加载出来的卡牌</returns>
    private static T Load<T>(CardString cardString, Player player = null) where T : CardModel
    {
        var card = Activator.CreateInstance<T>();
        card.Player = player;
        card.Cost = new CardIntVariable(card, "Cost", cardString.Cost);
        card.Title = cardString.Title;
        card.Description = cardString.Description;
        card.Flavor = cardString.Flavor;
        card.Rarity = cardString.Rarity;
        card.Class = cardString.Category;
        card.Camp = cardString.Camp;
        card.CardType = cardString.CardType;
        card.Pack = cardString.Pack;
        card.AnimationPath = cardString.AnimationPath;
        card.Icon = GD.Load<PackedScene>(cardString.IconPath);
        card.Labels = new CardVariable<List<string>>(card, "Labels", cardString.Labels);
        return (T)card.LoadData(cardString);
    }
    private async Task Destroy()
    {
        CardCmd.Remove(this);
        await NodeCard.DestoryCard(this);
    }
    /// <summary>
    /// 目标选择
    /// </summary>
    public virtual TargetType TargetType { get; } = TargetType.Fighters;
    /// <summary>
    /// 子类加载卡牌数据
    /// </summary>
    /// <param name="cardString">目标String</param>
    /// <returns>加载出来的卡牌</returns>
    protected virtual CardModel LoadData(CardString cardString)
    {
        return this;
    }
    /// <summary>
    /// 这个是筛选能成为目标的函数
    /// </summary>
    /// <param name="target">目标</param>
    /// <returns></returns>
    public virtual bool TargetFilter(ITarget target)
    {
        return true;
    }

    [IconText("ffffff")]
    public const string Sun = "res://素材(C#)/文本图片/inhnd_sun_gb.png";

    [IconText("ffffff")]
    public const string Brain = "res://素材(C#)/文本图片/inhnd_brains.png";
    /// <summary>
    /// 卡牌的特殊标签
    /// </summary>
    public string ExDescription { get; set; }
    /// <summary>
    /// 卡牌的标题(名字)
    /// </summary>
    public string Title { get; private set; }
    /// <summary>
    /// 卡牌的正规描述
    /// </summary>
    public string Description { get; private set; }
    /// <summary>
    /// 卡牌的非正规描述
    /// </summary>
    public string Flavor { get; private set; }
    /// <summary>
    /// 卡牌的卡包
    /// </summary>
    public string Pack { get; private set; }
    /// <summary>
    /// 卡牌的动画路径
    /// </summary>
    public string AnimationPath { get; private set; }
    /// <summary>
    /// 卡牌的稀有度
    /// </summary>
    public Rarity Rarity { get; private set; }
    /// <summary>
    /// 卡牌的派系
    /// </summary>
    public Class Class { get; private set; }
    /// <summary>
    /// 卡牌的阵营
    /// </summary>
    public Camp Camp { get; private set; }

    /// <summary>
    /// 卡牌的状态（正面/反面）
    /// </summary>
    public Status Status { get; set; } = Status.FaceDown;
    /// <summary>
    /// 卡牌的类别
    /// </summary>
    public CardType CardType { get; private set; }
    /// <summary>
    /// 卡牌的标签
    /// </summary>
    public CardVariable<List<string>> Labels { get; private set; }
    /// <summary>
    /// 卡牌的特殊变量
    /// </summary>
    private readonly List<ICardVariable> _variables = new List<ICardVariable>();
    /// <summary>
    /// 卡牌的特殊变量
    /// </summary>
    public IReadOnlyList<ICardVariable> Variables => _variables;
    /// <summary>
    /// 卡牌的费用
    /// </summary>
    public CardIntVariable Cost { get; private set; }
    /// <summary>
    /// 卡牌的卡面
    /// </summary>
    public PackedScene Icon { get; private set; }
    /// <summary>
    /// 卡牌打出时的效果
    /// </summary>
    /// <param name="target">游戏传入的卡牌目标</param>
    /// <returns></returns>
    public virtual Task Play(ITarget target) { return Task.CompletedTask; }
    /// <summary>
    /// 克隆之后的逻辑
    /// </summary>
    /// <param name="card"></param>
    protected virtual void AfterClone() { }
    /// <summary>
    /// 克隆卡牌
    /// </summary>
    /// <returns></returns>
    public CardModel Clone()
    {
        CardModel card = MemberwiseClone() as CardModel;
        card.AfterClone();
        return card;
    }
    /// <summary>
    /// 子类重置卡牌数值的逻辑
    /// </summary>
    protected virtual void ResetData()
    { }
    /// <summary>
    /// 重置卡牌的数值
    /// </summary>
    public async Task Reset()
    {
        await Cost.Reset();
        await Labels.Reset();
        ExDescription = null;
        ResetData();
    }
    /// <summary>
    /// 修改这张卡牌时
    /// </summary>
    /// <typeparam name="T">修改值的类型</typeparam>
    /// <param name="valueAfter">修改之前的值</param>
    /// <param name="valueBefore">修改之后的值</param>
    /// <returns></returns>
    public virtual Task Changed<T>(string key, T valueAfter, T valueBefore)
    {
        return Task.CompletedTask;
    }
    /// <summary>
    /// 挂在卡牌上的效果
    /// </summary>
    private List<Buff> _buffs;

    private List<Buff> BuffList
    {
        get
        {
            _buffs ??= [.. InitBuffs];
            return _buffs;
        }
    }

    public IReadOnlyList<Buff> Buffs => BuffList;
    /// <summary>
    /// 卡牌初始的Buff
    /// </summary>
    protected virtual List<Buff> InitBuffs { get; } = [];
    /// <summary>
    /// 添加效果
    /// </summary>
    public async Task AddBuff(Buff buff, VariableReason reason)
    {
        BuffList.Add(buff);
        await buff.OnTiming(Timing.WhenApply, this, reason);
    }

    /// <summary>
    /// 移除效果
    /// </summary>
    public async Task RemoveBuff(Buff buff, VariableReason reason)
    {
        BuffList.Remove(buff);
        await buff.OnTiming(Timing.WhenRemove, this, reason);
    }

    /// <summary>
    /// 把时点转发给所有关心它的 Buff
    /// </summary>
    public async Task FireTiming(Timing timing, params object[] parameters)
    {
        foreach (var buff in BuffList.ToList())
        {
            if (buff.Timings.Contains(timing))
                await buff.OnTiming(timing, this,parameters);
        }
    }

    /// <summary>
    /// 把 Buff 转移到新卡
    /// </summary>
    public async Task TransferBuffsTo(CardModel newCard)
    {
        foreach (var buff in BuffList.ToList())
        {
            await RemoveBuff(buff,VariableReason.Self);
            await newCard.AddBuff(buff,VariableReason.Self);
        }
    }

}
