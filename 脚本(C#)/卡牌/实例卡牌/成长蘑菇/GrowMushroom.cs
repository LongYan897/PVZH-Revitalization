using Card.Cmd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Mushroom;

public class GrowMushroom : FighterCardModel
{
    public override bool FriendTarget => true;
    private class GrowBuff : Buff
    {
        public override Timing[] Timings => [Timing.AfterPlay];
        public override bool IsTransferable => false;
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (card is FighterCardModel fighter)
            {
                if (CardCmd.AnyPlant(p=>p != fighter))
                {
                    var target = await CardCmd.PlayerChoicePlant(null, card, true, p => p != fighter);
                    await fighter.PlayFighterAnimationTask("special");
                    if (target != null)
                        await Task.WhenAll(target.GainHp(card.GetSignInt("Health"),Variable.VariableReason.Plant),target.Atk.Gain(card.GetSignInt("Strength"),Variable.VariableReason.Plant));
                }
            }
        }
    }
    protected override List<Buff> InitBuffs => [
        new GrowBuff()
        ];
}
