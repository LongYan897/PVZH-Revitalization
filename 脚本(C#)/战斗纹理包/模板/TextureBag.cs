using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battle.Texture;

/// <summary>
/// 纹理包
/// </summary>
public abstract class TextureBag
{
    protected abstract Dictionary<string,Texture2D> Textures { get; }
    /// <summary>
    /// 获取道路纹理
    /// </summary>
    /// <param name="type">道路类型</param>
    /// <param name="index">是否有变种</param>
    /// <returns>道路纹理</returns>
    public Texture2D ForRoad(RoadType type,bool index = false)
    {
        return type switch
        {
            RoadType.Water => Textures["Water"],
            RoadType.Height => Textures["Height"],
            RoadType.Ground => index switch
            {
                true => Textures["Ground1"],
                false => Textures["Ground2"]
                },
            _ => null
        };
    }
    /// <summary>
    /// 背景
    /// </summary>
    public Texture2D Base => Textures["Base"];
    /// <summary>
    /// 上侧背景
    /// </summary>
    public Texture2D Top => Textures["Top"];
    /// <summary>
    /// 左侧背景
    /// </summary>
    public Texture2D Left => Textures["Left"];
    /// <summary>
    /// 右侧背景
    /// </summary>
    public Texture2D Right => Textures["Right"];
    /// <summary>
    /// 上侧栏杆
    /// </summary>
    public Texture2D Cover => Textures["Cover"];
    /// <summary>
    /// 英雄坑
    /// </summary>
    public Texture2D MainHole => Textures["MainHole"];
    private static TextureBag Load<T>() where T : TextureBag, new() => new T();
    private static TextureBag Bag;
    /// <summary>
    /// 设置纹理包
    /// </summary>
    /// <typeparam name="T">纹理包</typeparam>
    public static void SetCurrent<T>() where T : TextureBag,new()
    {
        Bag = Load<T>();
    }
    /// <summary>
    /// 加载当前的纹理包
    /// </summary>
    /// <returns>当前的纹理包</returns>
    public static TextureBag LoadCurrent()
    {
        return Bag;
    }
}