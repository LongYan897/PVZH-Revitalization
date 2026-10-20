using Godot;
using System;

public partial class OptionSlot : Control
{
	public string IntroduceText { get; set; }
	public int PriceCount { get; set; }
	private Label IntroduceLabel;
	private Label PriceLabel;

	public override void _Ready()
	{
		IntroduceLabel = GetNode<Label>("购买按钮/按钮文本");
		PriceLabel = GetNode<Label>("购买按钮/CenterContainer/HBoxContainer/钻石数量");
	}
	public void Setup()
	{
		IntroduceLabel.Text = IntroduceText;
		PriceLabel.Text = PriceCount.ToString();
	}
}
