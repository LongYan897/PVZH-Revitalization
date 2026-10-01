using Godot;
using Hero.String;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hero;
/// <summary>
/// 英雄描述的可视化部分
/// </summary>
[GlobalClass]
public partial class HeroDes : Control
{
    private static readonly PackedScene Scene = GD.Load<PackedScene>("res://场景(C#)/英雄界面.tscn");
    private string _heroName;
    private Tween _tween;
    private bool _isEnd;
    private void Open()
    {
        _isEnd = false;
        GetNode<Control>("%单位容器").ZIndex = 1036;
        GetNode<Control>("%单位容器").Position = new(360f, 305f);
        GetNode<Control>("%单位容器").TopLevel = true;
        GetNode<ColorRect>("%底色").MouseFilter = MouseFilterEnum.Stop;
        Scale = Vector2.Zero;
        GetNode<ColorRect>("%底色").Modulate = new Color(0, 0, 0, 0);
        _tween = CreateTween();
        _tween.SetTrans(Tween.TransitionType.Back);
        _tween.SetEase(Tween.EaseType.Out);
        _tween.Parallel().TweenProperty(this, "scale", Vector2.One, 0.5f);
        _tween.Parallel().TweenProperty(GetNode<ColorRect>("%底色"), "modulate", new Color(0, 0, 0, 0.5f), 0.5f);
        SpineHandler sprite = GetNode<SpineHandler>("%动画");
        sprite.Scale = new Vector2(0.4f, 0.4f);
        sprite.LoadSkeletonData(HeroString.GetData(_heroName, "Animation").AsString());
        sprite.SetAnimation(0, "intro", false);
        sprite.AddAnimation(0, "idle", true);
    }
    private async void Clear()
    {
        if (_isEnd) return;
        if (_tween != null && _tween.IsValid())
        {
            _tween.Kill();
        }
        GetNode<Control>("%单位容器").ZIndex = 12;
        GetNode<Control>("%单位容器").Position = new(0f, -325f);
        GetNode<Control>("%单位容器").TopLevel = false;
        var sprite = GetNode<SpineHandler>("%动画");
        sprite.ClearTrack(0);
        _tween = CreateTween();
        _tween.SetTrans(Tween.TransitionType.Back);
        _tween.SetEase(Tween.EaseType.In);
        _tween.Parallel().TweenProperty(this, "scale", Vector2.Zero, 0.3f);
        _tween.Parallel().TweenProperty(GetNode<ColorRect>("%底色"), "modulate", new Color(0, 0, 0, 0), 0.3f);

        await ToSignal(GetTree().CreateTimer(0.1f), Timer.SignalName.Timeout);
        GetNode<ColorRect>("%底色").MouseFilter = MouseFilterEnum.Ignore;

        await ToSignal(GetTree().CreateTimer(0.4f), Timer.SignalName.Timeout);
        Visible = false;
    }
    public override void _Input(InputEvent @event)
    {
        if(@event is InputEventScreenTouch touch)
        {
            if (!touch.Pressed) return;
            if (!_isEnd)
            {
                Clear();
                _isEnd = true;
                GetViewport().SetInputAsHandled();
            }
        }
    }
    public static HeroDes Create(string heroName)
    {
        var instance = Scene.Instantiate<HeroDes>();
        instance._heroName = heroName;
        return instance;
    }
    public void Display()
    {
        Open();
    }
}
