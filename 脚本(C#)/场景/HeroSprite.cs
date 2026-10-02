using Godot;
using Hero;
using Hero.String;
using Pack;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class HeroSprite : Control
{
    private NButton Button;
    private string _heroName;
    public override void _Ready()
    {
        Button = GetNode<NButton>("图标按钮");
        Texture2D texture = ResourceLoader.Load<Texture2D>(HeroString.GetData(_heroName, "Icon").AsString());
        Button.TextureNormal = texture;
        Button.Pressed += OnNButtonPressed;
    }
    private void OnNButtonPressed()
    {
        HeroDes heroDes = HeroDes.Create(_heroName);
        GetTree().CurrentScene.AddChild(heroDes);
        heroDes.Position = new Vector2(360,640);
        heroDes.ZIndex = 1000;
        heroDes.Display();
    }

    public static HeroSprite Create(string heroName)
    {
        var scene = GD.Load<PackedScene>("res://场景(C#)/英雄图标.tscn");
        var instance = scene.Instantiate<HeroSprite>();
        instance._heroName = heroName;
        return instance;
    }
}
