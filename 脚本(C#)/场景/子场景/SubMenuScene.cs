using Controller;
using Godot;
using System;
using System.Threading.Tasks;

namespace Scene;

/// <summary>
/// 子场景（多级菜单里的“一层”）。<br/>
/// 根节点必须是它的子类，SceneManager 才能把这一层压进菜单栈里。
/// </summary>
public abstract partial class SubMenuScene : Node2D
{
    public sealed override async void _EnterTree()
    {
        _WhenEnterTree();

        // 用 F6 直接运行这一层时，先把 autoload 主界面的相机/图层处理掉，否则位置全错
        if (IsStandaloneRun) ApplyStandalonePreview();

        if (IsOpening) return;
        IsOpening = true;

        for (int i = 0; i < 180 && Main.SceneContainer == null && IsInsideTree(); i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        BackButton.JoinScene(HideReturnButton);
        try
        {
            await _Load();
        }
        catch (Exception e)
        {
            GD.PushError($"[SubMenuScene] {Name} 的 _Load() 出错：{e}");
        }
        finally
        {
            SceneManager.InvokeLoadEnd();
        }
    }

    public virtual void _WhenEnterTree()
    { }

    public abstract Task _Load();

    /// <summary>
    /// 打开这一层时由 <see cref="SubMenu.Payload"/> 传进来的数据。<br/>
    /// SceneManager 会在场景加入树（也就是 _Load 执行）之前把它塞进来，所以 _Load 里能直接读到。
    /// </summary>
    public object Payload { get; set; }

    /// <summary>
    /// 点击返回按钮后的函数，用于处理一些功能。
    /// </summary>
    public virtual Task Outro() => Task.CompletedTask;

    /// <summary>是否隐藏主界面那个全局返回按钮（本层自带返回按钮时返回 true）</summary>
    public virtual bool HideReturnButton { get; } = false;

    public bool IsOpening;

    // ==================================================================
    // F6 独立运行支持
    // ==================================================================

    /// <summary>
    /// 是否是用 F6 直接把本场景当主场景跑。<br/>
    /// 正常游戏里这一层会被 SceneManager 塞进 %UponScene(CanvasLayer)，
    /// 父节点是 CanvasLayer；F6 时父节点是根视口。
    /// </summary>
    private bool IsStandaloneRun => GetParent() == GetTree().Root;

    /// <summary>
    /// autoload 里保留、不要隐藏的图层（子场景自己会用到的）。
    /// Cards = 卡片飞行动画容器，Des = 英雄/卡牌详情层。
    /// </summary>
    private static readonly string[] StandaloneKeepLayers = { "Cards", "Des" };

    /// <summary>
    /// F6 直接运行子场景时：
    /// 1) 主界面 autoload 的 Camera2D 是按旧分辨率中心设的 offset，会把场景整体推偏 → 关掉；
    /// 2) autoload 的主界面图层会盖在场景上 → 隐藏掉，只留场景本体。
    /// </summary>
    private void ApplyStandalonePreview()
    {
        Node main = GetTree().Root.GetNodeOrNull("MainController");
        if (main == null) return;

        if (main.GetNodeOrNull("Camera2D") is Camera2D cam) cam.Enabled = false;

        foreach (Node child in main.GetChildren())
            HideStandaloneLayer(child);
    }

    private static void HideStandaloneLayer(Node node)
    {
        if (node == null) return;

        if (node is CanvasLayer layer)
        {
            if (Array.IndexOf(StandaloneKeepLayers, layer.Name.ToString()) >= 0) return;
            layer.Visible = false;
        }
        else if (node is CanvasItem item)
        {
            item.Visible = false;
        }

        foreach (Node child in node.GetChildren())
            HideStandaloneLayer(child);
    }
}
