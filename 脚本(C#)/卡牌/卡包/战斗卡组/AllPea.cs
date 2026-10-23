using Card.Flower;
using Card.Mushroom;
using Card.Pea;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pack;

public class AllPea : PresetPack
{
    public override string PackName => "唯一爱豆";
    protected override IReadOnlyList<CardModel> DeckCards => 
        new List<CardModel>().RepeatLoad<ButtonMushroom>(4).RepeatLoad<GrowMushroom>(4).RepeatLoad<SunFlower>(2);
}
