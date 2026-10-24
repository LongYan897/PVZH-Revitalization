using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class ZombieQuestMenu : SubMenu
{
    protected override void EnterField()
    {
        QuestSubMenuScene.Quest = Card.Camp.Zombie;
    }
}
