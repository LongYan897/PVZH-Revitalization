using Godot;
using System;

public partial class CardBar : Panel
{
	public Label IntroduceLabel { get; set; }
    public override void _Ready()
    {
        IntroduceLabel = GetNode<Label>("介绍文本");
    }
}
