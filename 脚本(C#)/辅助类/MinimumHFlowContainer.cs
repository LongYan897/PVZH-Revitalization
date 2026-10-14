using Godot;

namespace Pack;

[GlobalClass]
public partial class MinimumHFlowContainer : HFlowContainer
{
    public override void _Ready()
    {
        Resized += SizeChanged;
    }
    private void SizeChanged()
    {
        GD.Print(Size.Y);
        CustomMinimumSize = new Vector2(CustomMinimumSize.X, _GetMinimumSize().Y);
    }
}
