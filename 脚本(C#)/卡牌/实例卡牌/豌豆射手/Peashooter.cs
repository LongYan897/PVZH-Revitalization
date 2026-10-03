using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using Target;

namespace Card.Pea;

public class Peashooter : FighterCardModel
{
    public override string AmmoPath => "res://素材(C#)/卡牌/豌豆射手/PeaAmmo.png";
    protected override List<Buff> InitBuffs => [
        new PlayedExtraAttackBuff()
        ];
}
