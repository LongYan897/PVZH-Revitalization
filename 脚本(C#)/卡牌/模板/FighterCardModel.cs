using Card.String;
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
        Atk = new("Attack", cardString.Attack);
        AtkType = new("AttackType", cardString.AtkType);
        Hp = new("Health", cardString.Health);
        HpType = new("HealthType",cardString.HpType);
        return LoadCustomData(cardString);
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
    public IntVariable Atk {  get;private set; }
    /// <summary>
    /// 卡牌的攻击类型
    /// </summary>
    public Variable<AtkType> AtkType {  get; private set; }
    /// <summary>
    /// 卡牌的生命值
    /// </summary>
    public IntVariable Hp { get; private set; }
    /// <summary>
    /// 卡牌的生命值类型
    /// </summary>
    public Variable<HpType> HpType { get; private set; }
}
