using Card;
using Card.Pack;
using Controller;
using Godot;
using Hero;
using Hero.Model;
using Pack;
using Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Widgets;

namespace Scene;

[GlobalClass]
public partial class DeckEditSubMenuScene : SubMenuScene
{
    public static PresetPack CurrentPack;
    public static DeckEditSubMenuScene Instance { get; private set; }

    private static readonly Color RemoveColor = new("#ff0000");
    private static readonly Color AddColor = new("#15ff00");

    private DIYPack _diy;
    private Control _deckContainer;
    private Control _ownedContainer;
    private Control _unownedContainer;
    private Dictionary<Class, int> _classRank;
    private readonly Dictionary<string, NodeCard> _deckCards = new();
    private Tween _countTween;
    private int _focusVersion = 0;
    private bool _isAutoBuilding = false;
    public override void _WhenEnterTree()
    {
        Instance = this;
    }

    public override async Task _Load()
    {
        _deckContainer = GetNodeOrNull<Control>("%已进入卡组");
        _ownedContainer = GetNodeOrNull<Control>("%已拥有");
        _unownedContainer = GetNodeOrNull<Control>("%未拥有");

        if (CurrentPack is DIYPack dIY)
        {
            _diy = dIY;
            SetupAutoBuildButton();
            SetupHeroDisplay(dIY.Hero);
            SetupPackName(dIY.Hero);
            SetupCampColors(dIY.Hero);
        }
        await Refresh();
    }

    public override async Task Outro()
    {
        var nameEdit = GetNodeOrNull<LineEdit>("%名称");
        if (nameEdit != null && _diy != null)
        {
            _diy.PackName = nameEdit.Text;
        }

        if (_diy != null && _diy.Cards != null && _diy.Cards.Count > 0)
        {
            var asset = PlayerManager.HostPlayer?.Asset;
            if (asset != null)
            {
                if (!asset.Presets.Contains(_diy))
                {
                    asset.AddPreset(_diy);
                }
            }
        }

        await SelectDeck.SafeRefresh();
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
        if (_diy == null) return;

        var hero = _diy.Hero;
        var asset = PlayerManager.HostPlayer?.Asset;
        if (asset == null) return;

        var classOrder = GetClassOrder(hero?.Camp ?? Camp.Plant);
        if (_classRank == null)
        {
            _classRank = new Dictionary<Class, int>();
            for (int i = 0; i < classOrder.Count; i++)
                _classRank[classOrder[i]] = i;
        }

        var deckGroups = (_diy.Cards ?? new List<CardModel>())
            .Where(c => c != null && !string.IsNullOrEmpty(c.Title) && c.Rarity != Rarity.Token)
            .GroupBy(c => c.Title)
            .ToDictionary(g => g.Key, g => (template: g.First(), count: g.Count()));

        var cardsAll = CardModel.AllCards
            .Where(c => c != null && !string.IsNullOrEmpty(c.Title) && c.Rarity != Rarity.Token && hero != null && hero.Camp == c.Camp && hero.Data.Category.Contains(c.Class))
            .GroupBy(c => c.Title)
            .Select(g => g.First())
            .OrderBy(c => _classRank.TryGetValue(c.Class, out var rank) ? rank : int.MaxValue)
            .ThenBy(c => c.Cost.Current)
            .ThenBy(c => CardModel.AllCards.IndexOf(c))
            .ToList();

        foreach (var kvp in _deckCards.ToList())
        {
            int count = deckGroups.TryGetValue(kvp.Key, out var dg) ? dg.count : 0;
            if (count <= 0)
            {
                kvp.Value.QueueFree();
                _deckCards.Remove(kvp.Key);
            }
            else if (kvp.Value.ForcedAmount != count)
            {
                kvp.Value.SetAmount(count);
            }
        }

        foreach (var kvp in deckGroups)
        {
            if (!_deckCards.ContainsKey(kvp.Key))
            {
                var template = kvp.Value.template;
                var node = await CreateCard(template, _deckContainer, kvp.Value.count, true, true, CardView.Deck, allowExpand: true);
                _deckCards[kvp.Key] = node;
                SetupOperateButton(node, kvp.Key, isRemove: true);
                await InsertOrdered(_deckContainer, node, template);
            }
        }

        foreach (var template in cardsAll)
        {
            int playerHas = asset.GetAmount(template.Title);
            int deckCount = deckGroups.TryGetValue(template.Title, out var dg) ? dg.count : 0;
            int available = playerHas - deckCount;

            NodeCard ownedNode = FindCardInContainer(_ownedContainer, template.Title);
            NodeCard unownedNode = FindCardInContainer(_unownedContainer, template.Title);

            if (available > 0)
            {
                if (ownedNode != null)
                {
                    if (ownedNode.ForcedAmount != available) ownedNode.SetAmount(available);
                }
                else
                {
                    var newNode = await CreateCard(template, _ownedContainer, available, true, true, CardView.Deck, allowExpand: true);
                    SetupOperateButton(newNode, template.Title, isRemove: false);
                    await InsertOrdered(_ownedContainer, newNode, template);
                }

                if (unownedNode != null)
                {
                    unownedNode.QueueFree();
                }
            }
            else
            {
                if (ownedNode != null)
                {
                    ownedNode.QueueFree();
                }

                if (playerHas == 0 && deckCount == 0)
                {
                    if (unownedNode == null)
                    {
                        var newNode = await CreateCard(template, _unownedContainer, 0, false, false, CardView.Collection, allowExpand: false);
                        await InsertOrdered(_unownedContainer, newNode, template);
                    }
                }
                else if (unownedNode != null)
                {
                    unownedNode.QueueFree();
                }
            }
        }

        UpdateDeckCountUI(false);
    }

    private NodeCard FindCardInContainer(Control container, string title)
    {
        if (container == null) return null;
        foreach (var child in container.GetChildren().OfType<NodeCard>())
            if (child.Model?.Title == title) return child;
        return null;
    }

    private void SetupAutoBuildButton()
    {
        var autoBtn = GetNodeOrNull<InputNineRect>("%帮我组完");
        if (autoBtn != null)
        {
            autoBtn.Input -= OnAutoBuildInput;
            autoBtn.Input += OnAutoBuildInput;
        }
    }

    private async void OnAutoBuildInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
        {
            if (_isAutoBuilding) return;
            var autoBtn = GetNodeOrNull<InputNineRect>("%帮我组完");
            if (autoBtn != null)
            {
                Tween tween = CreateTween();
                tween.TweenProperty(autoBtn, "scale", new Vector2(0.6f, 0.6f), 0.1f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
                tween.TweenProperty(autoBtn, "scale", new Vector2(0.7f, 0.7f), 0.1f).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
            }
            await AutoBuildDeck();
        }
    }

    private async Task AutoBuildDeck()
    {
        if (_diy == null || _isAutoBuilding) return;
        _isAutoBuilding = true;
        _focusVersion++;

        try
        {
            var asset = PlayerManager.HostPlayer?.Asset;
            if (asset == null) return;

            var hero = _diy.Hero;
            var availableCards = CardModel.AllCards
                .Where(c => c != null && !string.IsNullOrEmpty(c.Title) && c.Rarity != Rarity.Token && hero != null && hero.Camp == c.Camp && hero.Data.Category.Contains(c.Class) && asset.GetAmount(c.Title) > 0)
                .GroupBy(c => c.Title)
                .Select(g => g.First())
                .ToList();

            if (availableCards.Count == 0) return;

            var targetDeck = new List<CardModel>();
            int[] costLimits = { 2, 8, 10, 10, 6, 4, 2, 0, 0 };
            int[] targetCostCounts = new int[9];
            bool forceUnlimit = false;

            while (targetDeck.Count < 40)
            {
                bool addedThisRound = false;

                for (int cost = 0; cost <= 8; cost++)
                {
                    if (targetDeck.Count >= 40) break;
                    if (!forceUnlimit && targetCostCounts[cost] >= costLimits[cost]) continue;

                    CardModel bestCard = null;
                    int bestScore = -1;

                    foreach (var card in availableCards)
                    {
                        if (Math.Clamp(card.Cost.Current, 0, 8) != cost) continue;

                        int totalInTarget = targetDeck.Count(c => c.Title == card.Title);
                        int playerHas = asset.GetAmount(card.Title);

                        if (totalInTarget < 4 && totalInTarget < playerHas)
                        {
                            int score = CalculateLabelScore(card, targetDeck);
                            if (card.Rarity == Rarity.Legend) score += 2;
                            else if (card.Rarity == Rarity.SuperRare) score += 1;

                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestCard = card;
                            }
                        }
                    }

                    if (bestCard != null)
                    {
                        targetDeck.Add(bestCard.Clone());
                        targetCostCounts[cost]++;
                        addedThisRound = true;
                    }
                }

                if (!addedThisRound)
                {
                    if (!forceUnlimit) forceUnlimit = true;
                    else break;
                }
            }

            var currentCards = _diy.Cards.ToList();
            var targetCounts = targetDeck.GroupBy(c => c.Title).ToDictionary(g => g.Key, g => g.Count());

            var cardsToRemove = new List<CardModel>();
            foreach (var card in currentCards)
            {
                if (targetCounts.TryGetValue(card.Title, out int tCount) && tCount > 0)
                {
                    targetCounts[card.Title] = tCount - 1;
                }
                else
                {
                    cardsToRemove.Add(card);
                }
            }

            var cardsToAdd = new List<CardModel>();
            foreach (var card in targetDeck)
            {
                if (targetCounts.TryGetValue(card.Title, out int tCount) && tCount > 0)
                {
                    cardsToAdd.Add(card);
                    targetCounts[card.Title] = tCount - 1;
                }
            }

            cardsToRemove.Sort(Compare);
            cardsToAdd.Sort(Compare);

            foreach (var card in cardsToRemove)
            {
                await AutoRemoveFromDeck(card);
                await ToSignal(GetTree().CreateTimer(0.05f), Timer.SignalName.Timeout);
            }

            foreach (var card in cardsToAdd)
            {
                await AutoAddToDeck(card);
                await ToSignal(GetTree().CreateTimer(0.05f), Timer.SignalName.Timeout);
            }

            if (_deckContainer != null && _deckContainer.GetChildren().Count > 0)
            {
                var lastCard = _deckContainer.GetChildren().OfType<NodeCard>().LastOrDefault();
                if (lastCard != null) FocusCard(lastCard);
            }
        }
        finally
        {
            _isAutoBuilding = false;
            UpdateDeckCountUI(true);
        }
    }

    private async Task AutoRemoveFromDeck(CardModel card)
    {
        var existing = _diy.Cards.FirstOrDefault(c => c != null && c.Title == card.Title);
        if (existing == null) return;
        _diy.RemoveCard(existing);

        if (!_deckCards.TryGetValue(card.Title, out var deckNode)) return;

        Vector2 fromPos = deckNode.GlobalPosition;
        var template = deckNode.Model;

        int newAmount = deckNode.ForcedAmount - 1;
        if (newAmount > 0)
        {
            deckNode.SetAmount(newAmount);
        }
        else
        {
            _deckCards.Remove(card.Title);
            deckNode.QueueFree();
        }

        NodeCard ownedNode = FindCardInContainer(_ownedContainer, card.Title);
        if (ownedNode != null)
        {
            ownedNode.SetAmount(ownedNode.ForcedAmount + 1);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _ = PlayFlyEffect(fromPos, ownedNode.GlobalPosition, template);
            FocusCard(ownedNode);
        }
        else if (_ownedContainer != null)
        {
            var newNode = await CreateCard(template, _ownedContainer, 1, true, true, CardView.Deck, allowExpand: true);
            SetupOperateButton(newNode, card.Title, isRemove: false);
            await InsertOrdered(_ownedContainer, newNode, template);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _ = PlayFlyEffect(fromPos, newNode.GlobalPosition, template);
            FocusCard(newNode);
        }

        UpdateDeckCountUI(false);
    }

    private async Task AutoAddToDeck(CardModel card)
    {
        string title = card.Title;
        _diy.AddCard(card.Clone());

        NodeCard deckNode;
        if (_deckCards.TryGetValue(title, out var exist))
        {
            exist.SetAmount(exist.ForcedAmount + 1);
            deckNode = exist;
        }
        else
        {
            deckNode = await CreateCard(card, _deckContainer, 1, true, true, CardView.Deck, allowExpand: true);
            SetupOperateButton(deckNode, title, isRemove: true);
            _deckCards[title] = deckNode;
            await InsertOrdered(_deckContainer, deckNode, card);
        }

        NodeCard ownedNode = FindCardInContainer(_ownedContainer, title);
        Vector2 fromPos = _ownedContainer != null ? _ownedContainer.GlobalPosition + _ownedContainer.Size / 2f : Vector2.Zero;

        if (ownedNode != null)
        {
            fromPos = ownedNode.GlobalPosition;
            int newAmount = ownedNode.ForcedAmount - 1;
            if (newAmount > 0) ownedNode.SetAmount(newAmount);
            else ownedNode.QueueFree();
        }
        else if (_ownedContainer != null)
        {
            var asset = PlayerManager.HostPlayer?.Asset;
            int playerHas = asset?.GetAmount(title) ?? 0;
            int deckCount = _diy.Cards.Count(c => c.Title == title);
            int available = playerHas - deckCount;

            if (available > 0)
            {
                var newNode = await CreateCard(card, _ownedContainer, available, true, true, CardView.Deck, allowExpand: true);
                SetupOperateButton(newNode, title, isRemove: false);
                await InsertOrdered(_ownedContainer, newNode, card);
                ownedNode = newNode;
                fromPos = newNode.GlobalPosition;

                int newAmount = ownedNode.ForcedAmount - 1;
                if (newAmount > 0) ownedNode.SetAmount(newAmount);
                else ownedNode.QueueFree();
            }
        }

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _ = PlayFlyEffect(fromPos, deckNode.GlobalPosition, card);

        UpdateDeckCountUI(false);
        FocusCard(deckNode);
    }

    private int CalculateLabelScore(CardModel card, IReadOnlyList<CardModel> currentDeck)
    {
        if (card.Labels?.Current == null || card.Labels.Current.Count == 0 || currentDeck == null || currentDeck.Count == 0)
            return 0;

        var deckLabels = new Dictionary<string, int>();
        foreach (var deckCard in currentDeck)
        {
            if (deckCard.Labels?.Current != null)
            {
                foreach (var label in deckCard.Labels.Current)
                {
                    if (string.IsNullOrEmpty(label)) continue;
                    if (deckLabels.ContainsKey(label)) deckLabels[label]++;
                    else deckLabels[label] = 1;
                }
            }
        }

        int score = 0;
        foreach (var label in card.Labels.Current)
        {
            if (string.IsNullOrEmpty(label)) continue;
            if (deckLabels.TryGetValue(label, out int count))
            {
                score += count;
            }
        }

        return score;
    }

    private async void FocusCard(NodeCard card)
    {
        if (card == null) return;

        int currentVersion = ++_focusVersion;

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (currentVersion != _focusVersion) return;
        if (!GodotObject.IsInstanceValid(card) || !card.IsInsideTree()) return;

        Node current = card.GetParent();
        Node scrollContainer = null;

        while (current != null)
        {
            if (current.HasMethod("scroll_y_to"))
            {
                scrollContainer = current;
                break;
            }
            current = current.GetParent();
        }

        if (scrollContainer != null && scrollContainer is Control scrollControl)
        {
            var container = card.GetParent() as Control;
            if (container == null) return;

            int index = card.GetIndex();
            int row = index / 4;
            float cardHeight = card.Size.Y;

            if (cardHeight <= 0) cardHeight = 200f;

            float targetY = -(row * (cardHeight + 10f) - scrollControl.Size.Y / 2f + cardHeight / 2f);

            if (targetY > 0) targetY = 0;

            scrollContainer.Call("scroll_y_to", targetY, 0.4f);
        }
        else
        {
            if (scrollContainer != null && scrollContainer.HasMethod("ensure_control_visible_smooth"))
            {
                scrollContainer.Call("ensure_control_visible_smooth", card);
            }
        }
    }

    private void SetupCampColors(HeroModel hero)
    {
        if (hero == null) return;

        // 背景特效跟着阵营换色（植物=绿 / 僵尸=紫）
        GetNodeOrNull<SceneBackdrop>("%背景特效")?.SetCampTheme(hero.Camp);
        GetNodeOrNull<SceneLightSweep>("%光晕层")?.SetCampTheme(hero.Camp);

        if (hero.Camp == Camp.Plant)
        {
            var bar1 = GetNodeOrNull<CanvasItem>("%底层栏");
            if (bar1 != null) bar1.SelfModulate = new Color("#49734d");

            var bar2 = GetNodeOrNull<CanvasItem>("%底层栏2");
            if (bar2 != null) bar2.SelfModulate = new Color("#b3ffdb");

            var bar3 = GetNodeOrNull<CanvasItem>("%底层栏3");
            if (bar3 != null) bar3.SelfModulate = new Color("#429e4a");

            var bg2 = GetNodeOrNull<CanvasItem>("%Bg2");
            if (bg2 != null) bg2.SelfModulate = new Color("#57cf5d");
        }
        else if (hero.Camp == Camp.Zombie)
        {
            var bar1 = GetNodeOrNull<CanvasItem>("%底层栏");
            if (bar1 != null) bar1.SelfModulate = new Color("#494973");

            var bar2 = GetNodeOrNull<CanvasItem>("%底层栏2");
            if (bar2 != null) bar2.SelfModulate = new Color("#b3b8ff");

            var bar3 = GetNodeOrNull<CanvasItem>("%底层栏3");
            if (bar3 != null) bar3.SelfModulate = new Color("#58429e");

            var bg2 = GetNodeOrNull<CanvasItem>("%Bg2");
            if (bg2 != null) bg2.SelfModulate = new Color("#7f57cf");
        }
    }

    private void UpdateDeckCountUI(bool animate = true)
    {
        var countLabel = GetNodeOrNull<Label>("%数值");
        if (countLabel == null) return;

        if (countLabel.PivotOffset == Vector2.Zero)
            countLabel.PivotOffset = countLabel.Size / 2;

        int currentCount = _diy.Cards?.Count ?? 0;
        countLabel.Text = $"{currentCount}/40";

        var autoBtn = GetNodeOrNull<Control>("%帮我组完");
        if (autoBtn != null)
            autoBtn.Visible = (currentCount < 40);

        bool isPerfect = (currentCount == 40);
        Color startColor = isPerfect ? new Color("#7ed9de") : new Color("#6c9e2e");
        Color endColor = isPerfect ? new Color("#7ed9de") : new Color("#de8f28");

        if (_countTween != null && _countTween.IsValid())
        {
            _countTween.Kill();
        }

        if (animate)
        {
            countLabel.Scale = new Vector2(1.3f, 1.3f);
            countLabel.SelfModulate = startColor;

            _countTween = CreateTween();
            _countTween.SetParallel(true);

            _countTween.TweenProperty(countLabel, "scale", Vector2.One, 0.3f)
                 .SetTrans(Tween.TransitionType.Back)
                 .SetEase(Tween.EaseType.Out);

            _countTween.TweenProperty(countLabel, "self_modulate", endColor, 0.4f);
        }
        else
        {
            countLabel.Scale = Vector2.One;
            countLabel.SelfModulate = endColor;
        }
    }

    private void SetupHeroDisplay(HeroModel hero)
    {
        if (hero == null || hero.Data == null) return;

        SetTexture(GetNodeOrNull<Node>("%角色"), hero.Data.IconPath);

        var (c1, c2) = GetHeroClasses(hero);

        if (c1 != default)
        {
            var node1 = GetNodeOrNull<Node>("%属性图标1");
            SetTexture(node1, c1.ToPath());
            ApplyIconShader(node1, c1);

            var bg1 = GetNodeOrNull<Node>("%属性背景1");
            ApplyBackgroundGradient(bg1, c1, brightAtEnd: true);
        }

        if (c2 != default)
        {
            var node2 = GetNodeOrNull<Node>("%属性图标2");
            SetTexture(node2, c2.ToPath());
            ApplyIconShader(node2, c2);

            var bg2 = GetNodeOrNull<Node>("%属性背景2");
            ApplyBackgroundGradient(bg2, c2, brightAtEnd: false);
        }

        var btn = GetNodeOrNull<Button>("%Button");
        if (btn != null)
        {
            var captured = hero;
            btn.Pressed += () =>
            {
                HeroDes des = HeroDes.Create(captured.GetType().Name);
                GetTree().Root.GetNode<CanvasLayer>("MainController/Des").AddChild(des);
                des.Position = new Vector2(360, 640);
                des.Display();
            };
        }
    }

    private void ApplyIconShader(Node node, Class c)
    {
        if (node == null) return;

        ShaderMaterial mat = null;
        switch (node)
        {
            case Sprite2D s: mat = s.Material as ShaderMaterial; break;
            case TextureRect r: mat = r.Material as ShaderMaterial; break;
            case TextureButton b: mat = b.Material as ShaderMaterial; break;
        }

        if (mat == null || mat.Shader == null) return;

        float edge = 0.1f;
        float alpha = 0.01f;
        float dilation = 0.012f;
        float shrink = 0.1f;

        switch (c)
        {
            case Class.Solar:
                edge = 0.041f; alpha = 0.01f; dilation = 0.006f; shrink = 0.112f; break;
            case Class.MegaGrow:
            case Class.Smarty:
                edge = 0.195f; alpha = 0.01f; dilation = 0.012f; shrink = 0.101f; break;
            case Class.Kabloom:
                edge = 0.156f; alpha = 0.01f; dilation = 0.013f; shrink = 0.148f; break;
            case Class.Guardian:
                edge = 0.112f; alpha = 0.01f; dilation = 0.011f; shrink = 0.098f; break;
            case Class.Brainy:
                edge = 0.010f; alpha = 0.01f; dilation = 0.012f; shrink = 0.105f; break;
            case Class.Hearty:
                edge = 0.096f; alpha = 0.01f; dilation = 0.012f; shrink = 0.119f; break;
            case Class.Beastly:
                edge = 0.229f; alpha = 0.01f; dilation = 0.012f; shrink = 0.119f; break;
            case Class.Crazy:
                edge = 0.010f; alpha = 0.01f; dilation = 0.011f; shrink = 0.000f; break;
            case Class.Sneaky:
                edge = 0.500f; alpha = 0.01f; dilation = 0.011f; shrink = 0.000f; break;
            default:
                break;
        }

        mat.SetShaderParameter("edge_smoothness", edge);
        mat.SetShaderParameter("alpha_softness", alpha);
        mat.SetShaderParameter("dilation_radius", dilation);
        mat.SetShaderParameter("shrink", shrink);
    }

    private static void ApplyBackgroundGradient(Node node, Class c, bool brightAtEnd)
    {
        if (node == null) return;

        Texture2D tex = node switch
        {
            Sprite2D s => s.Texture,
            TextureRect r => r.Texture,
            _ => null
        };

        var (dark, bright) = GetClassBackgroundColors(c);

        if (tex is GradientTexture2D gradTex && gradTex.Gradient != null)
        {
            var gradient = (Gradient)gradTex.Gradient.Duplicate();

            if (gradient.GetPointCount() >= 2)
            {
                if (brightAtEnd)
                {
                    gradient.SetColor(0, dark);
                    gradient.SetColor(1, bright);
                }
                else
                {
                    gradient.SetColor(0, bright);
                    gradient.SetColor(1, dark);
                }
                gradTex.Gradient = gradient;
            }
        }
        else
        {
            if (node is CanvasItem ci)
                ci.SelfModulate = bright;
        }
    }

    private static (Color dark, Color bright) GetClassBackgroundColors(Class c)
    {
        return c switch
        {
            Class.MegaGrow => (new Color("#154c0a"), new Color("#228700")),
            Class.Smarty => (new Color("#4d4d4d"), new Color("#878787")),
            Class.Kabloom => (new Color("#870022"), new Color("#cf0034")),
            Class.Solar => (new Color("#948a01"), new Color("#ffd900")),
            Class.Guardian => (new Color("#332508"), new Color("#613325")),
            Class.Brainy => (new Color("#851487"), new Color("#c74cb6")),
            Class.Crazy => (new Color("#33006e"), new Color("#6504c7")),
            Class.Beastly => (new Color("#173f6e"), new Color("#3277a8")),
            Class.Sneaky => (new Color("#1c1c1c"), new Color("#2e2e2e")),
            Class.Hearty => (new Color("#7a3700"), new Color("#e66700")),
            _ => (new Color("#000000"), new Color("#ffffff")),
        };
    }

    private void SetupPackName(HeroModel hero)
    {
        var nameEdit = GetNodeOrNull<LineEdit>("%名称");
        if (nameEdit == null) return;

        string packName = _diy?.PackName;
        if (string.IsNullOrEmpty(packName))
        {
            string heroName = hero?.Data?.Title ?? "";
            packName = string.IsNullOrEmpty(heroName) ? "#1" : $"{heroName}#1";
            if (_diy != null) _diy.PackName = packName;
        }

        nameEdit.Text = packName;

        if (_diy != null)
        {
            var diy = _diy;
            nameEdit.TextChanged += text =>
            {
                diy.PackName = text;
            };
        }
    }

    private static void SetTexture(Node node, string path)
    {
        if (node == null || string.IsNullOrEmpty(path)) return;
        var tex = path.Load<Texture2D>();
        switch (node)
        {
            case Sprite2D s: s.Texture = tex; break;
            case TextureRect r: r.Texture = tex; break;
            case TextureButton b: b.TextureNormal = tex; break;
        }
    }

    private static (Class c1, Class c2) GetHeroClasses(HeroModel hero)
    {
        var priorityOrder = new[]
        {
            Class.MegaGrow, Class.Solar, Class.Smarty, Class.Guardian, Class.Kabloom,
            Class.Brainy, Class.Crazy, Class.Hearty, Class.Sneaky, Class.Beastly,
        };

        var set = new HashSet<Class>(hero.Data.Category ?? new List<Class>());

        Class c1 = default, c2 = default;
        bool hasC1 = false;

        foreach (var c in priorityOrder)
        {
            if (!set.Contains(c)) continue;
            if (!hasC1) { c1 = c; hasC1 = true; }
            else { c2 = c; break; }
        }

        return (c1, c2);
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
                if (_isAutoBuilding) return;
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
        _ = PlayFlyEffect(fromPos, deckNode.GlobalPosition, template);

        int newAmount = ownedNode.ForcedAmount - 1;
        if (newAmount > 0)
            ownedNode.SetAmount(newAmount);
        else
            ownedNode.QueueFree();

        UpdateDeckCountUI();
        FocusCard(deckNode);
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

        NodeCard ownedNode = FindCardInContainer(_ownedContainer, title);
        if (ownedNode != null)
        {
            ownedNode.SetAmount(ownedNode.ForcedAmount + 1);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _ = PlayFlyEffect(fromPos, ownedNode.GlobalPosition, template);
            FocusCard(ownedNode);
        }
        else if (_ownedContainer != null)
        {
            var newNode = await CreateCard(template, _ownedContainer, 1, true, true, CardView.Deck, allowExpand: true);
            SetupOperateButton(newNode, title, isRemove: false);
            await InsertOrdered(_ownedContainer, newNode, template);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _ = PlayFlyEffect(fromPos, newNode.GlobalPosition, template);
            FocusCard(newNode);
        }

        UpdateDeckCountUI();
    }

    private async Task PlayFlyEffect(Vector2 fromGlobalPos, Vector2 toGlobalPos, CardModel template)
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
        temp.ForceHideAmount = true;
        temp.PreventGray = true;
        temp.Fresh();
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

    private async Task InsertOrdered(Control container, NodeCard node, CardModel template)
    {
        if (container == null) return;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        int target = -1;
        var children = container.GetChildren();

        for (int i = 0; i < children.Count; i++)
        {
            var c = children[i];
            if (c == node) continue;

            var nameStr = c.Name.ToString();
            if (nameStr.Contains("占位") || nameStr.Contains("Placeholder"))
            {
                target = i;
                break;
            }

            if (c is NodeCard cardNode)
            {
                if (cardNode.Model == null)
                {
                    target = i;
                    break;
                }

                if (Compare(template, cardNode.Model) < 0)
                {
                    target = i;
                    break;
                }
            }
        }

        if (target >= 0)
            container.MoveChild(node, target);
    }

    private int Compare(CardModel a, CardModel b)
    {
        int ca = a.Cost.Current, cb = b.Cost.Current;
        if (ca != cb) return ca.CompareTo(cb);
        int ra = _classRank.TryGetValue(a.Class, out var r1) ? r1 : int.MaxValue;
        int rb = _classRank.TryGetValue(b.Class, out var r2) ? r2 : int.MaxValue;
        if (ra != rb) return ra.CompareTo(rb);

        int ia = CardModel.AllCards.IndexOf(a);
        int ib = CardModel.AllCards.IndexOf(b);
        if (ia >= 0 && ib >= 0)
            return ia.CompareTo(ib);

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