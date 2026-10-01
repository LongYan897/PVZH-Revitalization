using Battle.Entity;
using Card;
using Godot;
using Logger;
using Pack;
using System;
using System.Collections.Generic;
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
    public Road Model { get; private set; } = new() { Index =1, Type = RoadType.Ground };
    /// <summary>
    /// 绑定道路数据
    /// </summary>
    /// <param name="model"></param>
    public void Bind(Road model)
    {
        Model = model;
    }
    public Func<CardModel,Func<ITarget, bool>, bool> CanBeTarget =>
    (t,f) =>
    {
        bool? a;
        a = f?.Invoke(this);
        bool b = true;
        if (t is FighterCardModel fighter)
            b = Fighter.CanGenerate(fighter,Model);
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
            _ => new(360,640),
        };
    }
    public TargetType TargetType => TargetType.Lines | TargetType.FighterAndGrids | TargetType.Grids ;

    public static void CallRoadLinesTargeted(CardModel cardModel, TargetType targetType)
    {
        if (targetType.HasFlag(TargetType.Lines))
        {
            var List = new List<NodeRoad>();
            foreach (var road in Instances.Keys)
            {
                var bol = road.CanBeTarget(cardModel,cardModel.TargetFilter);
                if (bol)
                    List.Add(road);
            }
            foreach (var road in List)
            {
                road.CallLineTargeted();
                road.SetMonitoring(true);
            }
        }
    }
    public static void DeleteRoadLinesTargeted(TargetType targetType)
    {
        if (targetType.HasFlag(TargetType.Lines))
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
    private void DeleteLineTargeted()
    {
        KillTween();
        var line = GetNode<Sprite2D>("%环境高亮");
        line.Visible = false;
        Calling = false;
    }
    public void Targeted()
    {
        if (!Calling) return;
        KillTween();
        var line = GetNode<Sprite2D>("%环境高亮");
        _tweenTargeted = CreateTween().BindNode(line);
        _tweenTargeted2 = CreateTween().BindNode(line);
        line.Visible = true;
        _tweenTargeted.TweenProperty(line, "scale", new Vector2(0.33f, 0.378f) * new Vector2(1, 1f), 0.2f);
        _tweenTargeted2.TweenProperty(line, "modulate", _colortgBox.ColorA, 0.2f);
    }
    public async void Distargeted()
    {
        KillTween(); 
        var line = GetNode<Sprite2D>("%环境高亮");
        _tweenTargeted = CreateTween().BindNode(line);
        _tweenTargeted2 = CreateTween().BindNode(line);
        _tweenTargeted.TweenProperty(line, "scale", new Vector2(0.33f, 0.378f) * new Vector2(1f, 1f), 0.2f);
        _tweenTargeted2.TweenProperty(line, "modulate", _colortgBox.Default, 0.2f);
        await ToSignal(_tweenTargeted, Tween.SignalName.Finished);
        KillTween();
        _tweenTargeted = CreateTween().BindNode(line).SetLoops(-1);
        _tweenTargeted.TweenProperty(line, "scale", new Vector2(0.33f, 0.378f) * new Vector2(1.02f, 1.02f), 0.7f);
        _tweenTargeted.TweenProperty(line, "scale", new Vector2(0.33f, 0.378f) * new Vector2(0.98f, 0.98f), 0.7f);
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
    }
    public void SetMonitoring(bool roadOn)
    {
        GetNode<Area2D>("%碰撞箱").Monitoring = roadOn;
        GetNode<Area2D>("%碰撞箱").Monitorable = roadOn;
        GetNode<Node2D>("%环境高亮").Visible = roadOn;
        GetNode<Area2D>("%碰撞箱2").Monitoring = !roadOn;
        GetNode<Area2D>("%碰撞箱2").Monitorable = !roadOn;
        GetNode<Node2D>("%植物_僵尸1").Visible = !roadOn;
        GetNode<Area2D>("%碰撞箱3").Monitoring = !roadOn;
        GetNode<Area2D>("%碰撞箱3").Monitorable = !roadOn;
        GetNode<Node2D>("%植物_2").Visible = !roadOn;
    }
}
