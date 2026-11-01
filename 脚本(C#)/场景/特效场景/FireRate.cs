using Godot;
using Pack;
using Spine;

namespace Card;

[GlobalClass]
public partial class FireRate : Node2D
{
    private static PackedScene Scene => "res://场景/攻击特效/灼烧/fireRate.tscn".Load<PackedScene>();

    private const string SpinePath = "";

    public static FireRate Create(int amount)
    {
        var rate = Scene.Instantiate<FireRate>();
        rate.GetNode<SpineHandler>("%生命模板").QueueFree();
        for (int i = 0; i < amount; i++)
        {
            var spine = SpineHandler.Get();
            rate.AddChild(spine);
            spine.Name = $"生命{i}";
            spine.SetMeta("rate",i);
            spine.LoadSkeletonData(SpinePath);
        }
        return rate;
    }

    public void Fresh(int amount)
    {

    }

    public SpineHandler Main()
    {
        return GetNode<SpineHandler>("%火焰");
    }
}
