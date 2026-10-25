using Card;
using Card.Pack;
using Godot;
using Pack;
using Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class DeckPreviewSubMenuScene : SubMenuScene
{
    public static PresetPack CurrentPack;
    public override async Task _Load()
    {
        if (CurrentPack != null)
        {
            GetNode<Sprite2D>("%卡牌组").Texture = CurrentPack.GetBg();
            GetNode<Sprite2D>("%卡套").Texture = CurrentPack.GetFrame();
            GetNode<Sprite2D>("%封皮").Texture = (CurrentPack.CustomFrame ?? CurrentPack.Hero.Data.DeckIcon).Load<Texture2D>();
            GetNode<Label>("%名称").Text = CurrentPack.PackName;
            GetNode<Label>("%描述").Text = CurrentPack.Description;
            await CreateDeckCards();
        }
    }
    private async Task CreateDeckCards()
    {
        var ownedContainer = GetNodeOrNull<Control>("%卡牌");
        var unownedContainer = GetNodeOrNull<Control>("%卡牌2");

        if (ownedContainer != null)
        {
            foreach (var child in ownedContainer.GetChildren())
                child.QueueFree();
        }
        if (unownedContainer != null)
        {
            foreach (var child in unownedContainer.GetChildren())
                child.QueueFree();
        }

        var asset = PlayerManager.HostPlayer?.Asset;
        if (asset == null) return;

        var grouped = CurrentPack.Cards
            .Where(c => c != null && !string.IsNullOrEmpty(c.Title))
            .GroupBy(c => c.Title)
            .ToList();

        foreach (var group in grouped)
        {
            var template = group.First();
            int deckCount = group.Count();
            int owned = Mathf.Min(asset.GetAmount(template.Title), deckCount);

            if (owned > 0 && ownedContainer != null)
            {
                var node = await NodeCard.CreateCard(
                    template.Clone(),
                    Vector2.Zero,
                    CardView.Collection,
                    ownedContainer,
                    drawAnimation: false,
                    1.5f);
                node.AllowExpand = false;
                node.ForceShowAmount = true;
                node.ForcedAmount = owned;
                node.Fresh();
            }

            if (owned >= 4 || owned >= deckCount) continue;

            int missing = deckCount - owned;
            if (missing > 0 && unownedContainer != null)
            {
                var node = await NodeCard.CreateCard(
                    template.Clone(),
                    Vector2.Zero,
                    CardView.Collection,
                    unownedContainer,
                    drawAnimation: false,
                    1.5f);
                node.AllowExpand = false;
                node.ForceShowAmount = true;
                node.ForcedAmount = missing;
                node.Fresh();
            }
        }
    }
}
