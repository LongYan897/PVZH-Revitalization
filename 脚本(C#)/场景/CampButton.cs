using Card;
using Godot;
using Pack;

namespace Scene;

[GlobalClass]
public partial class CampButton : NButton
{
    [Export]
    public string Campite { get; set; }
    protected override void OnUp()
    {
        SelectDeck.CurrentCamp = Campite switch
        {
            "P" => Camp.Plant,
            "Z" => Camp.Zombie,
            _ => Camp.Plant
        };
    }
}
