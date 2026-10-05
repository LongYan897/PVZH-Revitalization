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
    private double potDelta;
    public override void _Process(double delta)
    {
        var str = "";
        for (int i = 0; i < potAmount; i++)
        {
            str += ".";
        }
        if (potDelta > 1f)
        {
            if (potAmount < 3)
                potAmount++;
            else
                potAmount = 0;
            potDelta = 0;
        }
        else
            potDelta += delta;
        GetNode<Label>("Label").Text = $"加载中{str}";
        GetNode<Label>("Label").MouseFilter = Control.MouseFilterEnum.Ignore;
        GetNode<TextureRect>("TextureRect").MouseFilter = Visible switch
        {
            false => Control.MouseFilterEnum.Ignore,
            true => Control.MouseFilterEnum.Stop
        };
    }
}
