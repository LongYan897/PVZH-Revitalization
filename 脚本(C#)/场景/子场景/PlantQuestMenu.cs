using Godot;

namespace Scene;

[GlobalClass]
public partial class PlantQuestMenu : SubMenu
{
    protected override void EnterField()
    {
        QuestSubMenuScene.Quest = Card.Camp.Plant;
    }
}
