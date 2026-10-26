using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

public class GatlingPea : FighterCardModel
{
    public override string AmmoPath => "res://素材(C#)/卡牌/豌豆射手/PeaAmmo.png";
    public override bool IsEvolution => true;
    public override bool CanEvolution(FighterCardModel cardModel)
    {
        return cardModel.HasLabel("豌豆");
    }
    public override async Task Evolve(FighterCardModel fighter)
    {
        await Battle(true);
    }
}
