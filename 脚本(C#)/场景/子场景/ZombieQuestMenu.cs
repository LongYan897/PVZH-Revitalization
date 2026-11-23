using Godot;

namespace Scene;

[GlobalClass]
public partial class ZombieQuestMenu : SubMenu
{
    protected override void EnterField()
    {
        QuestSubMenuScene.Quest = Card.Camp.Zombie;
    }
}
