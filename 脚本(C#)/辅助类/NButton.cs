using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pack;

[GlobalClass]
public partial class NButton : TextureButton
{
    private new Vector2 Size;
    public override void _Ready()
    {
        ButtonDown += OnNButtonDown;
        ButtonUp += OnNButtonUp;
        Size = Scale;
    }
    private void OnNButtonDown()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "scale", Size*0.9f, 0.1f);
    }
    private void OnNButtonUp()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "scale", Size, 0.1f);
    }
}
