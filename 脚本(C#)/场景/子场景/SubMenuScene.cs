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
}
