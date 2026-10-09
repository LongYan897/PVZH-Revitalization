 using Card;
using Godot;
using Hero.Model;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Target;
using Variable;

namespace Hero;

[GlobalClass]
public partial class HeroInstance : Control, ITarget
{
    private HeroInstance() { }
    public TargetType TargetType => TargetType;
    public HeroModel Hero { get; private set; }
    private HeroVariable<int> Health => new HeroVariable<int>(Hero,"Health",20);
    private HeroVariable<int> MaxHealth => new HeroVariable<int>(Hero, "MaxHealth", 20);
    private HeroVariable<int> Shield => new HeroVariable<int>(Hero, "Shield", 0);
    public SpineHandler spineHandler;
    public override void _Ready()
    {
        spineHandler = GetNode<SpineHandler>("动画");
        spineHandler.LoadSkeletonData(Hero.AnimPath);
    }
    public Func<CardModel, Func<ITarget, bool>, bool> CanBeTarget => (t,f) =>
    {
        return false;
    };

    public bool Calling => throw new NotImplementedException();

    public void OnCallTargeted(TargetType type)
    {
        throw new NotImplementedException();
    }

    public void OnDeleteTargeted(TargetType type)
    {
        throw new NotImplementedException();
    }

    public void OnDistargeted(TargetContext ctx)
    {
        throw new NotImplementedException();
    }

    public void OnTargeted(TargetContext ctx)
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// 更改英雄变量值
    /// </summary>
    /// <param name="value">更改值</param>
    /// <param name="reason">更改的原因</param>
    /// <param name="card">更改英雄变量的卡牌</param>
    /// <param name="variableType">更改的变量类型</param>
    /// <param name="impactType">影响的好坏类型</param>
    /// <returns></returns>
    public async Task Modify(int value, VariableReason reason, CardModel card, VariableType variableType, ImpactType impactType)
    {
        if (variableType == VariableType.Health) {
            int modifiedHealth = await Health.Set(value, reason, card, variableType, impactType);
            if (reason == VariableReason.Damaged && Health.Current - modifiedHealth > 0)
            {
                await Modify(Shield.Current + GD.RandRange(1,3),VariableReason.Damaged,card,VariableType.Shield,ImpactType.Benefit);
            }
        }
        if (variableType == VariableType.MaxHealth) await MaxHealth.Set(value, reason, card, variableType, impactType);
        if (variableType == VariableType.Shield) { 
            await Shield.Set(Math.Max(Math.Min(8,value),0), reason, card, variableType, impactType);
        }
    }
    public static async Task<HeroInstance> Create(HeroModel heroModel) => GD.Load<PackedScene>("res://场景(C#)/英雄单位.tscn").Instantiate<HeroInstance>();
}
