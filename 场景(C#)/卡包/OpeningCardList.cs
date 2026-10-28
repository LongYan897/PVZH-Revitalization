using Godot;
using System;

public partial class OpeningCardList : Node2D
{
	public FlowContainer NodeList { get; set; }
    public override void _Ready()
    {
        NodeList = GetNode<FlowContainer>("底版/SmoothScrollContainer/HFlowContainer");
    }
}
