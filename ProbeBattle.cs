using Godot;
using System.Threading.Tasks;

public partial class ProbeBattle : Node
{
	public override async void _Ready()
	{
		var main = GD.Load<PackedScene>("res://场景(C#)/主节点.tscn").Instantiate();
		AddChild(main);
		await Wait(1.5);

		var battle = main.GetNodeOrNull<Control>("Scene/ButtomScene/对战");
		if (battle == null) { GD.Print("[B] 没找到 对战 节点"); GetTree().Quit(); return; }

		GD.Print("[B] 对战 Control: pos=", battle.Position, " size=", battle.Size,
			" scale=", battle.Scale, " globalRect=", battle.GetGlobalRect());

		foreach (var childName in new[] { "Button", "Frame" })
		{
			var c = battle.GetNodeOrNull<Control>(childName);
			if (c != null) GD.Print("[B] ", childName, " globalRect=", c.GetGlobalRect(), " globalPos=", c.GlobalPosition);
		}

		var card = battle.GetNodeOrNull<Sprite2D>("Frame/卡片");
		if (card != null)
			GD.Print("[B] 卡片 Sprite: globalPos=", card.GlobalPosition, " scale=", card.GlobalScale,
				" textureSize=", card.Texture?.GetSize(), " 视觉尺寸=", (card.Texture?.GetSize() ?? Vector2.Zero) * card.GlobalScale);

		var label = battle.GetNodeOrNull<Label>("Frame/草坪战争");
		if (label != null) GD.Print("[B] 草坪战争 label globalRect=", label.GetGlobalRect());

		// 屏幕尺寸
		GD.Print("[B] 视口尺寸=", GetViewport().GetVisibleRect().Size);
		GetTree().Quit();
	}

	private async Task Wait(double sec)
	{
		await ToSignal(GetTree().CreateTimer(sec), SceneTreeTimer.SignalName.Timeout);
	}
}
