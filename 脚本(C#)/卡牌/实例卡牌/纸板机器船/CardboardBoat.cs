using Godot;
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
    protected override bool CanFusion(FighterCardModel cardModel,out Texture2D fuseIcon)
    {
        fuseIcon = GD.Load<Texture2D>("res://场景/僵尸卡组/纸板机器船/CardBoardFusion.png");
        return true;
    }
    public override async Task Fuse(FighterCardModel fighter)
    {
        fighter.CardTag |= CardTag.Amphibious;
        await fighter.GainHp(GetSignInt("Health"),Variable.VariableReason.Zombie | Variable.VariableReason.Diff);
    }
}
