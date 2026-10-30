using Battle;
using Card;
using Card.Cmd;
using Controller;
using Godot;
using Hero.Model;
using Pack;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Target;

namespace Hero;

[GlobalClass]
public partial class HeroInstance : Control, ITarget, IAttackable
{
    private HeroInstance() { }
    public TargetType TargetType => TargetType.Hero;
    public HeroModel Hero { get; private set; }
    public SpineHandler spineHandler;
    public static void Clear()
    {
        foreach (var p in Instances)
        {
            if (p.Key != null && GodotObject.IsInstanceValid(p.Key))
                p.Key.QueueFree();
        }
    }
    private static readonly Dictionary<HeroInstance, HeroModel> Instances = new();
    public override void _Ready()
    {
        GetNode<Area2D>("碰撞箱").SetMeta("Target", this);
        GetNode<Area2D>("碰撞箱").SetMeta("TargetType", "Hero");
        GetNode<Button>("碰撞").MouseFilter = Control.MouseFilterEnum.Ignore;
        spineHandler = GetNode<SpineHandler>("动画");
        var hitBtn = GetNode<Button>("碰撞");
        hitBtn.GuiInput += OnHitButtonGuiInput;
        TargetRegistry.Register(this);
    }
    private bool _isOpen = false;
    private void OnHitButtonGuiInput(InputEvent @event)
    {
        if (!_isOpen) return;
        if (@event is InputEventMouseButton mb
            && mb.ButtonIndex == MouseButton.Left
            && mb.Pressed)
        {
            OnHeroClick();
        }
    }
    private void OnHeroClick()
    {
        if (Calling)
            CardCmd.SelectTarget(this);
    }
    public Func<CardModel, Func<ITarget, bool>, bool, bool> CanBeTarget => (t, f, from) =>
    {
        bool camp = Hero.Camp switch
        {
            Camp.Plant => t.Camp == Camp.Zombie,
            Camp.Zombie => t.Camp == Camp.Plant,
            _ => false
        };
        return camp;
    };

    public async Task PlayAnimation(string name, bool returnToIdle, int track = 0)
    {
        await spineHandler.SetAnimationTask(track, name, false);
        if (returnToIdle)
            spineHandler.SetAnimation(track, "idle", true);
    }
    public static HeroInstance GetNode(HeroModel hero)
    {
        return Instances.FirstOrDefault(p => p.Value == hero).Key;
    }
    public bool Calling { get; private set; }

    private Tween _tweenTargeted2;
    private Tween _tweenTargeted;
    private void KillTween()
    {
        if (_tweenTargeted != null && _tweenTargeted.IsValid())
            _tweenTargeted.Kill();
        if (_tweenTargeted2 != null && _tweenTargeted2.IsValid())
            _tweenTargeted2.Kill();
    }
    private ColorBox _colortgBox = new ColorBox(new Color("ffffff"), new Color("37ff00"), default, default);
    public void OnCallTargeted(TargetType type, CardModel card, Camp? camp)
    {
        if (!type.HasFlag(TargetType.Hero)) return;
        CallTargeted();
        GetNode<Button>("碰撞").MouseFilter = Control.MouseFilterEnum.Stop;
    }
    private async void CallTargeted()
    {
        KillTween();
        Calling = true;
        var sp = GetNode<Node2D>("ExSprite");
        sp.Modulate = _colortgBox.Default;
        sp.Visible = true;
        sp.Scale = new();

        _tweenTargeted = CreateTween().BindNode(sp);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1f, 1f), 0.15f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1.1f, 1.1f), 0.05f);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1f, 1f), 0.05f);
        await ToSignal(_tweenTargeted, Tween.SignalName.Finished);

        _tweenTargeted = CreateTween().BindNode(sp).SetLoops(-1);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1.02f, 1.02f), 0.7f);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(0.98f, 0.98f), 0.7f);
    }
    public void OnDeleteTargeted(TargetType type, Camp? camp)
    {
        if (!type.HasFlag(TargetType.Hero)) return;
        DeleteTargeted();
        GetNode<Button>("碰撞").MouseFilter = Control.MouseFilterEnum.Ignore;
    }
    private void DeleteTargeted()
    {
        KillTween();
        Calling = false;
        var sp = GetNode<Node2D>("ExSprite");
        sp.Modulate = _colortgBox.Default;
        sp.Scale = 2.4f * new Vector2(1f, 1f);
        _tweenTargeted = CreateTween().BindNode(sp);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1.1f, 1.1f), 0.05f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted.TweenProperty(sp, "scale", Vector2.Zero, 0.15f)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted.TweenCallback(Callable.From(() =>
        {
            sp.Visible = false;
            sp.Scale = 2.4f * new Vector2(1f, 1f);
            sp.Modulate = _colortgBox.Default;
        }));
    }
    public async void OnDistargeted(TargetContext ctx)
    {
        KillTween();
        var sp = GetNode<Node2D>("ExSprite");
        _tweenTargeted = CreateTween().BindNode(sp);
        _tweenTargeted2 = CreateTween().BindNode(sp);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1f, 1f), 0.2f);
        _tweenTargeted2.TweenProperty(sp, "modulate", _colortgBox.Default, 0.2f);
        await ToSignal(_tweenTargeted, Tween.SignalName.Finished);
        if (!Calling) return;
        KillTween();
        _tweenTargeted = CreateTween().BindNode(sp).SetLoops(-1);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1.02f, 1.02f), 0.7f);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(0.98f, 0.98f), 0.7f);
    }

    public void OnTargeted(TargetContext ctx)
    {
        if (!Calling) return;
        KillTween();
        var sp = GetNode<Node2D>("ExSprite");
        _tweenTargeted = CreateTween().BindNode(sp);
        _tweenTargeted2 = CreateTween().BindNode(sp);
        sp.Visible = true;
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1.2f, 1.2f), 0.2f);
        _tweenTargeted2.TweenProperty(sp, "modulate", _colortgBox.ColorA, 0.2f);
    }
    public override void _ExitTree()
    {
        Instances.Remove(this);
        TargetRegistry.Unregister(this);
    }
    /// <summary>
    /// 创建一个英雄节点
    /// </summary>
    /// <param name="heroModel">英雄</param>
    /// <returns></returns>
    public static async Task<HeroInstance> Create(HeroModel heroModel, Vector2 vector2, bool isGuest)
    {
        var instance = GD.Load<PackedScene>("res://场景(C#)/英雄单位.tscn").Instantiate<HeroInstance>();
        var node = isGuest ? Main.GuestHeroContainer : Main.HeroContainer;
        node.AddChild(instance);
        instance.Hero = heroModel;
        instance.Scale = new(0.3f, 0.3f);
        Instances.Add(instance, heroModel);
        instance.Position = vector2;
        instance.Fresh();
        return instance;
    }
    private bool isFirst = true;
    private async void Fresh()
    {
        if (Hero == null) return;
        if (isFirst)
        {
            _isOpen = true;
            isFirst = false;
            spineHandler.LoadSkeletonData(Hero.AnimPath);
            await spineHandler.SetAnimationTask(0, "intro", false);
            Visible = true;
            SetLoop();
        }
    }
    private static readonly string[] LoopAnimations = { "special1", "special2", "wink" };

    private async void SetLoop()
    {
        Random random = new();

        var valid = LoopAnimations.Where(a => spineHandler.HasAnimation(a)).ToArray();
        if (valid.Length == 0)
        {
            await PlayAnimation("idle", true);
            SetLoop();
            return;
        }

        var str = valid[random.Next(valid.Length)];

        await PlayAnimation(str, true);

        for (int i = 0; i < random.Next(3, 6); i++)
            await PlayAnimation("idle", true);

        SetLoop();
    }
    //专门用于远程同步或者Ai的区域
    public void AutoTargeted(TargetContext ctx)
    {
        KillTween();

        var sp = GetNode<Node2D>("ExSprite");
        sp.Visible = true;
        sp.Scale = Vector2.Zero;
        sp.Modulate = _colortgBox.Default;

        _tweenTargeted = CreateTween().BindNode(sp);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1.1f, 1.1f), 0.1f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1f, 1f), 0.08f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);

        _tweenTargeted2 = CreateTween().BindNode(sp);
        _tweenTargeted2.TweenProperty(sp, "modulate", _colortgBox.ColorB, 0.1f);
    }

    public void AutoDistargeted(TargetContext ctx)
    {
        KillTween();

        var sp = GetNode<Node2D>("ExSprite");

        _tweenTargeted = CreateTween().BindNode(sp);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1.1f, 1.1f), 0.05f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted.TweenProperty(sp, "scale", Vector2.Zero, 0.15f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        _tweenTargeted.TweenCallback(Callable.From(() =>
        {
            sp.Visible = false;
            sp.Scale = 2.4f * new Vector2(1f, 1f);
            sp.Modulate = _colortgBox.Default;
        }));
    }
}