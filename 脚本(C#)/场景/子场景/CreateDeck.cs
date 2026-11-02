using Card.Pack;
using Godot;
using Hero.Model;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

public partial class CreateDeck : Control
{
    private static readonly PackedScene Scene = "res://场景(C#)/创建卡包.tscn".Load<PackedScene>();

    public static CreateDeck Instance(HeroModel hero)
    {
        CreateDeck deck = Scene.Instantiate<CreateDeck>();
        deck.Predeck(hero);
        return deck;
    }

    private void Predeck(HeroModel hero)
    {

    }
}
