using Card;
using Godot;
using Hero;
using Hero.Model;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class HeroFlow : MinimumHFlowContainer
{
    private static readonly PackedScene Scene = "res://场景(C#)/英雄卡组.tscn".Load<PackedScene>();
    public static HeroFlow Flow(HeroModel heroModel)
    {
        var flow = Scene.Instantiate<HeroFlow>();
        flow.Pref(heroModel);
        return flow;
    }
    private void Pref(HeroModel heroModel)
    {
        GetNode<Sprite2D>("%图标").Texture = heroModel.Data.IconPath.Load<Texture2D>();
        var cls = Classes(heroModel);
        GetNode<Sprite2D>("%属性").Texture = cls.c1.ToPath().Load<Texture2D>();
        GetNode<Sprite2D>("%属性2").Texture = cls.c2.ToPath().Load<Texture2D>();
        GetNode<Label>("%名称").Text = heroModel.Data.Title;
        GetNode<Button>("%Button").Pressed += () => Button(heroModel);
    }
    private void Button(HeroModel heroModel)
    {
        HeroDes heroDes = HeroDes.Create(heroModel.GetType().Name);
        GetTree().Root.GetNode<CanvasLayer>("MainController/Des").AddChild(heroDes);
        heroDes.Position = new Vector2(360, 640);
        heroDes.Display();
    }
    private (Class c1, Class c2) Classes(HeroModel heroModel)
    {
        var priorityOrder = new[]
        {
        Class.MegaGrow, Class.Solar, Class.Smarty, Class.Guardian, Class.Kabloom,
        Class.Brainy, Class.Crazy, Class.Hearty, Class.Sneaky, Class.Beastly,
        };

        var set = new HashSet<Class>(heroModel.Data.Category ?? new List<Class>());

        Class c1 = default, c2 = default;
        bool hasC1 = false;

        foreach (var c in priorityOrder)
        {
            if (!set.Contains(c)) continue;

            if (!hasC1) { c1 = c; hasC1 = true; }
            else { c2 = c; break; }
        }

        return (c1, c2);
    }
}
