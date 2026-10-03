using Card;
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
        try
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
            sprite.AddAnimation(0, "idle", true);
        }
        catch (Exception e)
        {
            GD.PushError($"英雄描述界面打开失败: {e.Message}");
        }
    }
    private async void Clear()
    {
        try
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
        catch (Exception e)
        {
            GD.PushError($"英雄描述界面关闭失败: {e.Message}");
        }
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
        GetNode<Control>("%单位容器").Scale = HeroString.GetData(_heroName,"DesScale").AsSingle()*new Vector2(1,1);
        GetNode<Control>("%单位容器").Position = HeroString.GetData(_heroName, "DesOffset").AsSingle() * new Vector2(0, 1) + new Vector2(0,-335);
        Open();
        Fresh();
    }
    private void Fresh()
    {
        GetNode<Label>("%名字").Text = HeroString.GetData(_heroName, "Title").AsString();
        GetNode<RichTextLabel>("%故事标签").Text = HeroString.GetData(_heroName, "Flavor").AsString();
        List<Class> category = HeroString.Loading(_heroName).Category;
        GetNode<TextureRect>("%属性图片").Texture = ResourceLoader.Load<CompressedTexture2D>(HeroString.Loading(_heroName).Category[0].ToPath());
        GetNode<TextureRect>("%属性图片2").Texture = ResourceLoader.Load<CompressedTexture2D>(HeroString.Loading(_heroName).Category[1].ToPath());
        GetNode<TextureRect>("%左背景").Modulate = category[1].ToColor();
        GetNode<TextureRect>("%右背景").Modulate = HeroString.Loading(_heroName).Category[0].ToColor();
        GetNode<Sprite2D>("%单位框").Modulate = category[1].ToColor();
        GetNode<Sprite2D>("%单位框2").Modulate = category[0].ToColor();
    }
    
}
