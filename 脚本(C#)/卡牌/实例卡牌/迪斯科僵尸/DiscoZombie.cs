using Battle.Entity;
using Card.Cmd;
using Card.Dance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Target;

namespace Card;

public class DiscoZombie : FighterCardModel
{
    private class DiscoBuff : Buff
    {
        public override Timing[] Timings => [Timing.AfterPlay];
        public override bool IsTransferable => false;
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (card is FighterCardModel fighter)
            {
                if (CardCmd.CanGenerate<BackupDancer>())
                {
                    var target = await CardCmd.PlayerChoiceRoad(null, fighter, true, r => CardCmd.CanGenerate<BackupDancer>(r));
                    await CardCmd.FighterGenerate(Load<BackupDancer>() as BackupDancer, target, Location.Zombie);
                }
            }
        }
    }
    protected override List<Buff> InitBuffs =>
        [new DiscoBuff()];
}
