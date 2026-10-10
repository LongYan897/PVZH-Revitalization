using Card;
using Card.Dance;
using Card.Flower;
using Card.Imp;
using Card.Pea;
using Card.Scientist;
using Godot;
using Phrases;
using Spine;
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
        GetNode<SpineHandler>("%动画").LoadSkeletonData("res://素材/ui/轮盘动画/轮盘动画.tres");
        PhraseManager.SetUp(GetNode<Button>("%Button"),GetNode<SpineHandler>("%动画"));
        NodeCard.DisplayCard(CardModel.Load<CardboardBoat>(),new(360,850));
        NodeCard.DisplayCard(CardModel.Load<BackupDancer>(), new(120, 850));
        NodeCard.DisplayCard(CardModel.Load<SunFlower>(), new(240, 850));
        return Task.CompletedTask;
    }
}
