using Battle.Entity;
using Card;
using Card.Imp;
using Card.Pea;
using Godot;
using Pack;

namespace Controller;

/// <summary>
/// 游戏的主控
/// </summary>
[GlobalClass]
public partial class Main : Node2D
{
    public static Node Animator { get; private set; }
    public static Node CardContainer { get; private set; }
    public static Node FighterContainer { get; private set; }
    public static Node DesLayer { get; private set; }
    public static Node AudioContainer { get; private set; }
    public override void _Ready()
    {
        //初始化各各组件
        HpType.Init();
        AtkType.Init();
        Icon.Init();
        //初始化容器
        Animator = GetNode<CanvasLayer>("Animators");
        CardContainer = GetNode<CanvasLayer>("Cards");
        FighterContainer = GetNode<CanvasLayer>("Fighters");
        DesLayer = GetNode<CanvasLayer>("Des");
        AudioContainer = GetNode<Node2D>("Audios");
        //
        CardModel card = CardModel.Load<Peashooter>();
        CardModel card1 = CardModel.Load<HailACopter>();
        CardModel card2 = CardModel.Load<RollingStone>();
        card1.Status = Status.FaceUp;
        card2.Status = Status.FaceUp;
        NodeCard.DrawACard(card1, new(0,0));
        NodeCard.DrawACard(card2, new(120, 0));
        Fighter.Generate(card as Peashooter, new Battle.Road() { Index = 1 }, 0);
    }
}
