using Godot;

/// <summary>
/// 联网测试：请求本地后端 /api/Game/test。<br/>
/// 注意：Godot 的类名在 C# 里会被规范成 PascalCase —— 文档里的 HTTPRequest 对应 C# 的
/// HttpRequest（HTTPClient → HttpClient、OS → Os、JSON → Json 同理），写 HTTPRequest 会 CS0246。
/// </summary>
public partial class NetworkTest : Node
{
	private HttpRequest _http;

	public override void _Ready()
	{
		_http = new HttpRequest();
		AddChild(_http);
		_http.RequestCompleted += OnRequestCompleted;

		string url = "http://localhost:5065/api/Game/test";
		Error err = _http.Request(url);
		GD.Print($"[Net] Request() 返回 = {err}（0 = OK）  url={url}");
	}

	private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		string responseText = System.Text.Encoding.UTF8.GetString(body);
		// result 是 Godot 的 Error 枚举：0 = 成功；非 0 就是连接/解析失败
		GD.Print($"[Net] result={result} 状态码={responseCode} 返回内容={responseText}");
	}
}
