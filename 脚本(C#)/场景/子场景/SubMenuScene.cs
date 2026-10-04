using Controller;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scene;

public abstract partial class SubMenuScene : Node2D
{
    public sealed override async void _EnterTree()
    {
        BackButton.JoinScene();
        await _Load();
        SceneManager.InvokeLoadEnd();
    }
    public abstract Task _Load();
}
