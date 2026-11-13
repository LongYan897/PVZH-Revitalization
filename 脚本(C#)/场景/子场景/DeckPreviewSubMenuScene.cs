using Card;
using Card.Pack;
using Godot;
using Pack;
using Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class DeckPreviewSubMenuScene : SubMenuScene
{
    public static PresetPack CurrentPack;
    public static DeckPreviewSubMenuScene Instance { get; private set; }

    public override void _WhenEnterTree()
    {
        Instance = this;
    }

    public override async Task _Load()
    {
        EditSubMenu.CurrentPack = CurrentPack;
        await Refresh();
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
        base._ExitTree();
    }

    public static async Task SafeRefresh()
    {
        if (Instance != null && GodotObject.IsInstanceValid(Instance) && !Instance.IsQueuedForDeletion())
        {
            await Instance.Refresh();
        }
    }

    public async Task Refresh()
    {
        if (CurrentPack == null) return;

        GetNode<Sprite2D>("%卡牌组").Texture = CurrentPack.GetBg();
        GetNode<Sprite2D>("%卡套").Texture = CurrentPack.GetFrame();
        GetNode<Sprite2D>("%封皮").Texture = (CurrentPack.CustomFrame ?? CurrentPack.Hero.Data.DeckIcon).Load<Texture2D>();
        GetNode<Label>("%名称").Text = CurrentPack.PackName;
        GetNode<Label>("%描述").Text = CurrentPack.Description;
        GetNode<Label>("%进度数字").Text = CurrentPack.LockedCard.ToString();
        GetNode<Panel>("%进度绿色条").Size = new(CurrentPack.LockedCard * 368f / 40f, 45f);
        GetNode<Panel>("%进度绿色亮条").Size = new(CurrentPack.LockedCard * 366f / 40f, 23f);

        var ownedContainer = GetNodeOrNull<Control>("%卡牌");
        var unownedContainer = GetNodeOrNull<Control>("%卡牌2");

        if (ownedContainer == null || unownedContainer == null) return;

        var asset = PlayerManager.HostPlayer?.Asset;
        if (asset == null) return;

        var camp = CurrentPack.Hero?.Camp ?? Camp.Plant;
        var classOrder = GetClassOrder(camp);
        var classRank = new Dictionary<Class, int>();
        for (int i = 0; i < classOrder.Count; i++)
            classRank[classOrder[i]] = i;

        var grouped = CurrentPack.Cards
            .Where(c => c != null && !string.IsNullOrEmpty(c.Title) && c.Rarity != Rarity.Token)
            .GroupBy(c => c.Title)
            .Select(g => (template: g.First(), deckCount: g.Count()))
            .OrderBy(x => x.template.Cost.Current)
            .ThenBy(x => classRank.TryGetValue(x.template.Class, out var rank) ? rank : int.MaxValue)
            .ThenBy(x => x.template.Title, StringComparer.Ordinal)
            .ToList();

        var existingOwned = ownedContainer.GetChildren().OfType<NodeCard>().ToDictionary(n => n.Model.Title);
        var existingUnowned = unownedContainer.GetChildren().OfType<NodeCard>().ToDictionary(n => n.Model.Title);

        foreach (var (template, deckCount) in grouped)
        {
            int playerHas = asset.GetAmount(template.Title);
            int showOwned = Math.Min(playerHas, deckCount);
            int showMissing = deckCount - playerHas;

            if (showOwned > 0)
            {
                if (existingOwned.TryGetValue(template.Title, out var node))
                {
                    if (node.ForcedAmount != showOwned) node.SetAmount(showOwned);
                    existingOwned.Remove(template.Title);
                }
                else
                {
                    var newNode = await NodeCard.CreateCard(template.Clone(), Vector2.Zero, CardView.Collection, ownedContainer, drawAnimation: false, 1f);
                    newNode.AllowExpand = false;
                    newNode.ForceShowAmount = true;
                    newNode.ForcedAmount = showOwned;
                    newNode.Fresh();
                    newNode.PreventGray = true;
                }
            }

            if (showMissing > 0)
            {
                if (existingUnowned.TryGetValue(template.Title, out var node))
                {
                    if (node.ForcedAmount != showMissing) node.SetAmount(showMissing);
                    existingUnowned.Remove(template.Title);
                }
                else
                {
                    var newNode = await NodeCard.CreateCard(template.Clone(), Vector2.Zero, CardView.Collection, unownedContainer, drawAnimation: false, 1f);
                    newNode.AllowExpand = false;
                    newNode.ForceShowAmount = true;
                    newNode.ForcedAmount = showMissing;
                    newNode.Fresh();
                }
            }
        }

        foreach (var node in existingOwned.Values) node.QueueFree();
        foreach (var node in existingUnowned.Values) node.QueueFree();
    }

    private static List<Class> GetClassOrder(Camp camp)
    {
        return camp switch
        {
            Camp.Plant => [
                Class.MegaGrow,
                Class.Guardian,
                Class.Kabloom,
                Class.Smarty,
                Class.Solar
            ],
            Camp.Zombie => [
                Class.Sneaky,
                Class.Hearty,
                Class.Crazy,
                Class.Brainy,
                Class.Beastly,
            ],
            _ => []
        };
    }
}