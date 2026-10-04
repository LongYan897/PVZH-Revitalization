using Godot;
using Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controller;
/// <summary>
/// 控制场景切换的类
/// </summary>
public static class SceneManager
{
    /// <summary>
    /// 当前打开的子目录
    /// </summary>
    private static readonly Dictionary<SubMenu, SubMenu> Menus = new();

    private static TaskCompletionSource<bool> _LoadTask;
    public static Task LoadTask => _LoadTask?.Task ?? Task.CompletedTask;
    public static void InvokeLoadEnd()
    {
        _LoadTask?.TrySetResult(true);
        _LoadTask = null;
    }
    private static async Task Load()
    {
        Main.Load.Visible = true;
        Tween tween = Main.Load.CreateTween();
        tween.TweenProperty(Main.Load,"modulate",new Color(1,1,1,1),0.2f);
        await Main.Load.ToSignal(tween, Tween.SignalName.Finished);
    }
    private static async Task LoadEnd()
    {
        Main.Load.Visible = true;
        Tween tween = Main.Load.CreateTween();
        tween.TweenProperty(Main.Load, "modulate", new Color(1, 1, 1, 0), 0.2f);
        await Main.Load.ToSignal(tween, Tween.SignalName.Finished);
    }
    /// <summary>
    /// 打开一个子目录
    /// </summary>
    /// <param name="subMenu">子目录</param>
    public static async void JumpToSub(SubMenu subMenu)
    {
        if (subMenu == null) return;
        var parent =
            Main.SceneContainer;
        _LoadTask = new TaskCompletionSource<bool>();
        parent.GetNode<CanvasLayer>("%UponScene").Layer = 0;
        var scene = subMenu.Instantiate(); ;
        if (Menus.Count == 0)
        {
            Menus.Add(subMenu, null);
            scene.Visible = false;
            parent.GetNode<CanvasLayer>("%UponScene").AddChild(scene);
        }
        else
        {
            var last = Menus.Last().Key;
            Menus[subMenu] = last;
            last.Destory();
            scene.Visible = false;
            parent.GetNode<CanvasLayer>("%UponScene").AddChild(scene);
        }
        parent.GetNode<CanvasLayer>("Load").Layer = 1;
        await Load();
        await LoadTask;
        scene.Visible = true;
        await LoadEnd();
        parent.GetNode<CanvasLayer>("Load").Layer = 0;
        parent.GetNode<CanvasLayer>("%UponScene").Layer = 1;
    }
    /// <summary>
    /// 关闭顶层子目录
    /// </summary>
    public static async void ReturnToSub()
    {
        var last = Menus.Last();
        Menus.Remove(last.Key);
        if (last.Value == null)
        {
            var parent =
                Main.SceneContainer;
            parent.GetNode<CanvasLayer>("%UponScene").Layer = 0;
            parent.GetNode<CanvasLayer>("Load").Layer = 1;
            await Load();
            last.Key.Destory();
            await LoadEnd();
            parent.GetNode<CanvasLayer>("Load").Layer = 0;
            parent.GetNode<CanvasLayer>("%UponScene").Layer = 1;
        }
        else
        {
            JumpToSub(last.Value);
        }
    }
}
