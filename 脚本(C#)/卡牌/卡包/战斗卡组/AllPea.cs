using Card.Animal;
using Card.Flower;
using Card.Mushroom;
using Card.Pea;
using Hero;
using Hero.Model;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pack;

public sealed class AllPea : PresetPack
{
    public override string PackName => "唯一爱豆";
    public override HeroModel Hero => HeroModel.Identity<Penelopea>();
    protected override DeckRarity Rarity => DeckRarity.Init;
    protected override IReadOnlyList<CardModel> DeckCards => 
        new List<CardModel>().RepeatLoad<Cattail>(20).RepeatLoad<Peashooter>(20);
}
