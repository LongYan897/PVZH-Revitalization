using Godot;
using System;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 游戏侧的 HTTP 访问层：所有请求都走这里，统一 baseUrl / 超时 / 错误处理。<br/>
/// 用法：<br/>
/// <code>
/// var (err, code, body) = await GameApi.PostJsonAsync(this, "/api/Game/login", json);
/// if (err != Error.Ok) { ... } // err 是 Godot 的 Error，0=Ok 表示网络层成功
/// </code>
/// 注意：Godot 的类名在 C# 里是 HttpRequest（不是 HTTPRequest）、HttpClient.Method（不是 HTTPClient）。
/// </summary>
public static class GameApi
{
	/// <summary>服务器地址。换正式服务器只改这一处。</summary>
	public const string BaseUrl = "http://127.0.0.1:5065";

	/// <summary>
	/// 发一个请求并等结果。<br/>
	/// 返回的 Result 是网络层结果（Ok 表示收到响应），Code 才是 HTTP 状态码（200/400/500...）。
	/// </summary>
	public static async Task<(Error Result, long Code, string Body)> RequestAsync(
		Node owner,
		HttpClient.Method method,
		string path,
		string jsonBody = null,
		string[] headers = null)
	{
		if (owner == null || !GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree())
			return (Error.Unavailable, 0, string.Empty);

		var http = new HttpRequest();
		owner.AddChild(http);

		var tcs = new TaskCompletionSource<(Error, long, string)>();
		http.RequestCompleted += (result, responseCode, _headers, body) =>
		{
			tcs.TrySetResult(((Error)result, responseCode, Encoding.UTF8.GetString(body)));
			if (GodotObject.IsInstanceValid(http)) http.QueueFree();
		};
		// 场景在请求途中被销毁时，别让 await 永远挂着
		http.TreeExiting += () => tcs.TrySetResult((Error.Unavailable, 0, string.Empty));

		string[] finalHeaders = headers
			?? (jsonBody != null ? new[] { "Content-Type: application/json" } : Array.Empty<string>());

		Error err = http.Request(BaseUrl + path, finalHeaders, method, jsonBody ?? string.Empty);
		if (err != Error.Ok)
		{
			if (GodotObject.IsInstanceValid(http)) http.QueueFree();
			return (err, 0, string.Empty);
		}

		return await tcs.Task;
	}

	/// <summary>GET 请求</summary>
	public static Task<(Error Result, long Code, string Body)> GetAsync(Node owner, string path)
		=> RequestAsync(owner, HttpClient.Method.Get, path);

	/// <summary>POST JSON 请求（自动带 Content-Type: application/json）</summary>
	public static Task<(Error Result, long Code, string Body)> PostJsonAsync(Node owner, string path, string jsonBody)
		=> RequestAsync(owner, HttpClient.Method.Post, path, jsonBody);
}
