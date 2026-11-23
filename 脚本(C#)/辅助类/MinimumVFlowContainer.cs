using Godot;

namespace Pack;

[GlobalClass]
public partial class MinimumVFlowContainer : VFlowContainer
{
    public override void _Ready()
    {
        Resized += SizeChanged;
    }
    private void SizeChanged()
    {
        GD.Print(Size.X);
        CustomMinimumSize = new Vector2(_GetMinimumSize().X, CustomMinimumSize.Y);
    }
}