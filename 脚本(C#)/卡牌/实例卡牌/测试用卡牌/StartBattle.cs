using Battle;
using System.Threading.Tasks;
using Target;

namespace Card;

public class StartBattle : CardModel
{
    public override TargetType TargetType => TargetType.Lines;
    public override bool TargetFilter(ITarget target)
    {
        return target is NodeRoad road && road.Model.GetFighters().Count > 0;
    }
    public override async Task Play(ITarget target)
    {
        if (target is NodeRoad road)
        {
            if (road.LastTargetKind == RoadTargetKind.Line)
            {
                await PlayInstantAnimation("bloom", target);
                await road.Model.Start();
            }
        }
    }
}
