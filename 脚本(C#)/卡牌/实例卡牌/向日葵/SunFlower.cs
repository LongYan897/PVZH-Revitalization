using Card.Pea;
using Phrases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Flower;

public class SunFlower : FighterCardModel
{
    protected override List<Buff> InitBuffs => 
        [
        new SunBuff(1,Phrase.Plant,async () => { await PlayFighterAnimationTask("special2"); }) {OnlyPlaced = true}
        ];
}
