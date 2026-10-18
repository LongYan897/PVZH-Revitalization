using Battle;
using System.Threading.Tasks;

namespace Card.Imp;

public class CopterCommando : FighterCardModel
{
    public override async Task AnimationWhenPlayed(Road road)
    {
        await PlayFighterAnimationTask("swing", true, 2);
        await PlayFighterAnimationTask("lights", true, 1);
    }
}
