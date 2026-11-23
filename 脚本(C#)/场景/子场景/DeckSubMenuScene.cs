using Card;
using Godot;
using Hero.Model;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class DeckSubMenuScene : SubMenuScene
{
    public static DeckSubMenuScene Instance { get; private set; }

    private const float SearchBoxHeight = 250f;
    private const float SearchCollapsedWidth = 255f;
    private const float SearchExpandDeltaX = 255f;
    private const float SearchOpenMoveX = -81.6f;
    private const int HeroesPerRow = 8;

    private Sprite2D BackGround;
    private Sprite2D Top1;
    private Sprite2D Top2;
    private TextureButton Plant;
    private TextureButton Zombie;
    private Control Container;
    private ColorRect HeroLabel;
    private ColorRect Cards;
    private HFlowContainer CardInformation;
    private NButton Search;
    private ColorRect SearchRect;
    private Label Amount;
    private PackedScene classScene;
    private List<Node> CurrentInformation = new List<Node>();
    private Dictionary<Class, List<CardModel>> CurrentClassCards = new Dictionary<Class, List<CardModel>>();
    private List<Control> CurrentClasses = new List<Control>();
    private Camp CurrentCamp = Camp.Plant;
    private List<NodeCard> AllNodeCards = new List<NodeCard>();
    private bool IsShowed = false;
    private List<Color> ThemeColors1 => new List<Color> {
        new Color(0.411f, 0.69f, 0.38f, 1.0f),
        new Color(0.535f, 0.9f, 0.495f, 1.0f),
        new Color(0.274f, 0.46f, 0.253f, 1.0f),
    };
    private List<Color> ThemeColors2 => new List<Color> {
        new Color(0.365f, 0.192f, 0.439f, 1.0f),
        new Color(0.657f, 0.348f, 0.79f, 1.0f),
        new Color(0.25f, 0.132f, 0.3f, 1.0f),
    };
    public override void _WhenEnterTree()
    {
        Instance = this;
    }

    public override async Task _Load()
    {
        BackGround = FindChild("背景图") as Sprite2D;
        Top1 = FindChild("顶部遮挡") as Sprite2D;
        Top2 = FindChild("顶部遮挡1") as Sprite2D;
        Plant = FindChild("植物按钮") as TextureButton;
        Zombie = FindChild("僵尸按钮") as TextureButton;
        Container = GetNode<HFlowContainer>("%容器");
        HeroLabel = GetNode<ColorRect>("%英雄标志");
        Cards = GetNode<ColorRect>("%卡牌标志");
        CardInformation = GetNode<HFlowContainer>("%卡牌列表");
        Search = GetNode<NButton>("%搜索");
        SearchRect = GetNode<ColorRect>("%搜索框");
        Amount = GetNode<Label>("%数量");
        classScene = ResourceLoader.Load<PackedScene>("res://场景(C#)/派系条.tscn");
        Plant.Pressed += OnPlantPressed;
        Zombie.Pressed += OnZombiePressed;
        Search.Pressed += OnSearchPressed;
        Search.GetNode<NButton>("取消").Pressed += OnCancelPressed;
        BackGround.Modulate = ThemeColors1[0];
        Top1.Modulate = ThemeColors1[1];
        Top2.Modulate = ThemeColors1[2];
        CurrentCamp = Camp.Plant;
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
        if (AllNodeCards.Count > 0)
        {
            foreach (var node in AllNodeCards)
            {
                if (GodotObject.IsInstanceValid(node))
                {
                    node.JustFresh();
                }
            }
            return;
        }

        if (CurrentCamp == Camp.Plant)
        {
            BackGround.Modulate = ThemeColors1[0];
            Top1.Modulate = ThemeColors1[1];
            Top2.Modulate = ThemeColors1[2];
        }
        else
        {
            BackGround.Modulate = ThemeColors2[0];
            Top1.Modulate = ThemeColors2[1];
            Top2.Modulate = ThemeColors2[2];
        }
        await InitializeInformation(CurrentCamp.ToName());
    }

    private void OnCancelPressed()
    {
        ShowCards(c => true);
        Button cancelFrame = Search.GetNode<Button>("取消框");
        NButton cancel = Search.GetNode<NButton>("取消");
        Tween t1 = cancelFrame.CreateTween();
        Tween t2 = Search.CreateTween();
        t1.TweenProperty(cancelFrame, "size:x", -SearchExpandDeltaX, 0.5).AsRelative();
        t2.TweenProperty(Search, "position:x", -SearchOpenMoveX, 0.5).AsRelative();
        cancel.Visible = false;
        cancel.Position = Vector2.Zero;
        IsShowed = false;
    }

    private async void OnSearchPressed()
    {
        SearchRect.Visible = true;
        LineEdit lineEdit = SearchRect.GetNode<LineEdit>("LineEdit");
        lineEdit.GrabFocus();
        await lineEdit.ToSignal(lineEdit, LineEdit.SignalName.TextSubmitted);
        string text = lineEdit.Text;
        SearchRect.Visible = false;
        lineEdit.Text = "";
        if (!string.IsNullOrEmpty(text))
        {
            SearchCards(text);
            if (IsShowed) return;
            Button cancelFrame = Search.GetNode<Button>("取消框");
            NButton cancel = Search.GetNode<NButton>("取消");
            Tween t1 = cancelFrame.CreateTween();
            Tween t2 = Search.CreateTween();
            t1.TweenProperty(cancelFrame, "size:x", SearchExpandDeltaX, 0.5).AsRelative();
            t2.TweenProperty(Search, "position:x", SearchOpenMoveX, 0.5).AsRelative();
            await t1.ToSignal(t1, Tween.SignalName.Finished);
            cancel.Visible = true;
            cancel.Position = new Vector2(cancelFrame.Size.X * 0.5f, 0);
            IsShowed = true;
        }
        else
        {
            ShowCards(c => true);
        }
    }

    private async void OnPlantPressed()
    {
        CurrentCamp = Camp.Plant;
        BackGround.Modulate = ThemeColors1[0];
        Top1.Modulate = ThemeColors1[1];
        Top2.Modulate = ThemeColors1[2];
        await InitializeInformation(CurrentCamp.ToName());
    }

    private async void OnZombiePressed()
    {
        CurrentCamp = Camp.Zombie;
        BackGround.Modulate = ThemeColors2[0];
        Top1.Modulate = ThemeColors2[1];
        Top2.Modulate = ThemeColors2[2];
        await InitializeInformation(CurrentCamp.ToName());
    }

    private void CreateSpirtes(string camp)
    {
        if (CurrentInformation.Count > 0)
        {
            foreach (Node node in CurrentInformation)
            {
                node.QueueFree();
            }
        }
        CurrentInformation = new List<Node>();
        List<Type> types = HeroModel.SeekHeroModel(camp);
        HFlowContainer slot = Container.GetNode<HFlowContainer>("标志栏");
        foreach (Type type in types)
        {
            HeroSprite hs = HeroSprite.Create(type.Name);
            slot.AddChild(hs);
            CurrentInformation.Add(hs);
        }
        int remainder = types.Count % HeroesPerRow;
        if (remainder != 0)
        {
            PackedScene placeholderScene = ResourceLoader.Load<PackedScene>(
                camp == Camp.Plant.ToName()
                    ? "res://场景(C#)/植物英雄图标.tscn"
                    : "res://场景(C#)/僵尸英雄图标.tscn");
            int fillCount = HeroesPerRow - remainder;
            for (int i = 0; i < fillCount; i++)
            {
                Control hs = placeholderScene.Instantiate<Control>();
                CurrentInformation.Add(hs);
                slot.AddChild(hs);
            }
        }
    }

    private async Task CreateCurrentCards()
    {
        int count = 0;
        foreach (Class c in CurrentClassCards.Keys) count += CurrentClassCards[c].Count;
        Class currentClass;
        Control classSprite;
        for (int n = 0; n < CurrentClassCards.Keys.Count; n++)
        {
            currentClass = CurrentClassCards.Keys.ToList()[n];
            classSprite = CreateClassSprite(currentClass);
            CurrentClasses.Add(classSprite);
        }
        for (int n = 0; n < CurrentClassCards.Keys.Count; n++)
        {
            currentClass = CurrentClassCards.Keys.ToList()[n];
            classSprite = CurrentClasses[n];
            foreach (CardModel card in CurrentClassCards[currentClass])
            {
                CardModel cardClone = card;
                cardClone.Status = Status.FaceUp;
                NodeCard Ncard = await NodeCard.DisplayCardAt(cardClone, Vector2.Zero, classSprite.GetNode<HFlowContainer>("排列"), CardView.Collection, 1);
                AllNodeCards.Add(Ncard);
            }
        }
    }

    private async Task InitializeInformation(string camp)
    {
        CreateSpirtes(camp);
        if (camp == Camp.Plant.ToName())
        {
            Plant.GetNode<TextureRect>("高亮").Visible = true;
            Zombie.GetNode<TextureRect>("高亮").Visible = false;
        }
        else
        {
            Plant.GetNode<TextureRect>("高亮").Visible = false;
            Zombie.GetNode<TextureRect>("高亮").Visible = true;
        }
        foreach (Control control in CurrentClasses) control.QueueFree();
        CurrentClasses = new List<Control>();
        CurrentClassCards = new Dictionary<Class, List<CardModel>>();
        AllNodeCards = new List<NodeCard>();
        List<Class> classes = sortClassFromCamp(camp);
        SortCards(cm => cm.Camp.ToName() == camp && cm.Rarity != Rarity.Token, classes);
        await CreateCurrentCards();
        Amount.Text = AllNodeCards.Count.ToString();
        SelectDeck.CurrentCamp = CurrentCamp;
    }

    private Control CreateClassSprite(Class c)
    {
        Control classI = classScene.Instantiate<MinimumHFlowContainer>();
        classI.GetNode<TextureRect>("底板/图标").Texture = ResourceLoader.Load<Texture2D>(c.ToPath());
        classI.GetNode<Label>("底板/文本").Text = c.ToName();
        GetNode<HFlowContainer>("%卡牌列表").AddChild(classI);
        return classI;
    }

    private void ShowCards(Func<CardModel, bool> predicate)
    {
        SortCards(predicate, CurrentClassCards.Keys.ToList());
        foreach (NodeCard ncard in AllNodeCards)
        {
            if (!predicate(ncard.Model)) ncard.Visible = false;
            else ncard.Visible = true;
            GD.Print(predicate(ncard.Model), "->", ncard.Model.Title);
        }
    }

    private void SearchCards(string keywords)
    {
        List<string> keyWords = keywords.Split(" ").ToList();
        Dictionary<Func<CardModel, bool>, string> predicates = new Dictionary<Func<CardModel, bool>, string>();
        List<string> classNames = new List<string>();
        foreach (Class c in Enum.GetValues<Class>()) classNames.Add(c.ToName());
        foreach (string keyword in keyWords)
        {
            string word = keyword;
            string predicateMod = "";
            if (word[0] == '+')
            {
                predicateMod = "+";
                word = word.Substring(1);
            }
            else if (word[0] == '-')
            {
                predicateMod = "-";
                word = word.Substring(1);
            }
            Func<CardModel, bool> p = (c) => c.GetAllStrings().Any(s => s.Contains(word));
            predicates[p] = predicateMod;
        }
        Func<CardModel, bool> predicate = (c) =>
        {
            bool result = true;
            foreach (Func<CardModel, bool> p in predicates.Keys)
            {
                if (predicates[p] == "") result = result && p(c);
                else if (predicates[p] == "-") result = result && !p(c);
                else if (predicates[p] == "+") result = result || p(c);
            }
            return result;
        };
        ShowCards(predicate);
    }

    private List<Class> sortClassFromCamp(string camp)
    {
        return camp switch
        {
            "植物" => [
                Class.MegaGrow,
                Class.Guardian,
                Class.Kabloom,
                Class.Smarty,
                Class.Solar
            ],
            "僵尸" => [
                Class.Sneaky,
                Class.Hearty,
                Class.Crazy,
                Class.Brainy,
                Class.Beastly,
            ],
            _ => []
        };
    }

    private void SortCards(Func<CardModel, bool> predicate, List<Class> classes)
    {
        Vector2I costRange = new Vector2I(0, -1);
        foreach (CardModel card in CardModel.AllCards.Where(predicate))
        {
            if (costRange[0] > costRange[1]) costRange = new Vector2I(card.Cost.Current, card.Cost.Current);
            else
            {
                if (costRange[0] > card.Cost.Current) costRange[0] = card.Cost.Current;
                if (costRange[1] < card.Cost.Current) costRange[1] = card.Cost.Current;
            }
        }
        foreach (Class c in classes)
        {
            CurrentClassCards[c] = new List<CardModel>();
            for (int cost = costRange[0]; cost <= costRange[1]; cost++)
            {
                List<CardModel> cards = CardModel.AllCards.Where(cm => cm.Cost.Current == cost && predicate(cm) && cm.Class == c).ToList();
                CurrentClassCards[c].AddRange(cards);
            }
        }
    }
}