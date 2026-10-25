using Battle;
using Battle.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Wood;

public class TorchWood : FighterCardModel
{
    private class TorchBuff : Buff
    {
        public override Timing[] Timings => [Timing.OnFighterEnter,Timing.OnFighterExit];
        public override bool IsTransferable => false;
        private Func<Task> redo;
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (card is FighterCardModel fighter)
            {
                if ((Road)parameters[0] == fighter.Road)
                {
                    var fight = parameters[1] as FighterCardModel;
                    if (!fight.Camp.Plant()) return;
                    if (!fight.HasLabel("豌豆")) return;
                    if (timing == Timing.OnFighterEnter)
                    {
                        if (fighter.Location == Location.PlantFront && fight.Location == Location.Plant)
                        {
                            redo = await fight.Atk.Gain(card.GetSignInt("Strength"), Variable.VariableReason.Plant | Variable.VariableReason.Fighter);
                        }
                    }
                    else if (timing == Timing.OnFighterExit)
                    {
                        if (redo != null)
                            await redo();
                    }
                }
            }
        }
    }
    protected override List<Buff> InitBuffs =>
        [new TorchBuff()];
}
