using Godot;
using Controller;
using Scene;
using System.Threading.Tasks;
/// <summary>
/// 加载界面：进入游戏前先弹出一个**独立的系统窗口**做登录（B 方案）。<br/>
/// 关键点：先把 <c>GetTree().Root.GuiEmbedSubwindows</c> 设为 false，
/// Window 节点才会是真正的独立窗口。<br/>
/// 登录成功后：作为子菜单层打开 → <see cref="Controller.SceneManager.ReturnToMain()"/> 回主界面；
/// 作为游戏入口场景 → 切换到主场景。
/// </summary>
public partial class LoadScene : SubMenuScene
{
	private LoginWindow _loginWindow;
	private Label _statusLabel;
    public override bool HideReturnButton => true;
    public override Task _Load()
	{
		_statusLabel = GetNodeOrNull<Label>("Panel/CenterContainer/Panel/Label");
		OpenLoginWindow();
		return Task.CompletedTask;
	}

	private void OpenLoginWindow()
	{
		// 关键：关掉"子窗口嵌入"，否则 Window 会被画进游戏窗口里（就是 A 方案的效果）
		GetTree().Root.GuiEmbedSubwindows = false;

		var scene = GD.Load<PackedScene>("res://场景(C#)/用户信息/登录窗口.tscn");
		if (scene == null)
		{
			GD.PushError("[LoadScene] 找不到 res://场景(C#)/用户信息/登录窗口.tscn");
			return;
		}

		_loginWindow = scene.Instantiate<LoginWindow>();
		_loginWindow.LoginSucceeded += OnLoginSucceeded;
		AddChild(_loginWindow);
		_loginWindow.Open();

		if (_statusLabel != null) _statusLabel.Text = "等待登录…";
	}

	private void OnLoginSucceeded(LoginWindow.LoginResult result)
	{
		// 登录完恢复嵌入模式，避免影响游戏里其它 UI
		GetTree().Root.GuiEmbedSubwindows = true;

		GD.Print($"[LoadScene] 登录成功：userId={result.UserId} displayName={result.DisplayName} gold={result.Gold}");

		// 如果这个加载界面是“退出登录”之类作为子菜单层打开的：登录成功后自动回主界面；
		// 如果它是游戏入口场景（不在菜单栈里）：切到主界面场景。
		if (SceneManager.IsCurrent(this))
		{
			SceneManager.ReturnToMain();
			return;
		}

		// 作为入口场景：登录成功后进入主界面
		GetTree().ChangeSceneToFile("res://场景(C#)/主节点.tscn");
	}
}
