using Godot;
using Pack;
using Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pack;

[GlobalClass]
public partial class CardDeck : Control
{
    private static readonly PackedScene Scene = "res://场景(C#)/卡组卡包.tscn".Load<PackedScene>();
    private static CardDeck CurrentEnable;
    public static CardDeck Instance(PresetPack preset)
    {
        CardDeck deck = Scene.Instantiate<CardDeck>();
        deck.Predeck(preset);
        return deck;
    }
    private async void Pressed(PresetPack preset)
    {
        GetNode<Sprite2D>("%背景").SelfModulate = new("084259");
        GetNode<Node2D>("%表层").Visible = false;
        GetNode<Node2D>("%内层").Visible = true;
        Scale = new(1, 1);
        Tween tween = CreateTween();
        tween.TweenProperty(this, "scale", new Vector2(1, 1) * 0.98f, 0.1f).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", new Vector2(1, 1), 0.1f).SetEase(Tween.EaseType.In);
        await tween.ToTask();
        CurrentEnable?.UnPressed();
        CurrentEnable = this;
        GetNode<Button>("Button").MouseFilter = MouseFilterEnum.Ignore;
    }
    private void UnPressed()
    {
        GetNode<Sprite2D>("%背景").SelfModulate = new("ffffff");
        GetNode<Node2D>("%表层").Visible = true;
        GetNode<Node2D>("%内层").Visible = false;
        GetNode<Button>("Button").MouseFilter = MouseFilterEnum.Stop;
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed)
                CurrentEnable?.UnPressed();
        }
    }
    private void Predeck(PresetPack preset)
    {
        GetNode<Button>("Button").Pressed += () => Pressed(preset);
        GetNode<Sprite2D>("%封皮").Texture = (preset.CustomFrame ?? preset.Hero.Data.DeckIcon).Load<Texture2D>();
        GetNode<Sprite2D>("%背景").Texture = preset.GetBg();
        GetNode<Sprite2D>("%卡套").Texture = preset.GetFrame();
        GetNode<Sprite2D>("%卡套").Modulate = preset is DIYPack ? new Color("1d658a") : new Color(1,1,1,1);
    }
}
