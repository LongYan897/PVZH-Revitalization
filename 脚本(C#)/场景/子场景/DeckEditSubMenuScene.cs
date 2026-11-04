using Card;
using Card.Pack;
using Controller;
using Godot;
using Pack;
using Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class DeckEditSubMenuScene : SubMenuScene
{
    public static PresetPack CurrentPack;

    private static readonly Color RemoveColor = new("ff0000");
    private static readonly Color AddColor = new("15ff00");

    private DIYPack _diy;
    private Control _deckContainer;
    private Control _ownedContainer;
    private Control _unownedContainer;
    private Dictionary<Class, int> _classRank;
    private readonly Dictionary<string, NodeCard> _deckCards = new();

    public override async Task _Load()
    {
        if (CurrentPack is DIYPack dIY)
        {
            _diy = dIY;
            await LoadPreset(dIY);
        }
    }

    private async Task LoadPreset(DIYPack dIY)
    {
        var hero = dIY.Hero;
        var asset = PlayerManager.HostPlayer?.Asset;
        var classOrder = GetClassOrder(hero?.Camp ?? Camp.Plant);
        _classRank = new Dictionary<Class, int>();
        for (int i = 0; i < classOrder.Count; i++)
            _classRank[classOrder[i]] = i;

        _deckContainer = GetNodeOrNull<Control>("%已进入卡组");
        _ownedContainer = GetNodeOrNull<Control>("%已拥有");
        _unownedContainer = GetNodeOrNull<Control>("%未拥有");

        ClearCards(_deckContainer);
        ClearCards(_ownedContainer);
        ClearCards(_unownedContainer);
        _deckCards.Clear();

        var cardsAll = CardModel.AllCards
            .Where(c => c != null && !string.IsNullOrEmpty(c.Title) && hero != null && hero.Camp == c.Camp && hero.Data.Category.Contains(c.Class))
            .GroupBy(c => c.Title)
            .Select(g => g.First())
            .OrderBy(c => _classRank.TryGetValue(c.Class, out var rank) ? rank : int.MaxValue)
            .ThenBy(c => c.Cost.Current)
            .ThenBy(c => c.Title, StringComparer.Ordinal)
            .ToList();

        var deckGroups = (dIY.Cards ?? new List<CardModel>())
            .Where(c => c != null && !string.IsNullOrEmpty(c.Title))
            .GroupBy(c => c.Title)
            .ToDictionary(g => g.Key, g => (template: g.First(), count: g.Count()));

        var deckOrdered = deckGroups.Values
            .OrderBy(x => _classRank.TryGetValue(x.template.Class, out var rank) ? rank : int.MaxValue)
            .ThenBy(x => x.template.Cost.Current)
            .ThenBy(x => x.template.Title, StringComparer.Ordinal)
            .ToList();

        if (_deckContainer != null)
        {
            foreach (var (template, count) in deckOrdered)
            {
                if (count <= 0) continue;
                var node = await CreateCard(template, _deckContainer, count, true, true, CardView.Deck, allowExpand: true);
                _deckCards[template.Title] = node;
                SetupOperateButton(node, template.Title, isRemove: true);
            }
        }

        foreach (var template in cardsAll)
        {
            int playerHas = asset?.GetAmount(template.Title) ?? 0;
            int deckCount = deckGroups.TryGetValue(template.Title, out var dg) ? dg.count : 0;
            int available = playerHas - deckCount;

            if (available > 0 && _ownedContainer != null)
            {
                var node = await CreateCard(template, _ownedContainer, available, true, true, CardView.Deck, allowExpand: true);
                SetupOperateButton(node, template.Title, isRemove: false);
            }
            else if (playerHas == 0 && _unownedContainer != null)
            {
                await CreateCard(template, _unownedContainer, 0, false, false, CardView.Collection, allowExpand: false);
            }
        }
    }

    private static async Task<NodeCard> CreateCard(CardModel template, Control container, int amount, bool showAmount, bool preventGray, CardView cardView, bool allowExpand)
    {
        var node = await NodeCard.CreateCard(
            template.Clone(),
            Vector2.Zero,
            cardView,
            container,
            drawAnimation: false,
            1f);
        node.AllowExpand = allowExpand;
        node.ForceShowAmount = showAmount;
        node.ForcedAmount = amount;
        node.Fresh();
        node.PreventGray = preventGray;
        return node;
    }

    private void SetupOperateButton(NodeCard node, string title, bool isRemove)
    {
        var opBtn = node.GetNodeOrNull<InputNineRect>("%操作按钮");
        if (opBtn == null) return;

        TrySetButtonText(opBtn, isRemove ? "移除" : "添加");
        opBtn.SelfModulate = isRemove ? RemoveColor : AddColor;

        opBtn.Input += async (@event) =>
        {
            if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
            {
                if (!opBtn.Visible || opBtn.MouseFilter == Control.MouseFilterEnum.Ignore) return;
                if (isRemove)
                    await OnRemoveCard(title, node);
                else
                    await OnAddCard(title, node);
            }
        };
    }

    private async Task OnAddCard(string title, NodeCard ownedNode)
    {
        if (ownedNode == null || ownedNode.Model == null) return;

        int before = _diy.Cards.Count(c => c != null && c.Title == title);
        _diy.AddCard(ownedNode.Model.Clone());
        int after = _diy.Cards.Count(c => c != null && c.Title == title);
        if (after <= before) return;

        Vector2 fromPos = ownedNode.GlobalPosition;
        var template = ownedNode.Model;

        NodeCard deckNode;
        if (_deckCards.TryGetValue(title, out var exist))
        {
            exist.SetAmount(exist.ForcedAmount + 1);
            deckNode = exist;
        }
        else
        {
            deckNode = await CreateCard(template, _deckContainer, 1, true, true, CardView.Deck, allowExpand: true);
            SetupOperateButton(deckNode, title, isRemove: true);
            _deckCards[title] = deckNode;
            await InsertOrdered(_deckContainer, deckNode, template);
        }

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        PlayFlyEffect(fromPos, deckNode.GlobalPosition, template);

        int newAmount = ownedNode.ForcedAmount - 1;
        if (newAmount > 0)
            ownedNode.SetAmount(newAmount);
        else
            ownedNode.QueueFree();
    }

    private async Task OnRemoveCard(string title, NodeCard deckNode)
    {
        if (deckNode == null || deckNode.Model == null) return;

        var existing = _diy.Cards.FirstOrDefault(c => c != null && c.Title == title);
        if (existing == null) return;
        _diy.RemoveCard(existing);

        Vector2 fromPos = deckNode.GlobalPosition;
        var template = deckNode.Model;

        int newAmount = deckNode.ForcedAmount - 1;
        if (newAmount > 0)
        {
            deckNode.SetAmount(newAmount);
        }
        else
        {
            _deckCards.Remove(title);
            deckNode.QueueFree();
        }

        NodeCard ownedNode = FindOwnedCardByTitle(title);
        if (ownedNode != null)
        {
            ownedNode.SetAmount(ownedNode.ForcedAmount + 1);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            PlayFlyEffect(fromPos, ownedNode.GlobalPosition, template);
        }
        else if (_ownedContainer != null)
        {
            var newNode = await CreateCard(template, _ownedContainer, 1, true, true, CardView.Deck, allowExpand: true);
            SetupOperateButton(newNode, title, isRemove: false);
            await InsertOrdered(_ownedContainer, newNode, template);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            PlayFlyEffect(fromPos, newNode.GlobalPosition, template);
        }
    }

    private async void PlayFlyEffect(Vector2 fromGlobalPos, Vector2 toGlobalPos, CardModel template)
    {
        var temp = await NodeCard.CreateCard(
            template.Clone(),
            Vector2.Zero,
            CardView.Collection,
            Main.CardContainer,
            drawAnimation: false,
            1f);
        if (temp == null || !GodotObject.IsInstanceValid(temp)) return;

        temp.AllowExpand = false;
        temp.ForceShowAmount = false;
        temp.Fresh();
        temp.PreventGray = true;
        temp.GlobalPosition = fromGlobalPos;

        var startScale = temp.Scale;
        var endScale = startScale * 0.6f;

        var tween = temp.CreateTween();
        tween.TweenProperty(temp, "global_position", toGlobalPos, 0.3f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(temp, "scale", endScale, 0.3f);
        await temp.ToSignal(tween, Tween.SignalName.Finished);

        if (GodotObject.IsInstanceValid(temp))
            temp.QueueFree();
    }

    private NodeCard FindOwnedCardByTitle(string title)
    {
        if (_ownedContainer == null) return null;
        foreach (var child in _ownedContainer.GetChildren().OfType<NodeCard>())
            if (child.Model?.Title == title) return child;
        return null;
    }

    private async Task InsertOrdered(Control container, NodeCard node, CardModel template)
    {
        if (container == null) return;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var children = container.GetChildren().OfType<NodeCard>().ToList();
        int target = -1;
        for (int i = 0; i < children.Count; i++)
        {
            var c = children[i];
            if (c == node || c.Model == null) continue;
            if (Compare(template, c.Model) < 0)
            {
                target = i;
                break;
            }
        }
        if (target >= 0)
            container.MoveChild(node, target);
    }

    private int Compare(CardModel a, CardModel b)
    {
        int ra = _classRank.TryGetValue(a.Class, out var r1) ? r1 : int.MaxValue;
        int rb = _classRank.TryGetValue(b.Class, out var r2) ? r2 : int.MaxValue;
        if (ra != rb) return ra.CompareTo(rb);
        int ca = a.Cost.Current, cb = b.Cost.Current;
        if (ca != cb) return ca.CompareTo(cb);
        return string.CompareOrdinal(a.Title, b.Title);
    }

    private static bool TrySetButtonText(Node node, string text)
    {
        if (node == null) return false;
        if (node is Button btn) { btn.Text = text; return true; }
        if (node is Label lbl) { lbl.Text = text; return true; }
        if (node is RichTextLabel rtl) { rtl.Text = text; return true; }
        foreach (var child in node.GetChildren())
            if (TrySetButtonText(child, text)) return true;
        return false;
    }

    private static void ClearCards(Control container)
    {
        if (container == null) return;
        foreach (var child in container.GetChildren().OfType<NodeCard>())
            child.QueueFree();
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