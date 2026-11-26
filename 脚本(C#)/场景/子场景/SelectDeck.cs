using Card;
using Card.Pack;
using Godot;
using Hero.Model;
using Hero.String;
using Logger;
using Pack;
using Play;
using Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class SelectDeck : SubMenuScene
{
    public static Camp CurrentCamp = Camp.Plant;
    public static SelectDeck Instance { get; private set; }

    private Camp DeckCamp = CurrentCamp;
    private PackedScene BallScene;
    private Dictionary<string, NButton> Balls = new Dictionary<string, NButton>();
    private MinimumHFlowContainer BallContainer;
    private MinimumHFlowContainer DeckContainer;
    private Panel BallPanel;
    private Dictionary<string, HeroFlow> FlowNodes = new Dictionary<string, HeroFlow>();

    private InputNineRect _trashBox;
    private bool _isTrashActive = false;
    private float _trashBoxOriginalX = 0f;

    public override void _WhenEnterTree()
    {
        Instance = this;
    }

    public override async Task _Load()
    {
        BallScene = "res://场景(C#)/英雄跳转球.tscn".Load<PackedScene>();
        BallContainer = GetNode<MinimumHFlowContainer>("主容器/英雄图标背景板/滚动框/英雄图标框");
        DeckContainer = GetNode<MinimumHFlowContainer>("%卡组框");
        BallPanel = GetNode<Panel>("主容器/英雄图标背景板");

        _trashBox = GetNodeOrNull<InputNineRect>("%TrashBox");
        if (_trashBox != null)
        {
            _trashBox.Input += OnTrashBoxInput;
            _trashBox.Texture = "res://素材(C#)/其他/trashbutton.png".Load<Texture2D>();
            _trashBoxOriginalX = _trashBox.Position.X;
        }

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

    private void OnTrashBoxInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
        {
            _isTrashActive = !_isTrashActive;

            if (_trashBox != null && GodotObject.IsInstanceValid(_trashBox))
            {
                float targetRightX = _trashBoxOriginalX + 112f;

                var tween = _trashBox.CreateTween();

                tween.TweenProperty(_trashBox, "scale", new Vector2(0.9f, 0.9f), 0.1f)
                     .SetTrans(Tween.TransitionType.Quad)
                     .SetEase(Tween.EaseType.Out);

                tween.TweenProperty(_trashBox, "position:x", targetRightX, 0.15f)
                     .SetTrans(Tween.TransitionType.Quad)
                     .SetEase(Tween.EaseType.Out);

                tween.TweenCallback(Callable.From(() => {
                    if (GodotObject.IsInstanceValid(_trashBox))
                    {
                        _trashBox.Texture = GD.Load<Texture2D>(_isTrashActive ? "res://素材(C#)/其他/trashbutton_on.png" : "res://素材(C#)/其他/trashbutton.png");

                        if (_isTrashActive)
                        {
                            foreach (var flow in FlowNodes.Values)
                            {
                                foreach (var child in flow.GetChildren().OfType<CardDeck>())
                                {
                                    child.SetDeleteMode(true);
                                }
                            }
                        }
                        else
                        {
                            DeletePendingDecks();
                        }
                    }
                }));

                tween.TweenProperty(_trashBox, "position:x", _trashBoxOriginalX, 0.15f)
                     .SetTrans(Tween.TransitionType.Quad)
                     .SetEase(Tween.EaseType.In);

                tween.TweenProperty(_trashBox, "scale", Vector2.One, 0.1f)
                     .SetTrans(Tween.TransitionType.Quad)
                     .SetEase(Tween.EaseType.Out);
            }
        }
    }

    private void DeletePendingDecks()
    {
        var asset = PlayerManager.HostPlayer?.Asset;
        if (asset != null)
        {
            var list = (List<PresetPack>)asset.Presets;
            var toRemove = new List<PresetPack>();

            foreach (var flow in FlowNodes.Values)
            {
                foreach (var child in flow.GetChildren().OfType<CardDeck>().ToList())
                {
                    if (child.IsPendingDelete)
                    {
                        toRemove.Add(child.Deck);
                        child.SetDeleteMode(false);

                        child.PivotOffset = child.Size / 2f;
                        var tween = child.CreateTween();
                        tween.TweenProperty(child, "scale", Vector2.Zero, 0.2f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.In);
                        tween.TweenProperty(child, "modulate:a", 0.0f, 0.2f);
                        tween.TweenCallback(Callable.From(() => {
                            if (GodotObject.IsInstanceValid(child))
                            {
                                flow.RemoveChild(child);
                                child.QueueFree();
                            }
                        }));
                    }
                    else
                    {
                        child.SetDeleteMode(false);
                        if (child.IsPendingDelete)
                        {
                            child.SetPendingDelete(false);
                        }
                    }
                }
            }

            foreach (var preset in toRemove)
            {
                list.Remove(preset);
            }
        }
    }

    public async Task Refresh()
    {
        List<Type> heroes = HeroModel.SeekHeroModel(DeckCamp.ToName());

        if (Balls.Count == 0)
        {
            foreach (var child in BallContainer.GetChildren())
            {
                child.QueueFree();
            }
            Balls.Clear();
            await CreateBalls();
        }

        var heroNames = heroes.Select(h => h.Name).ToHashSet();

        foreach (var kvp in FlowNodes.ToList())
        {
            if (!heroNames.Contains(kvp.Key) || !GodotObject.IsInstanceValid(kvp.Value))
            {
                if (GodotObject.IsInstanceValid(kvp.Value)) kvp.Value.QueueFree();
                FlowNodes.Remove(kvp.Key);
            }
        }

        for (int i = 0; i < heroes.Count; i++)
        {
            var heroType = heroes[i];
            var hero = HeroModel.Identity(heroType.Name);

            if (!FlowNodes.TryGetValue(heroType.Name, out var flow) || !GodotObject.IsInstanceValid(flow))
            {
                flow = HeroFlow.Flow(hero);
                DeckContainer.AddChild(flow);
                FlowNodes[heroType.Name] = flow;

                flow.AddChild(CreateDeck.Instance(hero));
            }
            flow.RefreshDecks();
            DeckContainer.MoveChild(flow, i);
        }

        if (DeckCamp == Camp.Plant) BallPanel.SelfModulate = new Color(0.411f, 0.69f, 0.38f, 1.0f);
        else BallPanel.SelfModulate = new Color(0.365f, 0.192f, 0.439f, 1.0f);
    }

    private async Task CreateBalls()
    {
        List<Type> heroes = HeroModel.SeekHeroModel(DeckCamp.ToName());
        int x = 1;
        foreach (Type hero in heroes)
        {
            await CreateBall(hero.Name);
            x++;
            if (x > 6) x = 1;
        }
        if (x > 1 && x <= 6)
        {
            for (int i = x; i <= 6; i++)
            {
                await CreateBall(null, true);
            }
        }
    }

    private async Task CreateBall(string heroName, bool disabled = false)
    {
        Texture2D texture;
        if (heroName != null) texture = GD.Load<Texture2D>(HeroString.GetData(heroName, "Icon").AsString());
        else
        {
            if (DeckCamp == Camp.Plant) texture = "res://素材(C#)/英雄图标/cornerimage_plants_create.png".Load<Texture2D>();
            else texture = "res://素材(C#)/英雄图标/cornerimage_zombies_create.png".Load<Texture2D>();
            Image image = texture.GetImage();
            image.Resize(227, 227);
            texture = ImageTexture.CreateFromImage(image);
        }
        NButton ball = BallScene.Instantiate<NButton>();
        ball.TextureNormal = texture;
        ball.Disabled = disabled;
        if (heroName != null) Balls.Add(heroName, ball);
        BallContainer.AddChild(ball);
    }
}