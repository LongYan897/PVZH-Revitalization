
using Card.Pack;
using Godot;
using Logger;

namespace Scene;

[GlobalClass]
public partial class EditSubMenu : SubMenu
{
    public static PresetPack CurrentPack;
    protected override void EnterField()
    {
        var diy = new DIYPack() { Hero = CurrentPack.Hero , PackName = CurrentPack.PackName};
        diy.AddCards(CurrentPack.Cards);
        DeckEditSubMenuScene.CurrentPack = diy;
    }
}
