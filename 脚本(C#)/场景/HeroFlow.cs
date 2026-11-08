using Card;
using Card.Pack;
using Godot;
using Hero;
using Hero.Model;
using Pack;
using Play;
using System.Collections.Generic;
using System.Linq;

namespace Scene;

[GlobalClass]
public partial class HeroFlow : MinimumHFlowContainer
{
    private static readonly PackedScene Scene = "res://场景(C#)/英雄卡组.tscn".Load<PackedScene>();
    public HeroModel CurrentHero { get; private set; }

    public static HeroFlow Flow(HeroModel heroModel)
    {
        var flow = Scene.Instantiate<HeroFlow>();
        flow.Pref(heroModel);
        return flow;
    }

    private void Pref(HeroModel heroModel)
    {
        CurrentHero = heroModel;
        GetNode<Sprite2D>("%图标").Texture = heroModel.Data.IconPath.Load<Texture2D>();
        var cls = Classes(heroModel);
        GetNode<Sprite2D>("%属性").Texture = cls.c1.ToPath().Load<Texture2D>();
        GetNode<Sprite2D>("%属性2").Texture = cls.c2.ToPath().Load<Texture2D>();
        GetNode<Label>("%名称").Text = heroModel.Data.Title;
        GetNode<Button>("%Button").Pressed += () => Button(heroModel);
    }

    public void RefreshDecks()
    {
        if (CurrentHero == null) return;

        var decks = PlayerManager.HostPlayer?.Presets?.Where(d => d.Hero != null && d.Hero.GetType() == CurrentHero.GetType()).ToList() ?? new List<PresetPack>();

        var existingDecks = new List<CardDeck>();
        foreach (var child in GetChildren())
        {
            if (child is CardDeck cd && !cd.IsQueuedForDeletion())
            {
                existingDecks.Add(cd);
            }
        }

        var toRemove = existingDecks.Where(cd => !decks.Contains(cd.Deck)).ToList();
        foreach (var cd in toRemove)
        {
            existingDecks.Remove(cd);
            RemoveChild(cd);
            cd.QueueFree();
        }

        foreach (var deck in decks)
        {
            if (!existingDecks.Any(x => x.Deck == deck))
            {
                var cd = CardDeck.Instance(deck);
                AddChild(cd);
                existingDecks.Add(cd);
            }
        }

        for (int i = 0; i < decks.Count; i++)
        {
            var deck = decks[i];
            var cd = existingDecks.First(x => x.Deck == deck);
            MoveChild(cd, i + 1);
        }
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