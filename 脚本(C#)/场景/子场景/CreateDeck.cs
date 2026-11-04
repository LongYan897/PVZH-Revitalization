using Card.Pack;
using Godot;
using Hero.Model;
using Pack;
using Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

[GlobalClass]
public partial class CreateDeck : SubMenu
{
    private static readonly PackedScene Scene = "res://场景(C#)/创建卡包.tscn".Load<PackedScene>();

    private HeroModel Hero;

    public static Control Instance(HeroModel hero)
    {
        Control deck = Scene.Instantiate<Control>();
        CreateDeck create = deck.GetNode<CreateDeck>("Control");
        create.Predeck(hero);
        return deck;
    }

    private void Predeck(HeroModel hero)
    {
        Hero = hero;
    }

    protected override void EnterField()
    {
        DeckEditSubMenuScene.CurrentPack = new DIYPack() { Hero = Hero , PackName = ""};
    }
}
