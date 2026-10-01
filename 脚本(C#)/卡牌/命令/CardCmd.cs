using Battle;
using Battle.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;
using Target;

namespace Card.Cmd;

/// <summary>
/// 用于卡牌的命令
/// </summary>
public static class CardCmd
{
    private static List<CardModel> CardInstances = new List<CardModel>();
    /// <summary>
    /// 向Cmd添加卡牌
    /// </summary>
    /// <param name="cardModel"></param>
    public static void Add(CardModel cardModel)
    {
        CardInstances.Add(cardModel);
    }
    /// <summary>
    /// 从Cmd删除卡牌
    /// </summary>
    /// <param name="cardModel"></param>
    public static void Remove(CardModel cardModel)
    {
        CardInstances.Remove(cardModel);
    }
    /// <summary>
    /// 修改卡牌后执行所有卡牌修改卡牌的逻辑
    /// </summary>
    /// <typeparam name="T">修改卡牌变量的类型</typeparam>
    /// <param name="cardModel">被修改的卡牌</param>
    /// <param name="before">变量修改前的值</param>
    /// <param name="after">变量修改后的值</param>
    /// <returns></returns>
    public static async Task ChangedCard<T>(CardModel cardModel, string key, T before, T after)
    {
        cardModel.Fresh();
        await cardModel.Changed<T>(key, before, after);
        foreach (CardModel card in CardInstances)
        {
            await card.AfterCardBeChanged(cardModel, key, before, after);
        }
    }
    /// <summary>
    /// 打出卡牌后执行所有卡牌打出卡牌的逻辑
    /// </summary>
    /// <param name="cardModel">打出的卡牌</param>
    /// <param name="target">打出卡牌的对象</param>
    /// <returns></returns>
    public static async Task PlayedCard(CardModel cardModel, ITarget target)
    {
        await cardModel.Play(target);
        foreach (CardModel card in CardInstances)
        {
            await card.AfterPlayCard(cardModel, target);
        }
    }
    /// <summary>
    /// 生成战斗单位
    /// </summary>
    /// <param name="fighter">战斗单位卡牌</param>
    /// <param name="road">打出的道路</param>
    /// <param name="location">打出的位置</param>
    /// <returns></returns>
    public static async Task FighterGenerate(FighterCardModel fighter,Road road,Location location)
    {
        var fight = Fighter.Generate(fighter,road,location);
        await fighter.IntroPlayed();
        await fighter.AnimationWhenPlayed(road);
        await fighter.AfterPlayed(road);
    }
    /// <summary>
    /// 从卡组中抽出卡牌
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public static async Task Draw(int amount = 1)
    {
    }
}
