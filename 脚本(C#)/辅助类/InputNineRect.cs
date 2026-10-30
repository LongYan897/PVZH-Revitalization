using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pack;

[GlobalClass]
public partial class InputNineRect : NinePatchRect
{
    [Signal]
    public delegate void InputEventHandler(InputEvent @event);
    public override void _Input(InputEvent @event)
    {
        if (!IsVisibleInTree()) return;
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (GetGlobalRect().HasPoint(GetViewport().GetMousePosition()))
            {
                EmitSignal(SignalName.Input, @event);

                GetViewport().SetInputAsHandled();
            }
        }
    }
}
