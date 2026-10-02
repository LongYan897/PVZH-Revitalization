using Battle;
using Battle.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

public class CopterCommando : FighterCardModel
{
	public override async Task AnimationWhenPlayed(Road road)
    {
        await PlayFighterAnimationTask("swing", true, 2);
        await PlayFighterAnimationTask("lights", true, 1);
    }
}
