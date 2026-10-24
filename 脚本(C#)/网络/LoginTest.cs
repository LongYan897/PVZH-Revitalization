using Godot;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// 登录测试场景：输入账号密码 → 调服务端 POST /api/Game/login → 在界面上显示结果。<br/>
/// 运行前先启动服务端（F:\godot\PVZH-Server，端口 5065）：dotnet run --project F:\godot\PVZH-Server
/// </summary>
public partial class LoginTest : Node2D
{
	private LineEdit _userInput;
	private LineEdit _passInput;
	private Button _loginButton;
	private Label _resultLabel;

	public override void _Ready()
	{
		_userInput = GetNode<LineEdit>("UI/账号");
		_passInput = GetNode<LineEdit>("UI/密码");
		_loginButton = GetNode<Button>("UI/登录按钮");
		_resultLabel = GetNode<Label>("UI/结果");

		_userInput.Text = "admin";
		_passInput.Text = "123456";
		_loginButton.Pressed += OnLoginPressed;
	}

	private async void OnLoginPressed()
	{
		_loginButton.Disabled = true;
		_resultLabel.Text = "登录中…";

		var payload = JsonSerializer.Serialize(new
		{
			Username = _userInput.Text,
			Password = _passInput.Text
		});

		var (err, code, body) = await GameApi.PostJsonAsync(this, "/api/Game/login", payload);

		_loginButton.Disabled = false;
		if (err != Error.Ok)
		{
			_resultLabel.Text = $"网络层失败：{err}（服务端没启动？）";
			return;
		}

		LoginResponse result = null;
		try
		{
			result = JsonSerializer.Deserialize<LoginResponse>(body);
		}
		catch (JsonException)
		{
			// 返回不是预期的 JSON，下面统一显示原始内容
		}

		if (result == null)
		{
			_resultLabel.Text = $"HTTP {code}\n返回无法解析：{body}";
			return;
		}

		_resultLabel.Text = $"HTTP {code}\n{result.Message}";
		if (result.Success)
		{
			_resultLabel.Text += $"\n昵称：{result.DisplayName}（id {result.UserId}）";
			_resultLabel.Text += $"\n金币：{result.Gold}";
		}
	}

	/// <summary>对应服务端返回：{"success":true,"message":"登录成功","userId":1,"displayName":"LongYan","gold":1000}</summary>
	public class LoginResponse
	{
		[JsonPropertyName("success")] public bool Success { get; set; }
		[JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
		[JsonPropertyName("userId")] public int UserId { get; set; }
		[JsonPropertyName("displayName")] public string DisplayName { get; set; } = string.Empty;
		[JsonPropertyName("gold")] public int Gold { get; set; }
	}
}
