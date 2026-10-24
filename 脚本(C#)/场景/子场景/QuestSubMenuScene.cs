using Card;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class QuestSubMenuScene : SubMenuScene
{
    public static Camp Quest;
    public override Task _Load()
    {
        switch (Quest)
        {
            case Camp.Plant:
                {
                    GetNode<BgFrame>("Bg").Color = new("49deb6");
                    break;
                }
            case Camp.Zombie:
                {
                    GetNode<BgFrame>("Bg").Color = new("632ce6");
                    break;
                }
        }
        return Task.CompletedTask;
    }
}
