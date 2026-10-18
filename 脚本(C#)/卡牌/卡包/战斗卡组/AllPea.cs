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
    public override IReadOnlyList<CardModel> Cards => 
        new List<CardModel>().RepeatLoad<Peashooter>(40);
}
