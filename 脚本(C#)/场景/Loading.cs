using Godot;
using Spine;
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
    private SpineHandler _spine;
    public override void _Ready()
    {
        _spine = GetNode<SpineHandler>("动画");
        _spine.LoadSkeletonData("res://数据资源/杂项/加载动画.tres");
    }
    public void LoadIn()
    {
        Visible = true;
        Random random = new();
        _spine.SetAnimation(0,random.Next(0,2) switch
        {
            0 => "loading",
            1 => "loading_behind",
            _ => null
        },false);
    }
    public void LoadOut()
    {
        Visible = false;
    }
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
    }
}
