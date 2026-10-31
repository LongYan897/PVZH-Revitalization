using Battle.Entity;
using Card;
using Card.Cmd;
using Godot;
using System;
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
    public bool IsUp { get; set; }
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
    public async Task Start(List<AtkStack> stks = null)
    {
        IsBattling = true;
        var bol = stks == null;
        await NodeRoad.GetNode(this).Invoke(true);
        if (bol)
        {
            await CardCmd.TimingOnCards(null, Timing.BeforeLaneStart, this);
            stks = new List<AtkStack>();
            await CardCmd.TimingOnCards(null, Timing.AfterLaneStart, this);
            var zombie = fighters.FirstOrDefault(f => f.Zombie);
            if (zombie != null)
            {
                var stk = new AtkStack(zombie.Model, [], this, zombie.Model.Atk.Current,true);
                await zombie.Model.Battle(stk);
                stks.Add(stk);
            }
            var plantf = fighters.FirstOrDefault(f => f.Plant1);
            var plant = fighters.FirstOrDefault(f => f.Plant2);
            var bothppf = plantf != null && plant != null;
            var cannonical = 0;
            if (plantf != null)
            {
                var stk = new AtkStack(plantf.Model, [], this, plantf.Model.Atk.Current, true);
                cannonical = plantf.Model.Atk.Current;
                await plantf?.Model?.Battle(stk);
                stks.Add(stk);
            }
            if (plant != null)
            {
                var stk = new AtkStack(plant.Model, [], this, plant.Model.Atk.Current, true) { IgnoreShield = bothppf && plantf != null && cannonical > 0 };
                await plant?.Model?.Battle(stk);
                stks.Add(stk);
            }
        }
        else
        {
            var zombie = fighters.FirstOrDefault(f => f.Zombie);
            if (zombie != null)
            {
                if (stks.Any(atk => atk.Attacker == zombie.Model && atk.ExtraAttacks > 0))
                {
                    var stk = stks.FirstOrDefault(atk => atk.Attacker == zombie.Model && atk.ExtraAttacks > 0);
                    stk.ExtraAttacks -= 1;
                    await zombie.Model.Battle(stk);
                }
            }
            var plantf = fighters.FirstOrDefault(f => f.Plant1);
            var plant = fighters.FirstOrDefault(f => f.Plant2);
            var bothppf = plantf != null && plant != null;
            var cannonical = 0;
            if (plantf != null)
            {
                cannonical = plantf.Model.Atk.Current;
                if (stks.Any(atk => atk.Attacker == plantf.Model && atk.ExtraAttacks > 0))
                {
                    var stk = stks.FirstOrDefault(atk => atk.Attacker == plantf.Model && atk.ExtraAttacks > 0);
                    stk.ExtraAttacks -= 1;

                    await plantf.Model.Battle(stk);
                }
            }
            if (plant != null)
            {
                if (stks.Any(atk => atk.Attacker == plant.Model && atk.ExtraAttacks > 0))
                {
                    var stk = stks.FirstOrDefault(atk => atk.Attacker == plant.Model && atk.ExtraAttacks > 0);
                    stk.ExtraAttacks -= 1;
                    stk.IgnoreShield = bothppf && plantf != null && cannonical > 0;
                    await plant.Model.Battle(stk);

                }
            }
        }

        await ResolveDeaths(stks);

        if (stks != null && stks.Any(s => s.ExtraAttacks > 0))
        {
            await Start(stks);
        }

        if (bol)
        {
            await CardCmd.TimingOnCards(null, Timing.BeforeLaneEnd, this);

            await NodeRoad.GetNode(this).Invoke(false);

            await CardCmd.TimingOnCards(null, Timing.AfterLaneEnd, this);

            IsBattling = false;
        }
    }

    /// <summary>
    /// 结算死亡
    /// </summary>
    private async Task ResolveDeaths(List<AtkStack> stacks)
    {
        var dying = CardCmd.GetOrderedFighters()
            .Where(f => f.Model is { IsDie: true })
            .ToList();

        var tasks = new List<Task>();

        foreach (var fighter in dying)
        {
            var related = stacks
                .Where(stk => stk.Targets.Contains(fighter))
                .ToList();

            await fighter.Model.BeginDie(
                VariableReason.Fighter | VariableReason.Damaged,
                related.FirstOrDefault());
        }

        foreach (var fighter in dying)
        {
            tasks.Add(fighter.Model.EndDie());
        }

        await Task.WhenAll(tasks);
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
    public async Task AddFighter(CardModel cardModel, Fighter fighter)
    {
        await cardModel.FireTiming(Timing.OnFighterEnter, this, fighter.Model);
        await CardCmd.TimingOnCards(cardModel, Timing.OnFighterEnter, this, fighter.Model);
        fighters.Add(fighter);
        await cardModel.FireTiming(Timing.AfterRoadChanged, this);
        await CardCmd.TimingOnCards(cardModel, Timing.AfterRoadChanged, this);
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
    /// <summary>
    /// 检查是否能容纳某个单位
    /// </summary>
    /// <typeparam name="T">要生成单位的类</typeparam>
    /// <returns></returns>
    public bool Containable(FighterCardModel fighter)
    {
        return Fighter.CanGenerate(fighter,this);
    }
}