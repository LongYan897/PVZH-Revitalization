using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Animal;

public class Cattail : FighterCardModel
{
    public override string AmmoPath => "res://场景/植物卡/猫尾草植物/cattail_tail_tip.png";
    public override bool ArrowAmmo => true;
}
