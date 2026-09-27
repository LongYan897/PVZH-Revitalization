using Card.Pea;
using Godot;

namespace Card;

/// <summary>
/// 游戏的主控
/// </summary>
[GlobalClass]
public partial class Main : Node2D
{
    public override void _Ready()
    {
        //初始化各各组件
        HpType.Init();
        AtkType.Init();
        //
        CardModel card = CardModel.Load<Peashooter>();
        card.Status = Status.FaceUp;
        NodeCard.DrawACard(this, card, new(0, 0));
    }
}
