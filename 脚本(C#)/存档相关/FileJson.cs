using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FileExtension;

/// <summary>
/// 读取并修改Json文件
/// </summary>
public partial class FileJson
{
	private const string _defaultSavePath = "res://数据(C#)/playerAsset.json";
	public string SavePath { get; private set; } = _defaultSavePath;
	private JsonObject _root { get; set; }
	private JsonObject _current { get; set; }
	private Stack<JsonObject> _parents = new();
	private string _currentPath { get; set; }
	/// <summary>
	/// 打开文件并读取 JSON 内容到内存中，若文件不存在则创建一个空的 JsonObject。
	/// </summary>
	/// <param name="path">godot中的文件目录，带后缀</param>
	public FileJson Open(string path)
	{
		SavePath = string.IsNullOrWhiteSpace(path) ? _defaultSavePath : path;
		string filePath = ProjectSettings.GlobalizePath(SavePath);
		string json = File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;

		JsonObject root = string.IsNullOrWhiteSpace(json)
			? new JsonObject()
			: JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
		_root = root;

		_current = root;
		_parents.Clear();
		_currentPath = "";
		return this;
	}
	/// <summary>
	/// 检查对象是否在Json中存在，不存在创造一个
	/// </summary>
	/// <param name="variable">变量名字</param>
	/// <returns></returns>
	public bool GetOrCreateObject(string variable)
	{
		if (_current == null) return false;
		if (_current[variable] is not JsonObject playerAsset)
		{
			playerAsset = new JsonObject();
			_current[variable] = playerAsset;
		}
		return true;
	}
	/// <summary>
	/// 从本层级中打开下一个对象
	/// </summary>
	/// <param name="key">键的名称</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public FileJson OpenObject(string key)
	{
		if (_current == null) 
			throw new InvalidOperationException("未打开文件");
		if (string.IsNullOrWhiteSpace(key))
			throw new ArgumentException("参数不能为空",nameof(key));
		JsonObject child = _current[key] as JsonObject ?? new JsonObject();
		_current[key] = child;
		_parents.Push(_current);
		_current = child;
		_currentPath = string.IsNullOrEmpty(_currentPath) ? key : $"{_currentPath}/{key}";
		return this;
	}
	/// <summary>
	/// 跳转到文件根目录
	/// </summary>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public FileJson BackRootObject()
	{
		if (_root == null) throw new InvalidOperationException("文件未打开");
		_current = _root;
		_parents.Clear();
		_currentPath = string.Empty;
		return this;
	}
	/// <summary>
	/// 返回上一级文件目录
	/// </summary>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public FileJson BackParentObject()
	{
		if (_root == null) throw new InvalidOperationException("文件未打开");
		if (_currentPath == string.Empty) throw new InvalidOperationException("已在文件根目录");
		_current = _parents.Pop();
		int slashIndex = _currentPath.LastIndexOf("/");
		_currentPath = slashIndex >= 0
			? _currentPath[..slashIndex]
			: string.Empty;
		return this;
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
		if (_current == null) return defaultVal;
		if (!_current.TryGetPropertyValue(variable, out var valueNode))return defaultVal;
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
		if (_current == null) return false;
		_current[variable] = JsonSerializer.SerializeToNode(value);
		return true;
	}
	/// <summary>
	/// 关闭并保存文件，清空引用，下次使用需重新打开。
	/// </summary>
	/// <returns></returns>
	public bool Close()
	{
		if (Save())
		{
			_root = null;
			_current = null;
			_currentPath = string.Empty;
			_parents.Clear();
			return true;
		}
		return false;
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
	/// <summary>
	/// 获取该层级所有该类型键值
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public Dictionary<string,T> GetEntries<T>()
	{
		var results = new Dictionary<string,T>();
		if (_current == null) return results;

		foreach (var kvp in _current)
		{
			if (kvp.Value is JsonValue jsonValue && jsonValue.TryGetValue<T>(out var v))
			{
				results.Add(kvp.Key, v);
			}
		}

		return results;
	}
	/// <summary>
	/// 获取当前层级指定键对应的数组。
	/// </summary>
	/// <typeparam name="T">数组元素类型</typeparam>
	/// <param name="variable">数组键名</param>
	/// <returns>反序列化后的数组；键不存在或不是数组时返回空列表</returns>
	public List<T> GetArray<T>(string variable)
	{
		if (_current == null || _current[variable] is not JsonArray array)
			return new List<T>();

		return array.Deserialize<List<T>>() ?? new List<T>();
	}
	/// <summary>
	/// 批量设置当前层级的键值；调用 Save() 后写入文件。
	/// </summary>
	/// <typeparam name="T">变量类型</typeparam>
	/// <param name="entries">要批量设置的键值</param>
	/// <returns>当前层级已打开时返回 true</returns>
	public bool SetEntries<T>(IReadOnlyDictionary<string, T> entries)
	{
		if (_current == null) return false;
		if (entries == null) throw new ArgumentNullException(nameof(entries));

		foreach (var entry in entries)
		{
			_current[entry.Key] = JsonSerializer.SerializeToNode(entry.Value);
		}

		return true;
	}
}
