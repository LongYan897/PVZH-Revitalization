using Card.Pack;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class DeckEditSubMenuScene : SubMenuScene
{
    public static PresetPack CurrentPack;
    public override Task _Load()
    {
        return Task.CompletedTask;
    }
}
