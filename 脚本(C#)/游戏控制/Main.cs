using Battle.Entity;
using Card;
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
    public override void _Ready()
    {
        //初始化各各组件
        HpType.Init();
        AtkType.Init();
        Icon.Init();
        //初始化容器
        Animator = GetNode<CanvasLayer>("Animators");
        //
        CardModel card = CardModel.Load<Peashooter>();
        CardModel card1 = CardModel.Load<RollingStone>();
        card1.Status = Status.FaceUp;
        NodeCard.DrawACard(this, card1, new(0, 0));
        Fighter.Generate(this, card as Peashooter, new Battle.Road() { Index = 1 }, 0);
    }
}
