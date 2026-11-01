using Godot;
using Pack;
using Spine;

namespace Card;

[GlobalClass]
public partial class FireRate : Node2D
{
    private static PackedScene Scene => "res://场景/攻击特效/灼烧/fireRate.tscn".Load<PackedScene>();
    public static FireRate Create()
    {
        var rate = Scene.Instantiate<FireRate>();
        rate.GetNode<SpineHandler>("%生命模板").QueueFree();
        return rate;
    }

    public SpineHandler Main()
    {
        return GetNode<SpineHandler>("%火焰");
    }
}
