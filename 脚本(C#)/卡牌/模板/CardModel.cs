using Card.String;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Target;
using Variable;
using Variable.Special;

namespace Card;

/// <summary>
/// 卡牌模板(继承用)
/// </summary>
public class CardModel
{
    public virtual Texture2D Icon { get; } = GD.Load<Texture2D>("res://素材(C#)/卡牌/豌豆射手/Pea.png");
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
    public static CardModel Load<T>() where T : CardModel
    {
        return Load<T>(CardString.Loading(typeof(T).Name));
    }
    /// <summary>
    /// 从CardString加载卡牌数据
    /// </summary>
    /// <param name="cardString">目标String</param>
    /// <returns>加载出来的卡牌</returns>
    private static T Load<T>(CardString cardString) where T : CardModel
    {
        var card = Activator.CreateInstance<T>();
        card.Cost = new IntVariable("Cost",cardString.Cost);
        card.Rarity = cardString.Rarity;
        card.Class = cardString.Category;
        card.Camp = cardString.Camp;
        card.CardType = cardString.CardType;
        card.Labels = new Variable<List<string>>("Labels",cardString.Labels);
        return (T)card.LoadData(cardString);
    }
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
    public Camp Camp { get; private set;  }

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
    public Variable<List<string>> Labels { get; private set; }
    /// <summary>
    /// 卡牌的特殊变量
    /// </summary>
    private readonly List<IVariable> _variables = new List<IVariable>();
    /// <summary>
    /// 卡牌的特殊变量
    /// </summary>
    public IReadOnlyList<IVariable> Variables => _variables;
    /// <summary>
    /// 卡牌的费用
    /// </summary>
    public IntVariable Cost { get; private set; }
    /// <summary>
    /// 卡牌打出时的效果
    /// </summary>
    /// <param name="target">游戏传入的卡牌目标</param>
    /// <returns></returns>
    public virtual Task Play(ITarget target) {  return Task.CompletedTask; }
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
    public void Reset()
    {
        Cost.Reset();
        ResetData();
    }

}
