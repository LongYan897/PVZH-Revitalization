
using Battle.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Variable;
using Environment = Battle.Entity.Environment;

namespace Battle;
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
    /// <summary>
    /// 决定该道路是第几行的
    /// </summary>
    public int Index { get; init; }
    /// <summary>
    /// 决定该道路的类型(高地/平地/水路)
    /// </summary>
    public RoadType Type { get; init; } = RoadType.Water;
    /// <summary>
    /// 该道路存储的战斗单位
    /// </summary>
    private readonly List<Fighter> fighters = new();
    /// <summary>
    /// 开始这条线上的战斗
    /// </summary>
    public async Task Start()
    {
        foreach (var fighter in fighters)
        {
            await fighter.BeforeAtk(this);
        }
        var zombie = fighters.FirstOrDefault(f => f.Zombie);
        await zombie?.Battle();
        var plantf = fighters.FirstOrDefault(f => f.Plant1);
        await plantf?.Battle();
        var plant = fighters.FirstOrDefault(f => f.Plant2);
        await plant?.Battle();
        foreach (var fighter in fighters)
        {
            await fighter.AfterAtk(this);
        }
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
    public void AddFighter(Fighter fighter)
    {
        fighters.Add(fighter);
    }
    /// <summary>
    /// 移除战斗单位从该道路
    /// </summary>
    /// <param name="fighter">要移除的战斗单位</param>
    public void RemoveFighter(Fighter fighter)
    {
        fighters.Remove(fighter);
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
