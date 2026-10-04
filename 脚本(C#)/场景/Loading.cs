using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class Loading : Node2D
{
    private int potAmount = 0;
    public override void _Process(double delta)
    {
        var str = "";
        for (int i = 0; i < potAmount; i++)
        {
            str += ".";
        }
        GetNode<Label>("Label").Text = $"加载中{str}";
    }
}
