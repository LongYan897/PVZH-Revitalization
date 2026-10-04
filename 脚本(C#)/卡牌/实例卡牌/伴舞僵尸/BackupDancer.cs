using Battle.Entity;
using Card.Cmd;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Dance;

public class BackupDancer : FighterCardModel
{
    private class BackupDancerBuff(BackupDancer bind) : Buff
    {
        public override Timing[] Timings => [Timing.OnFighterExit,Timing.AfterPlay];
        private BackupDancer model = bind;
        private FighterCardModel target;
        private Func<Task> redo;
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (timing == Timing.AfterPlay)
            {
                if (card is FighterCardModel fighterCard)
                {
                    if (fighterCard == model)
                    {
                        if (CardCmd.AnyZombie(z => z.HasLabel("跳舞")))
                        {
                            FighterCardModel fighter = await CardCmd.PlayerChoiceZombie(null, card,(z)=>z.HasLabel("跳舞"));
                            target = fighter;
                            redo = await fighter.Atk.Gain(model.GetSignInt("Strength"),Variable.VariableReason.Zombie);
                        }
                    }
                }
            }
            if (timing == Timing.OnFighterExit)
            {
                if (card is FighterCardModel fighterCard)
                {
                    if (parameters[1] == model || parameters[1] == target)
                    {
                        if (redo == null) return;
                        await redo();
                    }
                }
            }
        }
    }
    public override bool FriendTarget => true;
    protected override List<Buff> InitBuffs => [
        new BackupDancerBuff(this)
        ];
}
