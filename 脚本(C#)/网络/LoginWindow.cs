using Godot;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// 独立的登录窗口（Godot Window 节点）。<br/>
/// 由加载界面打开，需要先把 <c>GetTree().Root.GuiEmbedSubwindows</c> 设为 false，
/// 否则 Window 会被"嵌入"成游戏窗口里的子窗口，而不是独立的系统窗口。<br/>
/// 登录成功后发 <see cref="LoginSucceeded"/> 事件并隐藏自己。
/// </summary>
public partial class LoginWindow : Window
{
	/// <summary>登录成功（参数是服务端返回的用户信息）</summary>
	public event Action<LoginResult> LoginSucceeded;

	private LineEdit _userInput;
	private LineEdit _passInput;
	private Button _loginButton;
	private Label _tipLabel;

	public override void _Ready()
	{
		_userInput = GetNode<LineEdit>("UI/VBox/账号");
		_passInput = GetNode<LineEdit>("UI/VBox/密码");
		_loginButton = GetNode<Button>("UI/VBox/登录按钮");
		_tipLabel = GetNode<Label>("UI/VBox/提示");

		// 调试方便：默认填好测试账号，正式版删掉
		_userInput.Text = "admin";
		_passInput.Text = "123456";

		_loginButton.Pressed += OnLoginPressed;
		// 点登录窗口右上角的 × = 直接退出游戏（不登录就没法进）
		CloseRequested += () => GetTree().Quit();
	}

	/// <summary>弹出窗口（由加载界面调用）</summary>
	public void Open()
	{
		Title = "登录";
		PopupCentered(new Vector2I(420, 340));
	}

	private async void OnLoginPressed()
	{
		_loginButton.Disabled = true;
		_tipLabel.Text = "登录中…";

		var payload = JsonSerializer.Serialize(new
		{
			Username = _userInput.Text,
			Password = _passInput.Text
		});

		var (err, code, body) = await GameApi.PostJsonAsync(this, "/api/Game/login", payload);
		_loginButton.Disabled = false;

		if (err != Error.Ok)
		{
			_tipLabel.Text = $"连不上服务器（{err}）\n请确认 PVZH-Server 已启动";
			return;
		}

		LoginResult result = null;
		try
		{
			result = JsonSerializer.Deserialize<LoginResult>(body);
		}
		catch (JsonException)
		{
			_tipLabel.Text = $"HTTP {code}，返回无法解析：{body}";
			return;
		}

		if (result == null || !result.Success)
		{
			_tipLabel.Text = $"HTTP {code}\n{result?.Message ?? "账号或密码错误"}";
			return;
		}

		// 登录成功：先隐藏窗口，再通知加载界面继续
		Hide();
		LoginSucceeded?.Invoke(result);
	}

	/// <summary>对应服务端返回：{"success":true,"message":"登录成功","userId":1,"displayName":"LongYan","gold":1000}</summary>
	public class LoginResult
	{
		[JsonPropertyName("success")] public bool Success { get; set; }
		[JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
		[JsonPropertyName("userId")] public int UserId { get; set; }
		[JsonPropertyName("displayName")] public string DisplayName { get; set; } = string.Empty;
		[JsonPropertyName("gold")] public int Gold { get; set; }
	}
}
