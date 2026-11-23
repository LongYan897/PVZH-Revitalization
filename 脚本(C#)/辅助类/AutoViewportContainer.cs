using Godot;

namespace Pack;

[GlobalClass]
public partial class AutoViewportContainer : SubViewportContainer
{
    public override void _Ready()
    {
        Stretch = true;

        if (GetParent() is Control parent)
        {
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            parent.Resized += OnParentResized;
        }
        else
        {
            GD.PushWarning($"{Name}: parent is not Control.");
        }
    }

    private void OnParentResized()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }
}