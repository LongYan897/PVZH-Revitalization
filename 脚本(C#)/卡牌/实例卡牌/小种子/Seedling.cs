using Card.Cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

public class Seedling : FighterCardModel
{
    private class SeedBuff : Buff
    {
        public override Timing[] Timings => [Timing.AfterPhraseStart];
        public override int Priority => -1000;
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (card is FighterCardModel fighter)
            {
                Func<CardModel,bool> filter = (CardModel c) => c is FighterCardModel fighterCard && fighterCard.Camp.Plant() && fighterCard.Cost.Current <= fighter.GetSignInt("Sun");
                var tg = CardCmd.RandomCard((filter,1)).FirstOrDefault() as FighterCardModel;
                await fighter.PlayFighterAnimationTask("special");
                await fighter.TransformMe(tg);
            }
        }
        public override bool IsTransferable => false;
    }

    protected override List<Buff> InitBuffs => [new SeedBuff()];
}
