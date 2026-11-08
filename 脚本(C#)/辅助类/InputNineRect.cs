using Godot;

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
                if (GetViewport().IsInputHandled()) return;

                var hovered = GetViewport().GuiGetHoveredControl();

                if (hovered != null && hovered != this)
                {
                    if (hovered is InputNineRect) return;

                    bool hasCommonAncestor = false;
                    Node p1 = this;
                    while (p1 != null)
                    {
                        if (p1 == hovered || p1.IsAncestorOf(hovered))
                        {
                            hasCommonAncestor = true;
                            break;
                        }
                        p1 = p1.GetParent();
                    }

                    if (!hasCommonAncestor && hovered is Control control)
                    {
                        if (control.MouseFilter == MouseFilterEnum.Stop)
                        {
                            return;
                        }
                    }
                }

                EmitSignal(SignalName.Input, @event);
                GetViewport().SetInputAsHandled();
            }
        }
    }
}