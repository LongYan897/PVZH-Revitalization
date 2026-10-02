using Battle;
using Battle.Entity;
using Card;
using Card.Cmd;
using Card.Imp;
using Card.Pea;
using Godot;
using Pack;

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
        CardModel card3 = CardModel.Load<CopterCommando>();
        card1.Status = Status.FaceUp;
        card2.Status = Status.FaceUp;
        NodeCard.DrawACard(card1, new(0,0));
        NodeCard.DrawACard(card2, new(120, 0));
        await CardCmd.FighterGenerate(card3 as FighterCardModel,GetNode<NodeRoad>("道路").Model,Location.Zombie);
    }
}
