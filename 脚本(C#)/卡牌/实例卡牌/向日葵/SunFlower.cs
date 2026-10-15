using Phrases;
using System.Collections.Generic;

namespace Card.Flower;

public class SunFlower : FighterCardModel
{
    protected override List<Buff> InitBuffs =>
        [
        new SunBuff(1,Phrase.Plant,async () => { await PlayFighterAnimationTask("special2"); }) {OnlyPlaced = true}
        ];
}
