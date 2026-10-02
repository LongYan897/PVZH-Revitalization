using Battle.Entity;
using Card;
using Card.Cmd;
using Godot;
using Logger;
using Pack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Target;

namespace Battle;

[GlobalClass]
/// <summary>
/// 道路节点数据
/// </summary>
public partial class NodeRoad : Control, ITarget
{
    private static Dictionary<NodeRoad, Road> Instances = new();
    /// <summary>
    /// 绑定的道路数据
    /// </summary>
    public Road Model { get; private set; } = new() { Index = 1, Type = RoadType.Ground };
    /// <summary>
    /// 最近一次被选中的类型
    /// </summary>
    public enum RoadTargetKind { None, Line, Grid, CoopGrid }

    /// <summary>
    /// 最近一次被选中的类型
    /// </summary>
    public RoadTargetKind LastTargetKind { get; private set; } = RoadTargetKind.None;
    /// <summary>
    /// 绑定道路数据
    /// </summary>
    /// <param name="model"></param>
    public void Bind(Road model)
    {
        Model = model;
    }
    public Func<CardModel, Func<ITarget, bool>, bool> CanBeTarget =>
    (t, f) =>
    {
        bool? a;
        a = f?.Invoke(this);
        bool b = true;
        if (t is FighterCardModel fighter)
            b = Fighter.CanGenerate(fighter, Model);
        return (bool)a && b;
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
    public Vector2 GetFighterLocation(Location location)
    {
        return location switch
        {
            Location.Plant => GetNode<Node2D>("%植物_僵尸1").GetNode<Marker2D>("Pos").GlobalPosition,
            Location.PlantFront => GetNode<Node2D>("%植物2").GetNode<Marker2D>("Pos").GlobalPosition,
            Location.Zombie => GetNode<Marker2D>("%Pos1").GlobalPosition,
            _ => new(360, 640),
        };
    }
    public TargetType TargetType => TargetType.Lines | TargetType.Grids | TargetType.CoopGrids;
    private static bool NeedLine(TargetType t) =>
    t.HasFlag(TargetType.Lines);

    private static bool NeedG1(TargetType t) =>
        t.HasFlag(TargetType.Grids)
        || t.HasFlag(TargetType.CoopGrids);

    private static bool NeedG2(TargetType t) =>
        t.HasFlag(TargetType.CoopGrids);
    public static void CallTargeted(CardModel cardModel, TargetType targetType)
    {
        CallTargeted(cardModel, targetType, cardModel.TargetFilter);
    }

    public static void CallTargeted(CardModel cardModel, TargetType targetType, Func<ITarget, bool> filter)
    {
        bool line = NeedLine(targetType);
        bool g1 = NeedG1(targetType);
        bool g2 = NeedG2(targetType);
        if (!line && !g1) return;

        var list = new List<NodeRoad>();
        foreach (var road in Instances.Keys)
        {
            if (road.CanBeTarget(cardModel, filter))
                list.Add(road);
        }

        foreach (var road in list)
        {
            if (line) road.CallLineTargeted();
            else if (g2) road.CallCoopGridTargeted();
            else if (g1) road.CallGridTargeted();

            road.SetMonitoring(line, g1, g2);
        }
    }
    public static void DeleteTargeted(TargetType targetType)
    {
        if (NeedG1(targetType) || NeedG2(targetType) || NeedLine(targetType))
        {
            foreach (var road in Instances.Keys)
            {
                road.DeleteLineTargeted();
            }
        }
    }
    private Tween _tweenTargeted2;
    private Tween _tweenTargeted;
    private ColorBox _colorBox = new ColorBox(new("f4f4d5"), new("20ff07"), new Color(0, 243, 0), new Color(243, 0, 0));
    private ColorBox _colortgBox = new ColorBox(new Color("ffffff"), new Color("37ff00"), default, default);
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
    }
    private void CallLineTargeted()
    {
        KillTween();
        Calling = true;
        var line = GetNode<Sprite2D>("%环境高亮");
        _tweenTargeted = CreateTween().BindNode(line).SetLoops(-1);
        line.Modulate = _colortgBox.Default;
        line.Visible = true;
        _tweenTargeted.TweenProperty(line, "scale",new Vector2(0.33f, 0.378f ) * new Vector2(1.02f, 1.02f), 0.7f);
        _tweenTargeted.TweenProperty(line, "scale", new Vector2(0.33f, 0.378f) * new Vector2(0.98f, 0.98f), 0.7f);
    }
    private void CallGridTargeted()
    {
        KillTween();
        Calling = true;

        var hl = GetNode<TextureRect>("%单位高亮");
        _tweenTargeted = CreateTween().BindNode(hl).SetLoops(-1);
        hl.Modulate = _colortgBox.Default;
        hl.Visible = true;
        _tweenTargeted.TweenProperty(hl, "scale", new Vector2(1f, 1f) * new Vector2(1.02f, 1.02f), 0.7f);
        _tweenTargeted.TweenProperty(hl, "scale", new Vector2(1f, 1f) * new Vector2(0.98f, 0.98f), 0.7f);
    }

    private void CallCoopGridTargeted()
    {
        KillTween();
        Calling = true;

        var hl1 = GetNode<TextureRect>("%单位高亮");
        _tweenTargeted = CreateTween().BindNode(hl1).SetLoops(-1);
        hl1.Modulate = _colortgBox.Default;
        hl1.Visible = true;
        _tweenTargeted.TweenProperty(hl1, "scale", new Vector2(1f, 1f) * new Vector2(1.02f, 1.02f), 0.7f);
        _tweenTargeted.TweenProperty(hl1, "scale", new Vector2(1f, 1f) * new Vector2(0.98f, 0.98f), 0.7f);

        var hl2 = GetNode<TextureRect>("%单位高亮2");
        _tweenTargeted2 = CreateTween().BindNode(hl2).SetLoops(-1);
        hl2.Modulate = _colortgBox.Default;
        hl2.Visible = true;
        _tweenTargeted2.TweenProperty(hl2, "scale", new Vector2(1f, 1f) * new Vector2(1.02f, 1.02f), 0.7f);
        _tweenTargeted2.TweenProperty(hl2, "scale", new Vector2(1f, 1f) * new Vector2(0.98f, 0.98f), 0.7f);
    }

    private void DeleteLineTargeted()
    {
        KillTween();
        Calling = false;

        GetNode<Sprite2D>("%环境高亮").Visible = false;
        GetNode<TextureRect>("%单位高亮").Visible = false;
        GetNode<TextureRect>("%单位高亮2").Visible = false;
    }

    public void Targeted(bool line, bool g1, bool g2)
    {
        if (!Calling) return;
        KillTween();

        if (line)
        {
            var hl = GetNode<Sprite2D>("%环境高亮");
            _tweenTargeted = CreateTween().BindNode(hl);
            _tweenTargeted2 = CreateTween().BindNode(hl);
            hl.Visible = true;
            _tweenTargeted.TweenProperty(hl, "scale", new Vector2(0.33f, 0.378f), 0.2f);
            _tweenTargeted2.TweenProperty(hl, "modulate", _colortgBox.ColorA, 0.2f);
            LastTargetKind = RoadTargetKind.Line;
        }

        if (g1)
        {
            var hl = GetNode<TextureRect>("%单位高亮");
            _tweenTargeted = CreateTween().BindNode(hl);
            _tweenTargeted2 = CreateTween().BindNode(hl);
            hl.Visible = true;
            _tweenTargeted.TweenProperty(hl, "scale", new Vector2(1f, 1f), 0.2f);
            _tweenTargeted2.TweenProperty(hl, "modulate", _colortgBox.ColorA, 0.2f);
            LastTargetKind = RoadTargetKind.Grid;
        }

        if (g2)
        {
            var hl = GetNode<TextureRect>("%单位高亮2");
            _tweenTargeted = CreateTween().BindNode(hl);
            _tweenTargeted2 = CreateTween().BindNode(hl);
            hl.Visible = true;
            _tweenTargeted.TweenProperty(hl, "scale", new Vector2(1f, 1f), 0.2f);
            _tweenTargeted2.TweenProperty(hl, "modulate", _colortgBox.ColorA, 0.2f);
            LastTargetKind = RoadTargetKind.CoopGrid;
        }
    }
    public async void Distargeted(bool line, bool g1, bool g2)
    {
        KillTween();

        if (line)
            await ReturnPulse("%环境高亮", new Vector2(0.33f, 0.378f));
        if (g1)
            await ReturnPulse("%单位高亮", Vector2.One);
        if (g2)
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
        _tweenTargeted.TweenProperty(node, "scale", baseScale * new Vector2(1.02f, 1.02f), 0.7f);
        _tweenTargeted.TweenProperty(node, "scale", baseScale * new Vector2(0.98f, 0.98f), 0.7f);

        LastTargetKind = RoadTargetKind.None;
    }

    public override void _Ready()
    {
        GetNode<Area2D>("%碰撞箱").SetMeta("Target", this);
        GetNode<Area2D>("%碰撞箱").SetMeta("TargetType", "Road");
        GetNode<Area2D>("%碰撞箱2").SetMeta("Target", this);
        GetNode<Area2D>("%碰撞箱2").SetMeta("TargetType", "Grid");
        GetNode<Area2D>("%碰撞箱3").SetMeta("Target", this);
        GetNode<Area2D>("%碰撞箱3").SetMeta("TargetType", "Grid");
        Instances.Add(this, Model);
        GetNode<Node2D>("%环境高亮").Visible = false;
        GetNode<Node2D>("%植物_僵尸1").Visible = false;
        GetNode<Node2D>("%植物_2").Visible = false;
        GetNode<Button>("%碰撞").GuiInput += OnRoadGuiInput1;
        GetNode<Button>("%碰撞2").GuiInput += OnRoadGuiInput2;
        GetNode<Button>("%碰撞3").GuiInput += OnRoadGuiInput3;
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

        LastTargetKind = RoadTargetKind.Line;
        CardCmd.SelectTarget(this);
    }

    private void OnRoadGuiInput2(InputEvent @event)
    {
        if (!Calling) return;
        if (@event is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left || !mb.Pressed) return;
        if (!_isGridOn1) return;

        LastTargetKind = RoadTargetKind.Grid;
        CardCmd.SelectTarget(this);
    }

    private void OnRoadGuiInput3(InputEvent @event)
    {
        if (!Calling) return;
        if (@event is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left || !mb.Pressed) return;
        if (!_isGridOn2) return;

        LastTargetKind = RoadTargetKind.CoopGrid;
        CardCmd.SelectTarget(this);
    }
    public void SetMonitoring(bool roadOn,bool gridOn1,bool gridOn2)
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
}
