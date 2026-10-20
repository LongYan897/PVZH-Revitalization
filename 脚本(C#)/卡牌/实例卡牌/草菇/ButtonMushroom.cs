using Battle;
using Card.Cmd;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Mushroom;

public class ButtonMushroom : FighterCardModel
{
    public override string AmmoPath => "res://场景/植物卡/草菇植物/cloud_1 #91307.png";
    private class ButtonBuff : Buff
    {
        public override Timing[] Timings => [Timing.OnDeath];
        public override bool IsTransferable => false;
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (card is FighterCardModel fighter)
            {
                if (!(parameters[0] as AtkStack).Targets.Contains(fighter.Attacking())) return;
                if (CardCmd.AnyZombie())
                {
                    var target = await CardCmd.PlayerChoiceZombie(null, card, true);
                    if (target != null)
                    {
                        await fighter.Attack(target.Attacking());
                        await target.Atk.Lose(card.GetSignInt("Strength"), Variable.VariableReason.Plant | Variable.VariableReason.Loss);
                    }
                }
            }
        }
    }
    protected override List<Buff> InitBuffs => [
        new ButtonBuff()
        ];
}
