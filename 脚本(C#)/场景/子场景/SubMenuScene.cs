using Controller;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scene;

/// <summary>
/// 子场景
/// </summary>
public abstract partial class SubMenuScene : Node2D
{
    public sealed override async void _EnterTree()
    {
        BackButton.JoinScene();
        await _Load();
        SceneManager.InvokeLoadEnd();
    }
    public abstract Task _Load();
    /// <summary>
    /// 是否隐藏返回按钮
    /// </summary>
    public virtual bool HideReturnButton { get; }
}
