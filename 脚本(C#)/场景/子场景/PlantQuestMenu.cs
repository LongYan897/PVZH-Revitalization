using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class PlantQuestMenu : SubMenu
{
    protected override void EnterField()
    {
        QuestSubMenuScene.Quest = Card.Camp.Plant;
    }
}
