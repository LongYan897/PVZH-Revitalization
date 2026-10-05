using Card;
using Card.Pea;
using Godot;
using Phrases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class BattleSubMenuScene : SubMenuScene
{
    public override Task _Load()
    {
        PhraseManager.BuildUp(GetNode<Node2D>("道路层"));
        NodeCard.DisplayCard(CardModel.Load<Peashooter>(),new(360,850));
        return Task.CompletedTask;
    }
}
