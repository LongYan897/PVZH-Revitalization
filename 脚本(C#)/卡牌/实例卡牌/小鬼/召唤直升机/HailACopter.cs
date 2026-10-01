using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Target;

namespace Card.Imp;

public class HailACopter : CardModel
{
    public override async Task Play(ITarget target)
    {
        await PlayInstantAnimation("intro", target);
    }
}
