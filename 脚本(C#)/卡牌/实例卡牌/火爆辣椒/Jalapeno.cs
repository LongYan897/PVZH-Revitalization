using Card.Cmd;
using Controller;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Target;

namespace Card;

public class Jalapeno : CardModel
{
    public override TargetType TargetType => TargetType.Lines;
    public override bool TargetFilter(ITarget target)
    {
        return CardCmd.GetOrderedFighters().Any(f => f.Camp.Zombie());
    }
    public override async Task Play(ITarget target)
    {
        var zombs = CardCmd.GetOrderedFighters().Where(f=>f.Camp.Zombie());
        await PlayInstantAnimation("effect", target, scaleModulate: new Vector2(0.36f,0.36f) ,posModulate: new Vector2(0,-260));
        var tasks = new List<Task>();
        foreach (var road in CardCmd.GetRoads())
        {
            tasks.Add(AnimaActor.PlayInstantAnimationTask("res://数据资源/杂项/火焰动画.tres",road.Pos() + new Godot.Vector2(0,-270),"fire", new Godot.Vector2(0.65f,0.65f)));
        }

        foreach (var zomb in zombs)
        {
            tasks.Add( zomb.AddBuff(new FireBuff(3),Variable.VariableReason.Plant | Variable.VariableReason.Trick));
        }
        await Task.WhenAll(tasks);
    }
}
