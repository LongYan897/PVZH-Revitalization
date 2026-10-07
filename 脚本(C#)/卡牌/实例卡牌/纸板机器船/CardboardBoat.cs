using Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Scientist;

public class CardboardBoat : FighterCardModel
{
    public override bool IsFusion => true;
    protected override bool CanFusion(FighterCardModel cardModel)
    {
        return true;
    }
    public override async Task Fuse(FighterCardModel fighter)
    {
        fighter.CardTag |= CardTag.Amphibious;
        await fighter.GainHp(GetSignInt("Health"),Variable.VariableReason.Zombie | Variable.VariableReason.Diff);
    }
}
