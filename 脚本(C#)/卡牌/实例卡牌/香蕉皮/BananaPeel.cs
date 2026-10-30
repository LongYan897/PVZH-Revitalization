using Battle.Entity;
using Card.Cmd;
using Phrases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Target;

namespace Card.Fruit;

public class BananaPeel : CardModel
{
    public override TargetType TargetType => TargetType.Fighters;
    public override async Task Play(ITarget target)
    {
        if (target.CanBeFighter(out var fighter))
        {
            var (road, location) = await CardCmd.PlayerChoiceGrid(null,this,true,r=>r.Containable(fighter),Camp.Zombie);
            if (road != null)
            {
                await PlayInstantAnimation("effect", target);
                await fighter.MoveTo(road, location is Location loc ? loc : Location.Zombie);
                var redo = await fighter.Atk.Lose(GetSignInt("Strength"), Variable.VariableReason.Plant);
                var buff = new PeelBuff();
                var unable = CardCmd.EnableBuff(buff);
                buff.redo = async () =>
                {
                    unable();
                    await redo();
                };
            }
        }
    }
    private class PeelBuff : Buff
    {
        public Func<Task> redo;
        public override Timing[] Timings => [Timing.AfterPhraseEnd];
        public override bool IsTransferable => false;
        public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (parameters[0] is Phrase phrase && phrase != Phrase.Fight) return;
            if (redo != null)
            {
                await redo();
                redo = null;
            }
        }
    }
}
