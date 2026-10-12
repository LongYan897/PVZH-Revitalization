using Godot;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Frenemypillar.Script.Save;

/// <summary>
/// 演示读取、修改并写回 playerAsset.json。
/// 在 Godot 项目运行时手动调用 ModifyPlayerAssetJson。
/// </summary>
public partial class FileJson
{
	private const string _defaultSavePath = "res://数据(C#)/playerAsset.json";
    public string SavePath { get; private set; } = _defaultSavePath;
	private JsonObject _root { get; set; }
    /// <summary>
    /// 打开文件并读取 JSON 内容到内存中，若文件不存在则创建一个空的 JsonObject。
    /// </summary>
    /// <param name="path">godot中的文件目录，带后缀</param>
    public void Open(string path)
	{
		SavePath = string.IsNullOrWhiteSpace(path) ? _defaultSavePath : path;
		string filePath = ProjectSettings.GlobalizePath(SavePath);
		string json = File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;

		JsonObject root = string.IsNullOrWhiteSpace(json)
			? new JsonObject()
			: JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
		_root = root;
	}
    /// <summary>
    /// 检查对象是否在Json中存在，不存在创造一个
    /// </summary>
    /// <param name="variable">变量名字</param>
    /// <returns></returns>
    public bool GetOrCreateObject(string variable)
	{
		if (_root == null) return false;
		if (_root[variable] is not JsonObject playerAsset)
		{
            playerAsset = new JsonObject();
			_root[variable] = playerAsset;
        }
		return true;
	}
	/// <summary>
	/// 获取变量的数值
	/// </summary>
	/// <typeparam name="T">变量类型</typeparam>
	/// <param name="variable">变量名字</param>
	/// <param name="defaultVal">默认数值</param>
	/// <returns></returns>
	public T GetVariable<T>(string variable,T defaultVal = default)
	{
		if (_root == null) return defaultVal;
		if (!_root.TryGetPropertyValue(variable, out var valueNode))return defaultVal;
		if (valueNode is JsonValue value && value.TryGetValue<T>(out var result))
			return result;
		return defaultVal;
    }
    /// <summary>
    /// 设置变量的数值
    /// </summary>
    /// <typeparam name="T">变量类型</typeparam>
    /// <param name="variable">变量名字</param>
    /// <param name="value">数值</param>
    /// <returns></returns>
    public bool SetVariable<T>(string variable, T value)
	{
		if (_root == null) return false;
		_root[variable] = JsonValue.Create(value);
		return true;
    }
	/// <summary>
	/// 关闭并保存文件，清空引用，下次使用需重新打开。
	/// </summary>
	/// <returns></returns>
	public bool Close()
	{
        Save();
		_root = null;
		return true;
    }
    /// <summary>
    /// 保存文件，不清空引用，适合高频使用。
    /// </summary>
    /// <returns></returns>
    public bool Save()
	{
        if (_root == null) return false;
        string updateJson = _root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        string filePath = ProjectSettings.GlobalizePath(SavePath);
        File.WriteAllText(filePath, updateJson);
        GD.Print($"已更新存档：{SavePath}");
        return true;
    }
}
