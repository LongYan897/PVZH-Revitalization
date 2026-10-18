using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server;
/// <summary>
/// Crux客户端类，用于玩家注册/登录，以及存储信息
/// </summary>
public partial class CruxClient : Node
{
    public CruxClient()
    {
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey",API_KEY);
    }
    private const string URL = "https://crux.supercraft.host/v1";
    private const string PROJECT_ID = "d8235f2f-3e12-4673-851a-ebd9856b8744";
    private const string ENV_ID = "f2499779-3a9b-458c-b346-251cd8a6c01f";
    private const string API_KEY = "349e8b455223f5b0cd941351a9260f69f20c935f37d2494b7a889a537960fd56";
    private static readonly System.Net.Http.HttpClient HttpClient = new System.Net.Http.HttpClient();
    private string PlayerStack = null;
    public JsonElement PlayerStackElement;
    /// <summary>
    /// 通过邮箱注册玩家
    /// </summary>
    /// <param name="_email">邮箱地址</param>
    /// <param name="_password">密码</param>
    /// <param name="_display_name">游戏内名称</param>
    /// <returns></returns>
    public async Task Register(string _email,string _password,string _display_name) 
    {
        if (PlayerStack != null) return;
        var content = new {email = _email,password = _password,display_name = _display_name };
        PlayerStack = await Post($"{URL}/auth/register",JsonSerializer.Serialize(content));
        PlayerStackElement = JsonSerializer.Deserialize<JsonElement>(PlayerStack);
    }
    /// <summary>
    /// 通过邮箱登录
    /// </summary>
    /// <param name="_email">邮箱地址</param>
    /// <param name="_password">密码</param>
    /// <returns></returns>
    public async Task Login(string _email,string _password)
    {
        if (PlayerStack != null) return;
        var content = new { email = _email, password = _password };
        PlayerStack = await Post($"{URL}/auth/login",JsonSerializer.Serialize(content));
        PlayerStackElement = JsonSerializer.Deserialize<JsonElement>(PlayerStack);
        GD.Print(PlayerStackElement);
    }
    /// <summary>
    /// 刷新玩家临时令牌，若刷新令牌过期或未登录/注册则无作用
    /// 注册令牌过期需让玩家重新登录/注册
    /// </summary>
    /// <returns></returns>
    public async Task Refresh()
    {
        if (PlayerStack == null || PlayerStackElement.GetProperty("refresh_token_expires_in").GetInt64() >= 2592000) return;
        var content = new { refresh_token = PlayerStackElement.GetProperty("") };
        PlayerStack = await Post($"{URL}/auth/login", JsonSerializer.Serialize(content));
        PlayerStackElement = JsonSerializer.Deserialize<JsonElement>(PlayerStack);
        
    }
    /// <summary>
    /// 通过邮箱发送验证信息
    /// 注册已经包括了发送验证信息
    /// </summary>
    /// <param name="_email">邮箱地址</param>
    /// <returns></returns>
    public async Task SendEmail(string _email)
    {
        var content = new { email = _email };
        await Post($"{URL}/auth/send-verification",JsonSerializer.Serialize(content));
    }
    /// <summary>
    /// 玩家登入情况下，写入玩家信息
    /// </summary>
    /// <param name="keyValuePairs">要写入的键值对</param>
    /// <returns></returns>
    public async Task Write(Dictionary<string,object> keyValuePairs)
    {
        if (PlayerStack == null) return;
        var items = keyValuePairs.Select(kv => new { key = kv.Key, value = kv.Value }).ToArray();
        var content = new { items = items};
        await Post(

            $"{URL}/projects/{PROJECT_ID}/environments/{ENV_ID}/players/{PlayerStackElement.GetProperty("player_id").GetString()}/documents/batch-write",
            JsonSerializer.Serialize(content),
            PlayerStackElement.GetProperty("access_token").GetString()
            );
    }
    /// <summary>
    /// 玩家登入的情况下，读取玩家信息
    /// </summary>
    /// <param name="keys">所有要读取的键</param>
    /// <returns></returns>
    public async Task<string> Read(List<string> keys)
    {
        if (PlayerStack == null) return "";
        var content = new {keys = keys };
        return await Post(
            $"{URL}/projects/{PROJECT_ID}/environments/{ENV_ID}/players/{PlayerStackElement.GetProperty("player_id").GetString()}/documents/batch-read",
            JsonSerializer.Serialize(content),
            PlayerStackElement.GetProperty("access_token").GetString()
            );
    }
    /// <summary>
    /// 直接向Crux 发送 api 请求
    /// </summary>
    /// <param name="requestUrl">Crux 对应接口地址</param>
    /// <param name="requestContent">请求内容</param>
    /// <param name="bearerToken">请求头衔</param>
    /// <returns></returns>
    public async Task<string> Post(string requestUrl,string requestContent,string bearerToken=null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,requestUrl);
        request.Content = new StringContent(requestContent,Encoding.UTF8,"application/json");

        if (!string.IsNullOrEmpty(bearerToken)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",bearerToken);

        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
