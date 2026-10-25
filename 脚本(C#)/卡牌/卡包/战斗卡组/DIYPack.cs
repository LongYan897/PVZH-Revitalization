using Hero.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pack;

public class DIYPack : PresetPack
{
    public void SetName(string name) => PackName = name;
    public sealed override bool Gained => true;
    public void SetHero<Hero>() where Hero : HeroModel,new()
    {
        this.Hero = HeroModel.Identity<Hero>();
    }
    protected sealed override IReadOnlyList<CardModel> DeckCards => [];
}
