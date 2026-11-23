using Godot;
using System.Threading.Tasks;

namespace Extension
{
    public static class TimeExtension
    {
        public static async Task Wait(this Node node, float seconds)
        {
            await node.ToSignal(node.GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        }
    }
}
