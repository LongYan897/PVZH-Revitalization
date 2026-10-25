using Card;
using Godot;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
