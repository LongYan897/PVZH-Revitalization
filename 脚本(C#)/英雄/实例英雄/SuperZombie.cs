using Hero.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hero;
public partial class SuperZombie : HeroModel
{
    public SuperZombie() : base(
        "res://素材/英雄素材/超尸/超尸.tres",
        new List<Card.CardModel>() { }
    ) { }
}
