using System.Threading.Tasks;

namespace Card;

public class FireBuff(int amount) : StackableBuff(amount)
{
    public override Timing[] Timings => [Timing.BeforeLaneStart, Timing.WhenApply, Timing.WhenStack, Timing.WhenDestack, Timing.WhenRemove];
    public override int Priority => 1000;
    private FireRate instance;
    public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
    {
        if (card is FighterCardModel fighter)
        {
            if (timing == Timing.BeforeLaneStart)
            {
                if (!fighter.IsDie)
                {
                    await fighter.ApplyDamage(Stacks, Variable.VariableReason.Damaged | Variable.VariableReason.Diff);
                    await fighter.RemoveBuff(this, Variable.VariableReason.Diff,true);
                }
            }

            if (timing == Timing.WhenApply)
            {
                if (!fighter.IsDie)
                {
                    instance = FireRate.Create();
                    await instance.Main().SetAnimationTask(0,"intro");
                    instance.Main().AddAnimation(0, "idle",true);
                }
            }
        }
    }
}
