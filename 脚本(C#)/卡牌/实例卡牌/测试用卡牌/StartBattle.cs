using Battle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Target;

namespace Card;

public class StartBattle : CardModel
{
    public override TargetType TargetType => TargetType.Lines;
    public override async Task Play(ITarget target)
    {
        if (target is NodeRoad road)
        {
            if (road.LastTargetKind == NodeRoad.RoadTargetKind.Line)
            {
                await road.Model.Start();
            }
        }
    }
}
