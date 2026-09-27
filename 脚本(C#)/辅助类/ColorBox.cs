
using Godot;

namespace Pack;

/// <summary>
/// 调色盘,用于存储一些颜色
/// </summary>
public readonly struct ColorBox(Color normal,Color a,Color b,Color c)
{
    public readonly Color Default = normal;
    public readonly Color ColorA = a;
    public readonly Color ColorB = b;
    public readonly Color ColorC = c;
    public static ColorBox Make(string normal,string a,string b,string c)
    {
        return new(new(normal),new(a),new(b),new(c));
    }
}