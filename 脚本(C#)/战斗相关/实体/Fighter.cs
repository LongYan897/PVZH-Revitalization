using Card;
using Card.Cmd;
using Controller;
using Godot;
using Pack;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Target;
using static Godot.OpenXRCompositionLayer;

namespace Battle.Entity;

[GlobalClass]
/// <summary>
/// 战斗单位的实体
/// </summary>
public partial class Fighter : Control, ITarget, IAttackable
{
    private static readonly PackedScene Scene = GD.Load<PackedScene>("res://场景(C#)/战斗单位.tscn");
    private static readonly Dictionary<Fighter, FighterCardModel> Instances = new();
    private const int maxInstance = 20;

    public FighterCardModel Model { get; private set; }
    private Road Road;
    private Location index;

    public bool Zombie => Model.Camp == Camp.Zombie;
    public bool Plant1 => Model.Camp == Camp.Plant && index == Location.PlantFront;
    public bool Plant2 => Model.Camp == Camp.Plant && index == Location.Plant;

    public Func<CardModel, Func<ITarget, bool>, bool> CanBeTarget =>
        (t, f) =>
        {
            bool? a;
            a = f?.Invoke(this);
            bool b;
            b = Model.CanbeTarget(t);
            if (a != null)
                b = (bool)a && b;
            bool camp = Model.Camp switch
            {
                Camp.Plant => t.Camp == Camp.Zombie,
                Camp.Zombie => t.Camp == Camp.Plant,
                _ => false
            };
            if (!t.FriendTarget)
                b = b && camp;
            return b;
        };

    /// <summary>
    /// 死亡表现
    /// </summary>
    public async Task Die()
    {
        _isOpen = false;
        Road.RemoveFighter(this);

        GetNode<SpineHandler>("%伤害动画").SetAnimation(0, "die", false);
        GetNode<SpineHandler>("%血量动画").SetAnimation(0, "die", false);
        GetNode<Label>("%伤害数值").Visible = false;
        GetNode<Label>("%血量数值").Visible = false;

        await GetNode<SpineHandler>("%动画").SetAnimationTask(0, "die");

        if (Model.Camp == Camp.Plant)
        {
            Tween tween = CreateTween();
            tween.TweenProperty(GetNode<SpineHandler>("%动画"), "modulate", new Color(1, 1, 1, 0), 0.3f);
            await ToSignal(tween, Tween.SignalName.Finished);
        }

        Visible = false;
        Clear();

        await AnimaActor.PlayInstantAnimationTask("res://数据资源/属性动画/死亡特效.tres", GlobalPosition + new Vector2(0, -30), "intro");
    }

    private Tween _hurtTween;

    private void KillHurtTween()
    {
        if (_hurtTween != null && _hurtTween.IsValid())
            _hurtTween.Kill();
    }

    /// <summary>
    /// 受伤表现（只播动画和伤害飘字，不做逻辑）
    /// </summary>
    public async Task Hurt(int amount)
    {
        KillHurtTween();

        GetNode<Node2D>("%额外动画").Modulate = new Color(1, 1, 1, 1);
        Tween tween = CreateTween();
        tween.TweenProperty(GetNode<Node2D>("%额外动画"), "position", new Vector2(0, -300), 0.3f);
        tween.TweenProperty(GetNode<Node2D>("%额外动画"), "modulate", new Color(1, 1, 1, 0), 0.3f);
        GetNode<Label>("%额外血量数值").Text = $"-{amount}";
        GetNode<Node2D>("%额外动画").Visible = true;

        GetNode<SpineHandler>("%伤害动画").SetAnimation(0, "hurt", false);
        GetNode<SpineHandler>("%血量动画").SetAnimation(0, "hurt", false);
        await GetNode<SpineHandler>("%动画").SetAnimationTask(0, "hurt");

        GetNode<Node2D>("%额外动画").Visible = false;
        GetNode<Node2D>("%额外动画").Position = new Vector2();
    }

    public TargetType TargetType => TargetType.Fighters;

    private ColorBox _colortgBox = new ColorBox(new Color("ffffff"), new Color("37ff00"), default, default);

    public static void CallTargeted(CardModel cardModel, TargetType targetType)
    {
        CallTargeted(cardModel, targetType, cardModel.TargetFilter);
    }

    public static void CallTargeted(CardModel cardModel, TargetType targetType, Func<ITarget, bool> filter)
    {
        if (!targetType.HasFlag(TargetType.Fighters)) return;

        var list = new List<Fighter>();
        foreach (var fighter in Instances.Keys)
        {
            if (fighter.CanBeTarget(cardModel, filter))
                list.Add(fighter);
        }

        foreach (var fighter in list)
            fighter.CallTargeted();
    }

    public static void DeleteTargeted(TargetType targetType)
    {
        if (targetType.HasFlag(TargetType.Fighters))
        {
            foreach (var fighter in Instances.Keys)
                fighter.DeleteTargeted();
        }
    }

    /// <summary>
    /// 播放攻击动画（表现层）
    /// </summary>
    /// <param name="target">目标</param>
    /// <param name="isInstantAmmo">是否是立刻到达目标的子弹</param>
    public async Task Battle(List<IAttackable> target,bool isInstantAmmo)
    {
        await PlayAnimation("attack", true);
        if (Model.AmmoPath == null) return;
        foreach (var attackable in target)
        {
            if (attackable is Fighter fighter)
            {
                if (isInstantAmmo)
                {
                    if (GD.Load(Model.AmmoPath) is Texture2D texture)
                    {
                        CreateAmmon(texture, fighter.GlobalPosition);
                    }
                    else
                        AnimaActor.PlayInstantAnimation(Model.AmmoPath, fighter.GlobalPosition,"");
                }
                else
                {
                    if (GD.Load(Model.AmmoPath) is Texture2D texture)
                    {
                        CreateAmmon(texture, fighter.GlobalPosition);
                    }
                    else
                        AnimaActor.PlayInstantAnimation(Model.AmmoPath, fighter.Position, "");
                }
            }
        }
    }
    private async void CreateAmmon(Texture2D texture,Vector2 vector2)
    {
        var textureR = new TextureRect
        {
            Texture = texture,
            Position = GlobalPosition,
        };
        Tween tween = CreateTween();
        tween.TweenProperty(textureR, "position", vector2, 0.5f);
        Main.Animator.AddChild(textureR);
        await ToSignal(tween, Tween.SignalName.Finished);
        textureR.QueueFree();
    }

    public async Task BeforeAtk(AtkStack atkStack)
    {
        await Model.FireTiming(Timing.BeforeAttack, atkStack);
    }

    public async Task AfterAtk(AtkStack atkStack)
    {
        await Model.FireTiming(Timing.AfterAttack, atkStack);
    }

    public static Fighter Generate(FighterCardModel model, Road road, Location index)
    {
        if (Instances.Count < maxInstance)
        {
            var fighter = Scene.Instantiate<Fighter>();
            fighter.Position = NodeRoad.GetNode(road).GetFighterLocation(index);
            fighter.Model = model;
            fighter.index = index;
            Main.FighterContainer.AddChild(fighter);
            fighter.isFirstPlace = true;
            fighter.Road = road;
            road.AddFighter(fighter);
            fighter.Fresh();
            Instances.Add(fighter, model);
            return fighter;
        }
        else
        {
            var fighter = Instances.First(p => p.Key.Model == null).Key;
            fighter.Position = NodeRoad.GetNode(road).GetFighterLocation(index);
            fighter.isFirstPlace = true;
            fighter.Model = model;
            fighter.index = index;
            fighter.Road = road;
            road.AddFighter(fighter);
            fighter.Fresh();
            Instances[fighter] = model;
            return fighter;
        }
    }

    public static bool CanGenerate(FighterCardModel model, Road road)
    {
        var capa = model.CardTag;
        var roadFighters = road.GetFighters();
        var roadType = road.Type;

        if (model.Camp == Camp.Zombie && (roadType != RoadType.Water || (roadType == RoadType.Water && capa.HasFlag(CardTag.Amphibious))))
        {
            if (roadFighters.Any(f => f.Model.Camp == Camp.Zombie))
                return false;
            return true;
        }
        else if (model.Camp == Camp.Plant && (roadType != RoadType.Water || (roadType == RoadType.Water && capa.HasFlag(CardTag.Amphibious))))
        {
            if (capa.HasFlag(CardTag.Coop))
            {
                if (roadFighters.Count(f => f.Model.Camp == Camp.Plant) > 1)
                    return false;
                return true;
            }
            else
            {
                if (roadFighters.Any(f => f.Model.Camp == Camp.Plant))
                    return false;
                return true;
            }
        }
        return false;
    }

    public FighterCardModel ReturnCard()
    {
        var fighterCard = Model;
        Road.RemoveFighter(this);
        Clear();
        Model = null;
        return fighterCard;
    }

    public async Task PlayAnimation(string name, bool returnToIdle, int track = 0)
    {
        await GetNode<SpineHandler>("%动画").SetAnimationTask(track, name, false);
        if (returnToIdle)
            GetNode<SpineHandler>("%动画").SetAnimation(track, "idle", true);
    }

    public static Fighter GetNode(FighterCardModel fighterCard)
    {
        return Instances.FirstOrDefault(p => p.Value == fighterCard).Key;
    }

    private ColorBox _colorBox = new ColorBox(new("f4f4d5"), new("20ff07"), new Color(0, 243, 0), new Color(243, 0, 0));
    public bool Calling { get; private set; }

    private void CallTargeted()
    {
        KillTween();
        Calling = true;
        var sp = GetNode<Node2D>("ExSprite");
        _tweenTargeted = CreateTween().BindNode(sp).SetLoops(-1);
        sp.Modulate = _colortgBox.Default;
        sp.Visible = true;
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1.02f, 1.02f), 0.7f);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(0.98f, 0.98f), 0.7f);
    }

    private void KillTween()
    {
        if (_tweenTargeted != null && _tweenTargeted.IsValid())
            _tweenTargeted.Kill();
        if (_tweenTargeted2 != null && _tweenTargeted2.IsValid())
            _tweenTargeted2.Kill();
    }

    public override void _Ready()
    {
        GetNode<Area2D>("碰撞箱").SetMeta("Target", this);
        GetNode<Area2D>("碰撞箱").SetMeta("TargetType", "Fighter");
        var hitBtn = GetNode<Button>("碰撞");
        hitBtn.GuiInput += OnHitButtonGuiInput;
    }

    private bool _isOpen = false;

    private void OnHitButtonGuiInput(InputEvent @event)
    {
        if (!_isOpen) return;
        if (@event is InputEventMouseButton mb
            && mb.ButtonIndex == MouseButton.Left
            && mb.Pressed)
        {
            OnFighterClick();
        }
    }

    private void OnFighterClick()
    {
        if (!Calling)
            CardDes.DisplayDescription(Model);
        else
            CardCmd.SelectTarget(this);
    }

    public void Targeted()
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

    public async void Distargeted()
    {
        KillTween();
        var sp = GetNode<Node2D>("ExSprite");
        _tweenTargeted = CreateTween().BindNode(sp);
        _tweenTargeted2 = CreateTween().BindNode(sp);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1f, 1f), 0.2f);
        _tweenTargeted2.TweenProperty(sp, "modulate", _colortgBox.Default, 0.2f);
        await ToSignal(_tweenTargeted, Tween.SignalName.Finished);
        KillTween();
        _tweenTargeted = CreateTween().BindNode(sp).SetLoops(-1);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(1.02f, 1.02f), 0.7f);
        _tweenTargeted.TweenProperty(sp, "scale", 2.4f * new Vector2(0.98f, 0.98f), 0.7f);
    }

    private Tween _tweenTargeted2;
    private Tween _tweenTargeted;

    private void DeleteTargeted()
    {
        KillTween();
        Calling = false;
        var sp = GetNode<Node2D>("ExSprite");
        sp.Modulate = _colortgBox.Default;
        sp.Scale = 2.4f * new Vector2(1f, 1f);
        sp.Visible = false;
    }

    public void CardTryFresh(CardModel cardModel)
    {
        if (cardModel == null) return;
        if (cardModel != Model) return;
        Fresh();
    }

    private bool isFirstPlace = false;

    private void Fresh()
    {
        if (Model == null) return;

        if (isFirstPlace)
        {
            _isOpen = true;
            Scale = new(0.17f, 0.17f);
            GetNode<SpineHandler>("土坑").Visible = Model.Camp switch
            {
                Camp.Plant => false,
                Camp.Zombie => true,
                _ => false
            };
            GetNode<SpineHandler>("%动画").LoadSkeletonData(Model.AnimationPath);
            GetNode<SpineHandler>("土坑").LoadSkeletonData("res://数据资源/僵尸动画/土坑动画.tres");
            Visible = true;
            if (Zombie)
            {
                GetNode<SpineHandler>("土坑").Visible = true;
                GetNode<SpineHandler>("土坑").SetAnimation(0, "intro", false);
                GetNode<SpineHandler>("土坑").SetAttachment("土坑", Road.Type switch
                {
                    RoadType.Ground => "zombie_dirt_back",
                    RoadType.Height => "zombie_roof_back",
                    RoadType.Water => "zombie_water_back",
                    _ => null
                });
            }
            isFirstPlace = false;
        }

        if (Model is FighterCardModel fighterCard)
        {
            GetNode<Node2D>("%基础信息").Visible = true;
            AtkAnim();
            HpAnim();
            if (fighterCard.StarType != null)
            {
                GetNode<SpineHandler>("%等级").LoadSkeletonData(fighterCard.StarType.IconSkelPath);
                GetNode<SpineHandler>("%等级").SetAnimation(0, "intro", false);
            }
            if (fighterCard.Hp.HasChanged)
            {
                if (fighterCard.Hp.PositiveChanged)
                    GetNode<Label>("%血量数值").AddThemeColorOverride("default_color", _colorBox.ColorB);
                else
                    GetNode<Label>("%血量数值").AddThemeColorOverride("default_color", _colorBox.ColorC);
            }
            else
                GetNode<Label>("%血量数值").AddThemeColorOverride("default_color", _colorBox.Default);

            if (fighterCard.Atk.HasChanged)
            {
                if (fighterCard.Atk.PositiveChanged)
                    GetNode<Label>("%伤害数值").AddThemeColorOverride("default_color", _colorBox.ColorB);
                else
                    GetNode<Label>("%伤害数值").AddThemeColorOverride("default_color", _colorBox.ColorC);
            }
            else
                GetNode<Label>("%伤害数值").AddThemeColorOverride("default_color", _colorBox.Default);
        }
    }

    private Tween _tweenAtk;
    private Tween _tweenHp;

    private Tween LabelIntro(Label label, float duration = 0.5f)
    {
        var tween = CreateTween().BindNode(label);
        tween.SetTrans(Tween.TransitionType.Back);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(label, "scale", Vector2.One, duration);
        return tween;
    }

    private void KillAtkTween()
    {
        if (_tweenAtk != null && _tweenAtk.IsValid())
            _tweenAtk.Kill();
    }

    private void KillHpTween()
    {
        if (_tweenHp != null && _tweenHp.IsValid())
            _tweenHp.Kill();
    }

    private async void AtkAnim()
    {
        if (Model is not FighterCardModel fighterCard) return;

        KillAtkTween();

        var atkLabel = GetNode<Label>("%伤害数值");
        var atkSpine = GetNode<SpineHandler>("%伤害动画");

        atkLabel.Text = "";
        atkLabel.Scale = Vector2.Zero;

        atkSpine.LoadSkeletonData(fighterCard.AtkType.Current.IconSkelPath);
        atkSpine.Scale = new Vector2(0.85f, 0.85f);

        await atkSpine.SetAnimationTask(0, "intro");

        atkLabel.Text = $"{fighterCard.Atk.Current}";
        _tweenAtk = LabelIntro(atkLabel);
    }

    private async void HpAnim()
    {
        if (Model is not FighterCardModel fighterCard) return;

        KillHpTween();

        var hpLabel = GetNode<Label>("%血量数值");
        var hpSpine = GetNode<SpineHandler>("%血量动画");

        hpLabel.Text = "";
        hpLabel.Scale = Vector2.Zero;

        hpSpine.LoadSkeletonData(fighterCard.HpType.Current.IconSkelPath);
        GetNode<SpineHandler>("%额外血量动画").LoadSkeletonData(fighterCard.HpType.Current.IconSkelPath);
        GetNode<SpineHandler>("%额外血量动画").Scale = new Vector2(0.85f, 0.85f);
        hpSpine.Scale = new Vector2(0.85f, 0.85f);

        await hpSpine.SetAnimationTask(0, "intro");

        hpLabel.Text = $"{fighterCard.Hp.Current}";
        _tweenHp = LabelIntro(hpLabel);
    }

    private void Clear()
    {
        var sprite = GetNode<SpineHandler>("%动画");
        sprite.ClearTracks();
        GetNode<SpineHandler>("土坑").ClearTracks();
        Instances[this] = null;
    }
}

public enum Location
{
    Zombie,
    PlantFront,
    Plant
}