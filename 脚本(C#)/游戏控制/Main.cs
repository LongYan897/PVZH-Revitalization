using Card.Pea;
using Godot;

namespace Card;

[GlobalClass]
public partial class Main : Node2D
{
    public override void _Ready()
    {
        HpType.Init();
        AtkType.Init();
        CardModel card = CardModel.Load<Peashooter>();
        card.Status = Status.FaceUp;
        NodeCard.Create(this, card).SetAnchor(new Vector2(0,0));
    }
}
