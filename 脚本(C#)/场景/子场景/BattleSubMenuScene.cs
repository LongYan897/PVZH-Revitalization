using Card;
using Card.Dance;
using Card.Pea;
using Card.Scientist;
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
    public override bool HideReturnButton => true;
    public override Task _Load()
    {
        PhraseManager.BuildUp(GetNode<Node2D>("道路层"));
        NodeCard.DisplayCard(CardModel.Load<BackupDancer>(),new(360,850));
        NodeCard.DisplayCard(CardModel.Load<CardboardBoat>(), new(120, 850));
        return Task.CompletedTask;
    }
}
