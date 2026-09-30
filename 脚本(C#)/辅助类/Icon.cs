using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pack;

/// <summary>
/// 标记带文字图片文本的属性
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class IconTextAttribute : Attribute
{
    /// <summary>
    /// 字体颜色
    /// </summary>
	public Color Color { get; }
	/// <summary>
    /// 字体轮廓颜色
    /// </summary>
    public Color OutlineColor { get; }
    /// <summary>
    /// 字体轮廓大小
    /// </summary>
	public int OutlineSize {  get; }
    public IconTextAttribute(float r, float g, float b, float a = 1f)
    {
        Color = new Color(r, g, b, a);
        OutlineColor = new();
        OutlineSize = 0;
    }

    public IconTextAttribute(float r, float g, float b,
        float or, float og, float ob, int outlineSize, float oa = 1f)
    {
        Color = new Color(r, g, b);
        OutlineColor = new Color(or, og, ob, oa);
        OutlineSize = outlineSize;
    }
    public IconTextAttribute(string colorCode)
    {
        Color = new Color(colorCode);
        OutlineColor = new Color();
        OutlineSize = 0;
    }
    public IconTextAttribute(string colorCode,string outlineColorCode,int outlineSize)
    {
        Color = new Color(colorCode);
        OutlineColor = new Color(outlineColorCode);
        OutlineSize = outlineSize;
    }
}

public static class Icon
{
    private static readonly Dictionary<string, (string Path, IconTextAttribute Attr)> _table = new();

    public static void Init()
    {
        _table.Clear();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            foreach (var field in type.GetFields(flags))
            {
                var attr = field.GetCustomAttribute<IconTextAttribute>();
                if (attr == null) continue;
                if (field.GetValue(null) is string path && !string.IsNullOrWhiteSpace(path))
                    _table[field.Name] = (path, attr);
            }
            foreach (var prop in type.GetProperties(flags))
            {
                var attr = prop.GetCustomAttribute<IconTextAttribute>();
                if (attr == null) continue;
                if (prop.GetValue(null) is string path && !string.IsNullOrWhiteSpace(path))
                    _table[prop.Name] = (path, attr);
            }
        }
    }

    public static bool TryGet(string name, out string path, out IconTextAttribute attr)
    {
        if (_table.TryGetValue(name, out var v))
        {
            path = v.Path;
            attr = v.Attr;
            return true;
        }
        path = null;
        attr = null;
        return false;
    }
}