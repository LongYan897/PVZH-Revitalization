using Battle;
using Card.Cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Target;

namespace Card;

public class BrainyBid : CardModel
{
    public override TargetType TargetType => TargetType.Lines;
    public override bool TargetFilter(ITarget target)
    {
        return CardCmd.GetOrderedFighters().Where(p => p.Camp.Plant() && p.Atk.Current <= GetSignInt("Strength")).ToList().Count > 0;
    }
    public override async Task Play(ITarget target)
    {
        var plants = CardCmd.GetOrderedFighters().Where(p => p.Camp.Plant() && p.Atk.Current <= GetSignInt("Strength")).ToList();
        if (plants.Count == 0) return;
        var plant = plants[new Random().Next(plants.Count - 1)];
        await plant.Die(Variable.VariableReason.Zombie | Variable.VariableReason.Trick,immediate:true);
        if (plant.Road == (target.CanBeRoad(out Road road) ? road : null))
        {
            await CardCmd.ChangeCost(null, GetSignInt("Brain"), this);
        }
    }
}
