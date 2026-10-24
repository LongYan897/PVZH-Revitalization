using Battle.Entity;
using Battle.Texture;
using Card;
using Card.Cmd;
using Controller;
using Godot;
using Pack;
using Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Target;

namespace Battle;

[GlobalClass]
/// <summary>
/// 道路节点数据
/// </summary>
public partial class NodeRoad : Control, ITarget
{
    [Export]
    public int Index { get; private set; }
    private static readonly Dictionary<NodeRoad, Road> Instances = new();
    /// <summary>
    /// 改变目标类型
    /// </summary>
    /// <param name="targetKind">要改变的类型</param>
    public void CallTargetKind(RoadTargetKind targetKind)
    {
        Model.LastTargetKind = targetKind;
    }
    public override void _ExitTree()
    {
        TargetRegistry.Unregister(this);
        Instances.Remove(this);
    }
    /// <summary>
    /// 绑定道路数据
    /// </summary>
    /// <param name="model"></param>
    public void Bind(Road model)
    {
        Model = model;
        TargetRegistry.Register(this);
        Instances.Add(this, Model);
        Position += model.Type switch
        {
            RoadType.Ground => new Vector2(0, 20),
            _ => new()
        };
        _iColli1Scale = model.Type switch
        {
            RoadType.Height => new Vector2(0.33f, 0.354f),
            _ => new(0.33f, 0.325f)
        };
        GetNode<Node2D>("%环境高亮").Position = model.Type switch
        {
            RoadType.Height => new Vector2(0, -36.5f),
            _ => new(0, -24.5f)
        };
        GetNode<Node2D>("%环境高亮").Scale = _iColli1Scale;
        GetNode<Sprite2D>("%图标").Texture = TextureBag.LoadCurrent().ForRoad(Model.Type, Model.Index % 2 == 0);
    }
    public override void _EnterTree()
    {
        GetNode<Area2D>("%碰撞箱").SetMeta("Target", this);
        GetNode<Area2D>("%碰撞箱").SetMeta("TargetType", "Road");
        GetNode<Area2D>("%碰撞箱2").SetMeta("Target", this);
        GetNode<Area2D>("%碰撞箱2").SetMeta("TargetType", "Grid");
        GetNode<Area2D>("%碰撞箱3").SetMeta("Target", this);
        GetNode<Area2D>("%碰撞箱3").SetMeta("TargetType", "CoopGrid");
        GetNode<Node2D>("%环境高亮").Visible = false;
        GetNode<Node2D>("%植物_僵尸1").Visible = false;
        GetNode<Node2D>("%植物_2").Visible = false;
        GetNode<Node2D>("%植物_僵尸2").Visible = false;
        GetNode<Node2D>("%植物_3").Visible = false;
        GetNode<Button>("%碰撞").GuiInput += OnRoadGuiInput1;
        GetNode<Button>("%碰撞2").GuiInput += OnRoadGuiInput2;
        GetNode<Button>("%碰撞3").GuiInput += OnRoadGuiInput3;
    }
    private Vector2 _iColli1Scale;
    public static bool CanTargetedBy(CardModel card)
    {
        return TargetRegistry.AnyCanBeTarget(card, card.TargetType, false, card.TargetFilter);
    }
    /// <summary>
    /// 绑定的道路数据
    /// </summary>
    public Road Model { get; private set; }
    /// <summary>
    /// 最近一次被选中的类型
    /// </summary>
    public RoadTargetKind LastTargetKind => Model.LastTargetKind;
    public Func<CardModel, Func<ITarget, bool>, bool, bool> CanBeTarget =>
    (t, f, from) =>
    {
        bool? a;
        a = f?.Invoke(this);
        bool b = true;
        if (t is FighterCardModel fighter)
            b = Fighter.CanGenerate(fighter, Model);
        if (a != null)
            b = (bool)a && b;
        return b;
    };
    public static NodeRoad GetNode(Road road)
    {
        foreach (var item in Instances)
        {
            if (item.Value == road)
                return item.Key;
        }
        return null;
    }
    public static NodeRoad GetNodeByIndex(int index)
    {
        foreach (var item in Instances)
        {
            if (item.Value.Index == index)
                return item.Key;
        }
        return null;
    }
    public Vector2 GetFighterLocation(Location location)
    {
        return Model.IsUp switch
        {
            false => location switch
            {
                Location.Plant => GetNode<Node2D>("%植物_僵尸1").GetNode<Marker2D>("Pos").GlobalPosition,
                Location.PlantFront => GetNode<Node2D>("%植物_2").GetNode<Marker2D>("Pos").GlobalPosition,
                Location.Zombie => GetNode<Marker2D>("%Pos1").GlobalPosition,
                _ => new(360, 640),
            },
            true => location switch
            {

                Location.Plant => GetNode<Marker2D>("%Pos1").GlobalPosition,
                Location.PlantFront => GetNode<Marker2D>("%Pos2").GlobalPosition,
                Location.Zombie => GetNode<Node2D>("%植物_僵尸1").GetNode<Marker2D>("Pos").GlobalPosition,
                _ => new(360, 640),
            }
        };
    }
    public Vector2 GetHeroLocation(Player player)
    {
        return player == PlayerManager.HostPlayer ? GetNode<Marker2D>("%Hero").GlobalPosition : GetNode<Marker2D>("%Hero2").GlobalPosition;
    }
    public TargetType TargetType => TargetType.Lines | TargetType.Grids | TargetType.CoopGrids;
    private static bool NeedLine(TargetType t) =>
    t.HasFlag(TargetType.Lines);

    private static bool NeedG1(TargetType t) =>
        t.HasFlag(TargetType.Grids)
        || t.HasFlag(TargetType.CoopGrids);

    private static bool NeedG2(TargetType t) =>
        t.HasFlag(TargetType.CoopGrids);

    public void OnCallTargeted(TargetType targetType,CardModel card)
    {
        bool line = NeedLine(targetType);
        bool g1 = NeedG1(targetType);
        bool g2 = (NeedG2(targetType) || (NeedG1(targetType) && Model.GetFighters().Any(p => p.Model.Camp == Camp.Plant && p.Model.CardTag.HasFlag(CardTag.Coop)))) && Model.GetFighters().Any(p => p.Model.Camp == Camp.Plant);
        g2 &= card is FighterCardModel fighter && fighter.Camp.Plant();
        if (!line && !g1) return;

        if (line) CallLineTargeted();
        else if (g2) CallCoopGridTargeted();
        else if (g1) CallGridTargeted();

        SetMonitoring(line, g1, g2);
    }

    public async void OnDeleteTargeted(TargetType targetType)
    {
        if (NeedG1(targetType) || NeedG2(targetType) || NeedLine(targetType))
        {
            await DeleteLineTargeted();
        }
    }

    private Tween _tweenTargeted4;
    private Tween _tweenTargeted3;
    private Tween _tweenTargeted2;
    private Tween _tweenTargeted;
    private ColorBox _colorBox = new ColorBox(new("f4f4d5"), new("20ff07"), new Color(0, 243, 0), new Color(243, 0, 0));
    private ColorBox _colortgBox = new ColorBox(new Color("ffffff"), new Color("37ff00"), new Color("6ef0ff"), default);
    public bool Calling { get; private set; }
    private void KillTween()
    {
        if (_tweenTargeted != null && _tweenTargeted.IsValid())
        {
            _tweenTargeted.Kill();
        }
        if (_tweenTargeted2 != null && _tweenTargeted2.IsValid())
        {
            _tweenTargeted2.Kill();
        }
        if (_tweenTargeted != null && _tweenTargeted.IsValid())
        {
            _tweenTargeted3.Kill();
        }
        if (_tweenTargeted2 != null && _tweenTargeted2.IsValid())
        {
            _tweenTargeted4.Kill();
        }
    }
    private async void CallLineTargeted()
    {
        KillTween();
        Calling = true;
        var line = GetNode<Sprite2D>("%环境高亮");
        GetNode<Sprite2D>("%环境高亮").Visible = false;
        GetNode<TextureRect>("%单位高亮").Visible = false;
        GetNode<TextureRect>("%单位高亮2").Visible = false;

        line.Modulate = _colortgBox.Default;
        line.Visible = true;
        line.Scale = new();

        _tweenTargeted = CreateTween().BindNode(line);
        _tweenTargeted.TweenProperty(line, "scale", _iColli1Scale, 0.15f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted.TweenProperty(line, "scale", _iColli1Scale * 1.1f, 0.05f);
        _tweenTargeted.TweenProperty(line, "scale", _iColli1Scale, 0.05f);
        await ToSignal(_tweenTargeted, Tween.SignalName.Finished);

        _tweenTargeted = CreateTween().BindNode(line).SetLoops(-1);
        _tweenTargeted.TweenProperty(line, "scale", _iColli1Scale * new Vector2(1.01f, 1.01f), 0.7f);
        _tweenTargeted.TweenProperty(line, "scale", _iColli1Scale * new Vector2(0.99f, 0.99f), 0.7f);
    }
    private async void CallGridTargeted()
    {
        KillTween();
        Calling = true;

        var hl = GetNode<TextureRect>("%单位高亮");
        GetNode<Sprite2D>("%环境高亮").Visible = false;
        GetNode<TextureRect>("%单位高亮").Visible = false;
        GetNode<TextureRect>("%单位高亮2").Visible = false;

        hl.Modulate = _colortgBox.Default;
        hl.Visible = true;
        hl.Scale = new();

        _tweenTargeted = CreateTween().BindNode(hl);
        _tweenTargeted.TweenProperty(hl, "scale", Vector2.One, 0.15f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted.TweenProperty(hl, "scale", Vector2.One * 1.1f, 0.05f);
        _tweenTargeted.TweenProperty(hl, "scale", Vector2.One, 0.05f);
        await ToSignal(_tweenTargeted, Tween.SignalName.Finished);

        _tweenTargeted = CreateTween().BindNode(hl).SetLoops(-1);
        _tweenTargeted.TweenProperty(hl, "scale", new Vector2(1f, 1f) * new Vector2(1.02f, 1.02f), 0.7f);
        _tweenTargeted.TweenProperty(hl, "scale", new Vector2(1f, 1f) * new Vector2(0.98f, 0.98f), 0.7f);

        if (!Model.IsUp)
        {
            var f = Model.GetFighters().FirstOrDefault(p => p.Model.Camp == Camp.Plant);
            if (f != null)
                Cannonical = f.Model.Location;
        }
    }
    private async void CallCoopGridTargeted()
    {
        KillTween();
        Calling = true;

        var hl1 = GetNode<TextureRect>("%单位高亮");
        GetNode<Sprite2D>("%环境高亮").Visible = false;
        GetNode<TextureRect>("%单位高亮").Visible = false;
        GetNode<TextureRect>("%单位高亮2").Visible = false;

        hl1.Modulate = _colortgBox.Default;
        hl1.Visible = true;
        hl1.Scale = new();
        _tweenTargeted = CreateTween().BindNode(hl1);
        _tweenTargeted.TweenProperty(hl1, "scale", Vector2.One, 0.15f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted.TweenProperty(hl1, "scale", Vector2.One * 1.1f, 0.05f);
        _tweenTargeted.TweenProperty(hl1, "scale", Vector2.One, 0.05f);


        var hl2 = GetNode<TextureRect>("%单位高亮2");

        hl2.Modulate = _colortgBox.Default;
        hl2.Visible = true;
        hl2.Scale = new();

        _tweenTargeted2 = CreateTween().BindNode(hl2);
        _tweenTargeted2.TweenProperty(hl2, "scale", Vector2.One, 0.15f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted2.TweenProperty(hl2, "scale", Vector2.One * 1.1f, 0.05f);
        _tweenTargeted2.TweenProperty(hl2, "scale", Vector2.One, 0.05f);

        await Task.WhenAll(ToSignal(_tweenTargeted,Tween.SignalName.Finished).ToTask(), ToSignal(_tweenTargeted2, Tween.SignalName.Finished).ToTask());

        _tweenTargeted = CreateTween().BindNode(hl1).SetLoops(-1);
        _tweenTargeted.TweenProperty(hl1, "scale", new Vector2(1f, 1f) * new Vector2(1.02f, 1.02f), 0.7f);
        _tweenTargeted.TweenProperty(hl1, "scale", new Vector2(1f, 1f) * new Vector2(0.98f, 0.98f), 0.7f);

        _tweenTargeted2 = CreateTween().BindNode(hl2).SetLoops(-1);
        _tweenTargeted2.TweenProperty(hl2, "scale", new Vector2(1f, 1f) * new Vector2(1.02f, 1.02f), 0.7f);
        _tweenTargeted2.TweenProperty(hl2, "scale", new Vector2(1f, 1f) * new Vector2(0.98f, 0.98f), 0.7f);

        if (!Model.IsUp)
        {
            var f = Model.GetFighters().First(p => p.Model.Camp == Camp.Plant);
            Cannonical = f.Model.Location;
            _tweenTargeted4 = CreateTween().BindNode(f);
            _tweenTargeted4.TweenProperty(f, "position", GetNode<Node2D>("%Pos").GlobalPosition, 0.2f);
        }
    }
    private Location Cannonical = Location.Plant;
    private async Task DeleteLineTargeted()
    {
        KillTween();
        Calling = false;

        var line = GetNode<Sprite2D>("%环境高亮");
        var hl1 = GetNode<TextureRect>("%单位高亮");
        var hl2 = GetNode<TextureRect>("%单位高亮2");

        line.Scale = _iColli1Scale;
        hl1.Scale = Vector2.One;
        hl2.Scale = Vector2.One;

        _tweenTargeted = CreateTween().BindNode(line);
        _tweenTargeted.TweenProperty(line, "scale", _iColli1Scale * 1.1f, 0.05f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted.TweenProperty(line, "scale", Vector2.Zero, 0.15f)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted.TweenCallback(Callable.From(() =>
        {
            line.Visible = false;
            line.Scale = _iColli1Scale;
            line.Modulate = _colortgBox.Default;
        }));

        _tweenTargeted2 = CreateTween().BindNode(hl1);
        _tweenTargeted2.TweenProperty(hl1, "scale", Vector2.One * 1.1f, 0.05f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted2.TweenProperty(hl1, "scale", Vector2.Zero, 0.15f)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted2.TweenCallback(Callable.From(() =>
        {
            hl1.Visible = false;
            hl1.Scale = Vector2.One;
            hl1.Modulate = _colortgBox.Default;
        }));

        _tweenTargeted3 = CreateTween().BindNode(hl2);
        _tweenTargeted3.TweenProperty(hl2, "scale", Vector2.One * 1.1f, 0.05f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted3.TweenProperty(hl2, "scale", Vector2.Zero, 0.15f)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted3.TweenCallback(Callable.From(() =>
        {
            hl2.Visible = false;
            hl2.Scale = Vector2.One;
            hl2.Modulate = _colortgBox.Default;
        }));

        var bat = GetNode<TextureRect>("%Battle");
        var ar1 = GetNode<TextureRect>("%Arrow");
        var ar2 = GetNode<TextureRect>("%ArrowDown");
        bat.Visible = false;
        ar1.Visible = false;
        ar2.Visible = false;

        if (Model.GetFighters().Count(f => f.Model.Camp == Camp.Plant) == 1)
        {
            if (string.IsNullOrEmpty(lastCall))
            {
                var f = Model.GetFighters().FirstOrDefault(f => f.Model.Camp == Camp.Plant);
                _tweenTargeted4 = CreateTween().BindNode(f);
                _tweenTargeted4.TweenProperty(f, "position", GetFighterLocation(Cannonical), 0.2f);
                lastCall = "";
            }
            else
            {
                var f = Model.GetFighters().FirstOrDefault(f => f.Model.Camp == Camp.Plant);
                if (lastCall == "Grids")
                {
                    f.Model.Location = Location.PlantFront;
                    f.SwitchLayer(f.Model.Location);
                }
                else if (lastCall == "CoopGrids")
                {
                    f.Model.Location = Location.Plant;
                    f.SwitchLayer(f.Model.Location);
                }
            }
        }

        await Task.WhenAll(ToSignal(_tweenTargeted,Tween.SignalName.Finished).ToTask(), ToSignal(_tweenTargeted2, Tween.SignalName.Finished).ToTask(), ToSignal(_tweenTargeted3, Tween.SignalName.Finished).ToTask());

        SetMonitoring(false, false, false);
    }

    public void OnTargeted(TargetContext ctx)
    {
        if (!Calling) return;
        KillTween();

        if (ctx.Line)
        {
            var hl = GetNode<Sprite2D>("%环境高亮");
            _tweenTargeted = CreateTween().BindNode(hl);
            _tweenTargeted2 = CreateTween().BindNode(hl);
            hl.Visible = true;
            _tweenTargeted.TweenProperty(hl, "scale", _iColli1Scale, 0.2f);
            _tweenTargeted2.TweenProperty(hl, "modulate", _colortgBox.ColorA, 0.2f);
            Model.LastTargetKind = RoadTargetKind.Line;
        }

        if (ctx.Grid)
        {
            var hl = GetNode<TextureRect>("%单位高亮");
            _tweenTargeted = CreateTween().BindNode(hl);
            _tweenTargeted2 = CreateTween().BindNode(hl);
            hl.Visible = true;
            _tweenTargeted.TweenProperty(hl, "scale", new Vector2(1f, 1f), 0.2f);
            _tweenTargeted2.TweenProperty(hl, "modulate", _colortgBox.ColorA, 0.2f);
            Model.LastTargetKind = RoadTargetKind.Grid;
            var h2 = GetNode<TextureRect>("%单位高亮2");
            _tweenTargeted3 = CreateTween().BindNode(h2);
            h2.Visible = true;
            _tweenTargeted3.TweenProperty(h2, "scale", new Vector2(1f, 1f) * new Vector2(1.02f, 1.02f), 0.7f);
            _tweenTargeted3.TweenProperty(h2, "scale", new Vector2(1f, 1f) * new Vector2(0.98f, 0.98f), 0.7f);
            _tweenTargeted3.TweenProperty(h2, "modulate", _colortgBox.Default, 0.2f);
            if (Model.GetFighters().Count(p => p.Model.Camp == Camp.Plant) == 1 && ctx.FighterCard.Camp == Camp.Plant)
            {
                var f = Model.GetFighters().First(p => p.Model.Camp == Camp.Plant);
                _tweenTargeted4 = CreateTween().BindNode(f);
                _tweenTargeted4.TweenProperty(f, "position", GetFighterLocation(Location.PlantFront), 0.2f);
                lastCall = "Grids";
            }
            TargetedGridArrow(ctx.FighterCard, false);
        }

        if (ctx.CoopGrid)
        {
            var hl = GetNode<TextureRect>("%单位高亮2");
            _tweenTargeted = CreateTween().BindNode(hl);
            _tweenTargeted2 = CreateTween().BindNode(hl);
            hl.Visible = true;
            _tweenTargeted.TweenProperty(hl, "scale", new Vector2(1f, 1f), 0.2f);
            _tweenTargeted2.TweenProperty(hl, "modulate", _colortgBox.ColorA, 0.2f);
            Model.LastTargetKind = RoadTargetKind.CoopGrid;
            var h2 = GetNode<TextureRect>("%单位高亮");
            _tweenTargeted3 = CreateTween().BindNode(hl);
            h2.Visible = true;
            _tweenTargeted3.TweenProperty(h2, "scale", new Vector2(1f, 1f) * new Vector2(1.02f, 1.02f), 0.7f);
            _tweenTargeted3.TweenProperty(h2, "scale", new Vector2(1f, 1f) * new Vector2(0.98f, 0.98f), 0.7f);
            _tweenTargeted3.TweenProperty(h2, "modulate", _colortgBox.Default, 0.2f);

            if (Model.GetFighters().Count(p => p.Model.Camp == Camp.Plant) == 1 && ctx.FighterCard.Camp == Camp.Plant)
            {
                var f = Model.GetFighters().First(p => p.Model.Camp == Camp.Plant);
                _tweenTargeted4 = CreateTween().BindNode(f);
                _tweenTargeted4.TweenProperty(f, "position", GetFighterLocation(Location.Plant), 0.2f);
                lastCall = "CoopGrids";
            }
            TargetedGridArrow(ctx.FighterCard, true);
        }
    }
    private string lastCall;
    private void TargetedGridArrow(FighterCardModel fighterCard, bool isCoop)
    {
        var bat = GetNode<TextureRect>("%Battle");
        var ar1 = GetNode<TextureRect>("%Arrow");
        var ar2 = GetNode<TextureRect>("%ArrowDown");
        var boZomb = Model.GetFighters().Any(f => f.Model.Camp == Camp.Zombie || fighterCard.Camp == Camp.Zombie);
        var boPlant = Model.GetFighters().Any(f => f.Model.Camp == Camp.Plant || fighterCard.Camp == Camp.Plant);
        if (boZomb && boPlant)
        {
            bat.Visible = true;
            ar1.Visible = true;
            ar2.Visible = true;
            Tween tween = CreateTween().BindNode(bat);
            bat.Scale = Vector2.Zero;

            tween.TweenProperty(bat, "scale", new Vector2(0.5f, 0.5f), 0.05)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

            tween.TweenProperty(bat, "scale", new Vector2(1f, 1f), 0.1)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

            tween.TweenProperty(bat, "scale", new Vector2(0.6f, 0.6f), 0.15)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

            Tween tween1 = CreateTween().BindNode(ar2);
            Tween tween2 = CreateTween().BindNode(ar1);
            if (isCoop)
            {
                ar1.Position = new(-42.0f, -57.0f);
                ar1.Size = new(84.0f, 51.0f);
            }
            else
            {
                ar1.Position = new(-42.0f, -41.0f);
                ar1.Size = new(84, 199.0f);
            }

            ar2.Scale = new(0, 0);
            tween1.TweenProperty(ar2, "scale", new Vector2(1f, 1f), 0.06);
            ar1.Scale = new(0, 0);
            tween2.TweenProperty(ar1, "scale", new Vector2(1f, 1f), 0.06);
        }
        else
        {
            ar1.Visible = true;
            Tween tween2 = CreateTween().BindNode(ar1);
            if (isCoop)
            {
                ar1.Position = new(-42.0f, -57.0f);
                ar1.Size = new(84.0f, 51.0f);
            }
            else
            {
                ar1.Position = new(-42.0f, -38.0f);
                ar1.Size = new(84, 199.0f);
            }
            ar1.Scale = new(0, 0);
            tween2.TweenProperty(ar1, "scale", new Vector2(1f, 1.3f), 0.02)
                .SetTrans(Tween.TransitionType.Quad);

            tween2.TweenProperty(ar1, "scale", new Vector2(1f, 1.2f), 0.02)
                .SetTrans(Tween.TransitionType.Quad);

            tween2.TweenProperty(ar1, "scale", new Vector2(1f, isCoop ? 8.0f : 2.8f), 0.02)
                .SetTrans(Tween.TransitionType.Quad);
        }
    }
    public async void OnDistargeted(TargetContext ctx)
    {
        KillTween();

        var bat = GetNode<TextureRect>("%Battle");
        var ar1 = GetNode<TextureRect>("%Arrow");
        var ar2 = GetNode<TextureRect>("%ArrowDown");

        bat.Visible = false;
        ar1.Visible = false;
        ar2.Visible = false;
        lastCall = "";
        if (ctx.Line)
            await ReturnPulse("%环境高亮", _iColli1Scale);
        if (ctx.Grid)
            await ReturnPulse("%单位高亮", Vector2.One);
        if (ctx.CoopGrid)
            await ReturnPulse("%单位高亮2", Vector2.One);

    }

    private async Task ReturnPulse(string path, Vector2 baseScale)
    {
        Node node = null;
        if (path == "%环境高亮")
        {
            node = GetNode<Sprite2D>(path);
        }
        else
        {
            node = GetNode<TextureRect>(path);
        }
        _tweenTargeted = CreateTween().BindNode(node);
        _tweenTargeted2 = CreateTween().BindNode(node);
        _tweenTargeted.TweenProperty(node, "scale", baseScale, 0.2f);
        _tweenTargeted2.TweenProperty(node, "modulate", _colortgBox.Default, 0.2f);
        await ToSignal(_tweenTargeted, Tween.SignalName.Finished);

        if (!Calling) return;

        _tweenTargeted = CreateTween().BindNode(node).SetLoops(-1);
        _tweenTargeted.TweenProperty(node, "scale", baseScale * new Vector2(1.01f, 1.01f), 0.7f);
        _tweenTargeted.TweenProperty(node, "scale", baseScale * new Vector2(0.99f, 0.99f), 0.7f);

        Model.LastTargetKind = RoadTargetKind.None;
    }
    private bool _isRoadOn = true;
    private bool _isGridOn1 = false;
    private bool _isGridOn2 = false;
    private void OnRoadGuiInput1(InputEvent @event)
    {
        if (!Calling) return;
        if (@event is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left || !mb.Pressed) return;
        if (!_isRoadOn) return;

        Model.LastTargetKind = RoadTargetKind.Line;
        CardCmd.SelectTarget(this);
    }

    private void OnRoadGuiInput2(InputEvent @event)
    {
        if (!Calling) return;
        if (@event is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left || !mb.Pressed) return;
        if (!_isGridOn1) return;

        Model.LastTargetKind = RoadTargetKind.Grid;
        CardCmd.SelectTarget(this);
    }

    private void OnRoadGuiInput3(InputEvent @event)
    {
        if (!Calling) return;
        if (@event is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left || !mb.Pressed) return;
        if (!_isGridOn2) return;

        Model.LastTargetKind = RoadTargetKind.CoopGrid;
        CardCmd.SelectTarget(this);
    }
    public void SetMonitoring(bool roadOn, bool gridOn1, bool gridOn2)
    {
        _isRoadOn = roadOn;
        _isGridOn1 = gridOn1;
        _isGridOn2 = gridOn2;
        GetNode<Area2D>("%碰撞箱").Monitoring = roadOn;
        GetNode<Area2D>("%碰撞箱").Monitorable = roadOn;
        GetNode<Node2D>("%环境高亮").Visible = roadOn;
        GetNode<Area2D>("%碰撞箱2").Monitoring = gridOn1;
        GetNode<Area2D>("%碰撞箱2").Monitorable = gridOn1;
        GetNode<Node2D>("%植物_僵尸1").Visible = gridOn1;
        GetNode<Area2D>("%碰撞箱3").Monitoring = gridOn2;
        GetNode<Area2D>("%碰撞箱3").Monitorable = gridOn2;
        GetNode<Node2D>("%植物_2").Visible = gridOn2;
        var box1 = GetNode<Area2D>("%碰撞箱");
        box1.Monitoring = roadOn;
        box1.Monitorable = roadOn;

        var box2 = GetNode<Area2D>("%碰撞箱2");
        box2.Monitoring = gridOn1;
        box2.Monitorable = gridOn1;

        var box3 = GetNode<Area2D>("%碰撞箱3");
        box3.Monitoring = gridOn2;
        box3.Monitorable = gridOn2;
    }
    //专门用于远程同步或者Ai的区域
    /// <summary>
    /// 把目标格上的原单位换位。
    /// </summary>
    public void SwapForGrid()
    {
        if (Model.GetFighters().Count(p => p.Model.Camp == Camp.Plant) == 1)
        {
            var tween = CreateTween();
            var f = Model.GetFighters().First(p => p.Model.Camp == Camp.Plant);
            tween.TweenProperty(f,"position", GetFighterLocation(Location.PlantFront),0.3f);
            f.Model.Location = Location.PlantFront;
            f.SwitchLayer(f.Model.Location);
        }
    }

    /// <summary>
    /// 把目标格上的原单位换位。
    /// </summary>
    public void SwapForCoopGrid()
    {
        var tween = CreateTween();
        var f = Model.GetFighters().First(p => p.Model.Camp == Camp.Plant);
        tween.TweenProperty(f, "position", GetFighterLocation(Location.Plant), 0.3f);
        f.Model.Location = Location.Plant;
        f.SwitchLayer(f.Model.Location);
    }
    //专门用于远程同步或者Ai的区域
    /// <summary>
    /// 仅视觉：开关 Auto 路径对应的父级节点（不碰 Area2D、不碰 MouseFilter）
    /// </summary>
    private void SetAutoVisual(bool line, bool g1, bool g2)
    {
        GetNode<Node2D>("%环境高亮").Visible = line;
        GetNode<Node2D>("%植物_僵尸2").Visible = g1;
        GetNode<Node2D>("%植物_3").Visible = g2;
    }

    public void AutoTargeted(TargetContext ctx)
    {
        KillTween();

        bool line = ctx.Line;
        bool g1 = ctx.Grid || ctx.CoopGrid;
        bool g2 = ctx.CoopGrid;

        if (!line && !g1) return;

        if (line) AutoCallLineTargeted();
        else if (g2) AutoCallCoopGridTargeted();
        else if (g1) AutoCallGridTargeted();

        SetAutoVisual(line, g1, g2);
    }

    public void AutoDistargeted(TargetContext ctx)
    {
        KillTween();
        _ = AutoDeleteTargeted();
    }

    private async Task AutoDeleteTargeted()
    {
        KillTween();

        var line = GetNode<Sprite2D>("%环境高亮");
        var hl3 = GetNode<TextureRect>("%单位高亮3");
        var hl4 = GetNode<TextureRect>("%单位高亮4");

        line.Scale = _iColli1Scale;
        hl3.Scale = Vector2.One;
        hl4.Scale = Vector2.One;

        _tweenTargeted = CreateTween().BindNode(line);
        _tweenTargeted.TweenProperty(line, "scale", _iColli1Scale * 1.1f, 0.05f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted.TweenProperty(line, "scale", Vector2.Zero, 0.15f)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted.TweenCallback(Callable.From(() =>
        {
            line.Visible = false;
            line.Scale = _iColli1Scale;
            line.Modulate = _colortgBox.Default;
        }));

        _tweenTargeted2 = CreateTween().BindNode(hl3);
        _tweenTargeted2.TweenProperty(hl3, "scale", Vector2.One * 1.1f, 0.05f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted2.TweenProperty(hl3, "scale", Vector2.Zero, 0.15f)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted2.TweenCallback(Callable.From(() =>
        {
            hl3.Visible = false;
            hl3.Scale = Vector2.One;
            hl3.Modulate = _colortgBox.Default;
        }));

        _tweenTargeted3 = CreateTween().BindNode(hl4);
        _tweenTargeted3.TweenProperty(hl4, "scale", Vector2.One * 1.1f, 0.05f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted3.TweenProperty(hl4, "scale", Vector2.Zero, 0.15f)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted3.TweenCallback(Callable.From(() =>
        {
            hl4.Visible = false;
            hl4.Scale = Vector2.One;
            hl4.Modulate = _colortgBox.Default;
        }));

        var bat = GetNode<TextureRect>("%Battle");
        var ar1 = GetNode<TextureRect>("%Arrow");
        var ar2 = GetNode<TextureRect>("%ArrowDown");
        bat.Visible = false;
        ar1.Visible = false;
        ar2.Visible = false;

        await Task.WhenAll(
            ToSignal(_tweenTargeted, Tween.SignalName.Finished).ToTask(),
            ToSignal(_tweenTargeted2, Tween.SignalName.Finished).ToTask(),
            ToSignal(_tweenTargeted3, Tween.SignalName.Finished).ToTask());

        SetAutoVisual(false, false, false);
    }

    private void AutoCallLineTargeted()
    {
        var line = GetNode<Sprite2D>("%环境高亮");
        GetNode<TextureRect>("%单位高亮3").Visible = false;
        GetNode<TextureRect>("%单位高亮4").Visible = false;

        line.Modulate = _colortgBox.Default;
        line.Visible = true;
        line.Scale = Vector2.Zero;

        var tween = CreateTween().BindNode(line);
        tween.TweenProperty(line, "scale", _iColli1Scale, 0.15f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(line, "scale", _iColli1Scale * 1.1f, 0.05f);
        tween.TweenProperty(line, "scale", _iColli1Scale, 0.05f);
        tween.TweenProperty(line, "modulate", _colortgBox.ColorB, 0.1f);
    }

    private void AutoCallGridTargeted()
    {
        var hl = GetNode<TextureRect>("%单位高亮3");
        GetNode<Sprite2D>("%环境高亮").Visible = false;
        GetNode<TextureRect>("%单位高亮4").Visible = false;

        hl.Modulate = _colortgBox.Default;
        hl.Visible = true;
        hl.Scale = Vector2.Zero;

        var tween = CreateTween().BindNode(hl);
        tween.TweenProperty(hl, "scale", Vector2.One, 0.15f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(hl, "scale", Vector2.One * 1.1f, 0.05f);
        tween.TweenProperty(hl, "scale", Vector2.One, 0.05f);
        tween.TweenProperty(hl, "modulate", _colortgBox.ColorB, 0.1f);
    }

    private void AutoCallCoopGridTargeted()
    {
        var hl2 = GetNode<TextureRect>("%单位高亮4");
        GetNode<Sprite2D>("%环境高亮").Visible = false;
        GetNode<TextureRect>("%单位高亮3").Visible = false;
        hl2.Modulate = _colortgBox.Default;
        hl2.Visible = true;
        hl2.Scale = Vector2.Zero;

        var tween2 = CreateTween().BindNode(hl2);
        tween2.TweenProperty(hl2, "scale", Vector2.One, 0.15f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        tween2.TweenProperty(hl2, "scale", Vector2.One * 1.1f, 0.05f);
        tween2.TweenProperty(hl2, "scale", Vector2.One, 0.05f);
        tween2.TweenProperty(hl2, "modulate", _colortgBox.ColorB, 0.1f);
    }
}