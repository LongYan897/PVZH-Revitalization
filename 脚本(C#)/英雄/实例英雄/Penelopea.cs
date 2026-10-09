using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hero.Model;
namespace Hero;
public partial class Penelopea : HeroModel
{
    public Penelopea() : base(
        "res://素材/英雄素材/绿影侠/绿影侠.tres",
        new List<Card.CardModel>() { }
    ) { }
}
