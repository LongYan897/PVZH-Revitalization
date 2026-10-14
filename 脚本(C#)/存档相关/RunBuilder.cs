using System;
using System.Collections.Generic;
using System.Linq;

namespace Save;

public class PropsBuilder
{
    private readonly string _name;
    private readonly List<(string key, IRunSaveProp saveProp)> _data = new();

    private PropsBuilder(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Prop name cannot be empty", nameof(name));
        _name = name;
    }

    /// <summary>
    /// 启动链式构建器
    /// </summary>
    /// <param name="name">Props名称</param>
    public static PropsBuilder Create(string name)
    {
        return new PropsBuilder(name);
    }

    public PropsBuilder Add(string key, IRunSaveProp saveProp)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));
        if (saveProp == null)
            throw new ArgumentNullException(nameof(saveProp));

        _data.Add((key, saveProp));
        return this;
    }

    /// <summary>
    /// 构建 DictionaryProps
    /// </summary>
    public DictionaryProps ToDictionary()
    {
        return new DictionaryProps(_name, _data);
    }

    /// <summary>
    /// 构建 DictionaryProp
    /// </summary>
    public DictionaryProp ToDictionaryProp()
    {
        return new DictionaryProp(_data);
    }

    /// <summary>
    /// 构建 ListProps
    /// </summary>
    public ListProps ToList()
    {
        var propList = _data.Select(x => x.saveProp).ToList();
        return new ListProps(_name, propList);
    }

    /// <summary>
    /// 构建 ListProp
    /// </summary>
    public ListProp ToListProp()
    {
        var propList = _data.Select(x => x.saveProp).ToList();
        return new ListProp(propList);
    }
}
