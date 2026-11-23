using Godot;

namespace Pack;

[GlobalClass]
public partial class InputNineRect : NinePatchRect
{
    [Signal]
    public delegate void InputEventHandler(InputEvent @event);

    private bool _isPressing = false;

    public override void _Input(InputEvent @event)
    {
        if (!IsVisibleInTree()) return;
        if (MouseFilter == MouseFilterEnum.Ignore) return;

        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            bool isInside = new Rect2(Vector2.Zero, Size).HasPoint(GetLocalMousePosition());

            if (mb.Pressed)
            {
                if (!isInside) return;
                if (GetViewport().IsInputHandled()) return;

                var hovered = GetViewport().GuiGetHoveredControl();
                if (hovered is InputNineRect && hovered != this) return;

                _isPressing = true;
                EmitSignal(SignalName.Input, @event);
                GetViewport().SetInputAsHandled();
            }
            else
            {
                if (_isPressing)
                {
                    _isPressing = false;
                    EmitSignal(SignalName.Input, @event);
                    GetViewport().SetInputAsHandled();
                }
            }
        }
    }
}