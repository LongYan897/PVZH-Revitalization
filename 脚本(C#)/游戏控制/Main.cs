using Battle;
using Battle.Entity;
using Card;
using Card.Cmd;
using Card.Dance;
using Card.Imp;
using Card.Pea;
using Godot;
using Pack;
using Target;

namespace Controller;

/// <summary>
/// 游戏的主控
/// </summary>
[GlobalClass]
public partial class Main : Node
{
    public static Node Animator { get; private set; }
    public static Node CardContainer { get; private set; }
    public static Node FighterContainer { get; private set; }
    public static Node DesLayer { get; private set; }
    public static Node AudioContainer { get; private set; }
    public override async void _Ready()
    {
        //初始化各各组件
        HpType.Init();
        AtkType.Init();
        Icon.Init();
        CardModel.Init();
        SceneAnimaActor.Init(this);
        //初始化容器
        Animator = GetNode<CanvasLayer>("Animators");
        CardContainer = GetNode<CanvasLayer>("Cards");
        FighterContainer = GetNode<CanvasLayer>("Fighters");
        DesLayer = GetNode<CanvasLayer>("Des");
        AudioContainer = GetNode<Node2D>("Audios");
        //
        CardModel card1 = CardModel.Load<HailACopter>();
        CardModel card2 = CardModel.Load<Peashooter>();
        CardModel card3 = CardModel.Load<BackupDancer>();
        CardModel card4 = CardModel.Load<BackupDancer>();
        CardModel card6 = CardModel.Load<FinalMission>();
        CardModel card5 = CardModel.Load<StartBattle>();
        card1.Status = Status.FaceUp;
        card2.Status = Status.FaceUp;
        card3.Status = Status.FaceUp;
        card4.Status = Status.FaceUp;
        card5.Status = Status.FaceUp;
        card6.Status = Status.FaceUp;
        NodeCard.DrawACard(card1, new(0,0));
        NodeCard.DrawACard(card2, new(120, 0));
        NodeCard.DrawACard(card3, new(240, 0));
        NodeCard.DrawACard(card4, new(360, 0));
        NodeCard.DrawACard(card5, new(480, 0));
        NodeCard.DrawACard(card6, new(600, 0));
    }

    public void Clear()
    {
        foreach (var item in Animator.GetChildren())
        {
            item.QueueFree();
        }
        foreach (var item in CardContainer.GetChildren())
        {
            item.QueueFree();
        }
        foreach (var item in FighterContainer.GetChildren())
        {
            item.QueueFree();
        }
        foreach (var item in DesLayer.GetChildren())
        {
            item.QueueFree();
        }
        foreach (var item in AudioContainer.GetChildren())
        {
            item.QueueFree();
        }
        TargetRegistry.Clear();
        CardCmd.Clear();
    }
}
