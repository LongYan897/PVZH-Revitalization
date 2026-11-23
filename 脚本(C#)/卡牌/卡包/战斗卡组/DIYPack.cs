using Hero.Model;
using System.Collections.Generic;

namespace Card.Pack;

public class DIYPack : PresetPack
{
    public void SetName(string name) => PackName = name;
    public sealed override bool Gained => true;
    public void SetHero<Hero>() where Hero : HeroModel, new()
    {
        this.Hero = HeroModel.Identity<Hero>();
    }
    protected sealed override IReadOnlyList<CardModel> DeckCards => [];
}
