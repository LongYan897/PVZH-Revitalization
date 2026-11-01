using Controller;
using Godot;
using System.Threading.Tasks;

namespace Pack;

public static class TweenExtension
{
    public static Task ToTask(this Tween tween) => tween.ToSignal(tween, Tween.SignalName.Finished).ToTask();
}
