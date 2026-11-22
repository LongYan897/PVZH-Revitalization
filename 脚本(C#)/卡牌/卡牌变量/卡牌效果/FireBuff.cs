using Battle;
using Godot;
using Logger;
using Pack;
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
            if (timing == Timing.BeforeLaneStart && parameters[0] is Road road && road == fighter.Road)
            {
                if (!fighter.IsDie)
                {
                    await fighter.ApplyDamage(Stacks, Variable.VariableReason.Damaged | Variable.VariableReason.Diff);
                    await fighter.RemoveBuff(this, Variable.VariableReason.Diff, true);
                }
            }

            if (timing == Timing.WhenApply)
            {
                if (!fighter.IsDie)
                {
                    instance = FireRate.Create(Stacks);
                    fighter.AddAnimChild(FighterCardModel.AnimSlot.Hp, instance);
                    await instance.Main().SetAnimationTask(0, "intro");
                    instance.Main().AddAnimation(0, "idle", true);
                }
            }

            if (timing == Timing.WhenRemove)
            {
                if (instance != null && GodotObject.IsInstanceValid(instance))
                {
                    var scale = instance.Scale;
                    Tween tween = instance.CreateTween();
                    tween.TweenProperty(instance,"scale", scale * 1.5f , 0.2f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
                    tween.Parallel().TweenProperty(instance,"modulate", new Color(1,1,1,0) , 0.2f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
                    await tween.ToTask();
                    instance.QueueFree();
                }
            }
        }
    }

    [URL]
    public static string 灼烧 => "在下次自己线上战斗开始之前：对这只僵尸造成=Health={-{#/&:x}}。";
    [IconText("ffffff")]
    public const string Fire = "res://素材(C#)/文本图片/Fire.png";
}
