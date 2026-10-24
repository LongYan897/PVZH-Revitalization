using Godot;
using Scene;
using Controller;
using System.Threading.Tasks;

public partial class ProbeShot : Node
{
	public override async void _Ready()
	{
		var main = GD.Load<PackedScene>("res://场景(C#)/主节点.tscn").Instantiate();
		AddChild(main);
		await Wait(2.0);
		Shot("res://_shot_fresh.png");
		GD.Print("[S] 已保存刚启动的主界面截图");

		var upon = Main.SceneContainer.GetNode<CanvasLayer>("UponScene");
		SubMenu userInfoMenu = null;
		foreach (var n in main.FindChildren("*", "", true, false))
			if (n is SubMenu sm && (sm.SubScene ?? "").Contains("个人信息")) { userInfoMenu = sm; break; }
		SceneManager.JumpToSub(userInfoMenu);
		await Wait(1.5);

		var userScene = userInfoMenu.GetScene();
		SubMenu logout = null;
		foreach (var n in userScene.FindChildren("*", "", true, false))
			if (n is SubMenu sm && (sm.SubScene ?? "").Contains("加载界面")) { logout = sm; break; }
		SceneManager.JumpToSub(logout);
		await Wait(2.0);

		LoginWindow win = null;
		foreach (var n in GetTree().Root.FindChildren("*", "", true, false))
			if (n is LoginWindow w) { win = w; break; }
		if (win != null)
		{
			win.GetNode<LineEdit>("UI/VBox/账号").Text = "admin";
			win.GetNode<LineEdit>("UI/VBox/密码").Text = "123456";
			win.GetNode<Button>("UI/VBox/登录按钮").EmitSignal(BaseButton.SignalName.Pressed);
		}
		await Wait(3.0);
		Shot("res://_shot_after.png");
		GD.Print("[S] 已保存登录返回后的主界面截图, upon=", upon.GetChildCount(),
			" GuiEmbed=", GetTree().Root.GuiEmbedSubwindows);
		GetTree().Quit();
	}

	private void Shot(string path)
	{
		GetViewport().GetTexture().GetImage().SavePng(path);
	}

	private async Task Wait(double sec)
	{
		await ToSignal(GetTree().CreateTimer(sec), SceneTreeTimer.SignalName.Timeout);
	}
}
