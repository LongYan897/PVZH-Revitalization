using Battle.Entity;
using Card;
using Card.Cmd;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Variable;
using Environment = Battle.Entity.Environment;

namespace Battle;
/// <summary>
/// 选中的道路目标
/// </summary>
public enum RoadTargetKind { None, Line, Grid, CoopGrid }
/// <summary>
/// 道路的类型
/// </summary>
public enum RoadType
{
    /// <summary>
    /// 高地
    /// </summary>
    Height,
    /// <summary>
    /// 平地
    /// </summary>
    Ground,
    /// <summary>
    /// 水路
    /// </summary>
    Water
}
/// <summary>
/// 战斗道路
/// </summary>
public class Road
{
    public static bool IsBattling { get; private set; }
    /// <summary>
    /// 决定该道路是第几行的
    /// </summary>
    public int Index { get; init; }
    /// <summary>
    /// 该道路是否是朝向植物的
    /// </summary>
    public bool IsUp { get; init; }
    /// <summary>
    /// 决定该道路的类型(高地/平地/水路)
    /// </summary>
    public RoadType Type { get; init; } = RoadType.Height;

    /// <summary>
    /// 最近一次被选中的类型
    /// </summary>
    public RoadTargetKind LastTargetKind { get; internal set; } = RoadTargetKind.None;

    /// <summary>
    /// 该道路存储的战斗单位
    /// </summary>
    private readonly List<Fighter> fighters = new();
    /// <summary>
    /// 开始这条线上的战斗
    /// </summary>
    public async Task Start()
    {
        IsBattling = true;

        await CardCmd.TimingOnCards(null, Timing.BeforeLaneStart, this);

        foreach (var fighter in fighters.ToList())
        {
            await fighter.BeforeAtk(new AtkStack(null, [], this, 0));
        }

        await CardCmd.TimingOnCards(null, Timing.AfterLaneStart, this);
        var zombie = fighters.FirstOrDefault(f => f.Zombie);
        if (zombie != null)
            await zombie.Model.Battle(new AtkStack(zombie.Model, [], this, zombie.Model.Atk.Current));
        var plantf = fighters.FirstOrDefault(f => f.Plant1);
        if (plantf != null)
            await plantf?.Model?.Battle(new AtkStack(plantf.Model, [], this, plantf.Model.Atk.Current));
        var plant = fighters.FirstOrDefault(f => f.Plant2);
        if (plant != null)
            await plant?.Model?.Battle(new AtkStack(plant.Model, [], this, plant.Model.Atk.Current));
        foreach (var fighter in fighters.ToList())
        {
            await fighter.AfterAtk(new AtkStack(null, [], this, 0));
        }

        await CardCmd.TimingOnCards(null, Timing.BeforeLaneEnd, this);

        foreach (var fighter in fighters.ToList())
        {
            if (fighter.Model.IsDie)
                await fighter.Die();
        }

        await CardCmd.TimingOnCards(null, Timing.AfterLaneEnd, this);


        IsBattling = false;
    }
    private readonly Variable<Environment> _environment = new("Environment", null);
    /// <summary>
    /// 该道路的环境
    /// 只有道路类型是平地的才有
    /// </summary>
    public Environment Environment
    {
        get
        {
            return Type switch
            {
                RoadType.Ground => _environment.Current,
                _ => null
            };
        }
    }
    /// <summary>
    /// 添加战斗单位到该道路
    /// </summary>
    /// <param name="fighter">要添加的战斗单位</param>
    public async Task AddFighter(CardModel cardModel,Fighter fighter)
    {
        await cardModel.FireTiming(Timing.OnFighterEnter, this, fighter.Model);
        await CardCmd.TimingOnCards(cardModel, Timing.OnFighterEnter, this, fighter);
        fighters.Add(fighter);
        await cardModel.FireTiming(Timing.AfterRoadChanged, this);
        await CardCmd.TimingOnCards(cardModel,Timing.AfterRoadChanged,this);
    }
    /// <summary>
    /// 移除战斗单位从该道路
    /// </summary>
    /// <param name="fighter">要移除的战斗单位</param>


    public async Task RemoveFighter(CardModel cardModel, Fighter fighter)
    {
        await cardModel.FireTiming(Timing.OnFighterExit, this, fighter.Model);
        fighters.Remove(fighter);
        await CardCmd.TimingOnCards(cardModel, Timing.AfterRoadChanged, this);
    }
    /// <summary>
    /// 获取该道路上的所有战斗单位
    /// </summary>
    /// <returns>获取到的战斗单位列表</returns>
    public List<Fighter> GetFighters()
    {
        return fighters;
    }
}