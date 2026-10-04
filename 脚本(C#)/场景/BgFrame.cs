
using Godot;

namespace Scene;

[GlobalClass]
public partial class BgFrame : Sprite2D
{
	[Export] public Color Color { get; set; }
	private ShaderMaterial material;
	public override void _Ready()
	{
		material = Material as ShaderMaterial;
		material.SetShaderParameter("tint_color", Color);
	}

	public override void _Process(double delta)
	{
		var curVal = (Vector2)material.GetShaderParameter("direction");
		curVal += new Vector2(0.05f, -0.05f) * (float)delta;
		material.SetShaderParameter("direction", curVal);
	}
}
