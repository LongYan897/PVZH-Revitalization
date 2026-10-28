using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace Extension
{
    public static class TimeExtension
    {
        public static async Task Wait(this Node node,float seconds)
        {
            await node.ToSignal(node.GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        }
    }
}
