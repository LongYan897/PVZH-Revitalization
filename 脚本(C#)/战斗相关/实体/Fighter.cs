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
    public static bool CanTargetedBy(CardModel card)
    {
        if (card.TargetType.HasFlag(TargetType.Fighters))
            return TargetRegistry.AnyCanBeTarget(card, card.TargetType, card.TargetFilter);
        else
            return false;
    }
    /// <summary>
    /// 检测是否有某种单位
    /// </summary>
    /// <param name="filter">筛选器</param>
    /// <returns></returns>
    public static bool HasFighter(Func<Fighter, bool> filter) => Instances.Keys.Any(filter);
    private static readonly PackedScene Scene = GD.Load<PackedScene>("res://场景(C#)/战斗单位.tscn");
    private static readonly Dictionary<Fighter, FighterCardModel> Instances = new();
    private const int maxInstance = 20;

    public FighterCardModel Model { get; private set; }
    public Road Road { get; private set; }
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
            bool isFusionHost = t is FighterCardModel && Model is FighterCardModel fm && fm.IsFusion;
            bool isEvolver = t is FighterCardModel tf && tf.IsEvolution;
            bool fusOrEvoQuery = isFusionHost || isEvolver;

            if (!t.FriendTarget && !fusOrEvoQuery)
                b = b && camp;
            return b;
        };
    /// <summary>
    /// 改变显示层级
    /// </summary>
    /// <param name="location"></param>
    public void SwitchLayer(Location location)
    {
        index = location;
        if (GetParent().GetParent() != Main.FighterContainer) return;
        var parent = Road.IsUp switch
        {
            false => location switch
            {
                Location.Plant => Main.FighterDown,
                Location.PlantFront => Main.FighterFrontDown,
                Location.Zombie => Main.FighterUp,
                _ => Main.FighterFrontUp
            },
            true => location switch
            {
                Location.Plant => Main.FighterUp,
                Location.PlantFront => Main.FighterFrontUp,
                Location.Zombie => Main.FighterDown,
                _ => Main.FighterFrontDown
            }
        };
        if (parent == GetParent()) return;
        else
        {
            Reparent(parent);
        }
    }
    /// <summary>
    /// 死亡表现
    /// </summary>
    public async Task Die()
    {
        _isOpen = false;
        var fighterCard = Model;
        await Road.RemoveFighter(fighterCard,this);

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
    private SpineHandler _dmgSpine;
    private SpineHandler _hpSpine;
    private Label _dmgLabel;
    private Label _hpLabel;

    /// <summary>
    /// 受伤表现
    /// </summary>
    public async Task Hurt(int amount)
    {
        KillHurtTween();

        GetNode<Node2D>("%额外动画").Modulate = new Color(1, 1, 1, 1);
        GetNode<Node2D>("%额外动画").Position = new Vector2();
        Tween tween = CreateTween();
        tween.TweenProperty(GetNode<Node2D>("%额外动画"), "position", new Vector2(0, -300), 0.3f);
        GetNode<Label>("%额外血量数值").Text = $"-{amount}";
        GetNode<Node2D>("%额外动画").Visible = true;

        var animSpine = GetNode<SpineHandler>("%动画");
        float hurtDuration = animSpine.GetAnimationDuration("hurt");
        _dmgSpine.SetAnimation(0, "hurt", false);
        _hpSpine.SetAnimation(0, "hurt", false);
        GetNode<SpineHandler>("%动画").SetAnimation(0, "hurt", false);
        await FollowOffset(hurtDuration);
        GetNode<Node2D>("%额外动画").Position = new Vector2();
        GetNode<Node2D>("%额外动画").Visible = false;
    }

    private async Task FollowOffset(float animDuration, float extraTime = 0.1f)
    {
        Vector2 worldOffset1 = _dmgSpine.GetBoneWorldPos("root");
        Vector2 worldOffset2 = _hpSpine.GetBoneWorldPos("root");
        Vector2 dmgLabelBase = _dmgLabel.Position;
        Vector2 hpLabelBase = _hpLabel.Position;

        float nTime = 0f;
        float total = animDuration + extraTime;

        while (nTime <= total)
        {
            Vector2 offset1 = _dmgSpine.GetBoneWorldPos("root") - worldOffset1;
            Vector2 offset2 = _hpSpine.GetBoneWorldPos("root") - worldOffset2;

            _dmgLabel.Position = dmgLabelBase + 6f * offset1;
            _hpLabel.Position = hpLabelBase + 6f * offset2;

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            nTime += (float)GetProcessDeltaTime();
        }
    }

    public TargetType TargetType => TargetType.Fighters;

    private ColorBox _colortgBox = new ColorBox(new Color("ffffff"), new Color("37ff00"), default,default);

    public void OnCallTargeted(TargetType targetType)
    {
        if (!targetType.HasFlag(TargetType.Fighters)) return;
        CallTargeted();
    }

    public void OnDeleteTargeted(TargetType targetType)
    {
        if (targetType.HasFlag(TargetType.Fighters))
            DeleteTargeted();
    }
    /// <summary>
    /// 播放攻击动画（表现层）
    /// </summary>
    /// <param name="target">目标</param>
    /// <param name="isInstantAmmo">是否是立刻到达目标的子弹</param>
    public async Task Battle(List<IAttackable> target, bool isInstantAmmo)
    {
        var spine = GetNode<SpineHandler>("%动画");
        spine.ZIndex += 999;
        var key = Road.IsUp
            ? (Model.Camp == Camp.Zombie ? "attack" : "attack1")
            : (Model.Camp == Camp.Zombie ? "attack1" : "attack");

        var eventWait = spine.WaitForAnimEvent("attack");

        var animTask = PlayAnimation(key, true);

        var ammoTasks = new List<Task>();

        if (Model.AmmoPath != null)
        {
            float remaining = await eventWait;
            Vector2 firePoint = spine.GetBoneWorldPos("FirePoint");

            foreach (var attackable in target)
            {
                if (attackable is not Fighter fighter) continue;
                Vector2 targetPos = fighter.GlobalPosition;

                if (GD.Load(Model.AmmoPath) is Texture2D texture)
                    ammoTasks.Add(CreateAmmon(texture, firePoint, targetPos, remaining));
                else
                    AnimaActor.PlayInstantAnimation(Model.AmmoPath, targetPos, "");
            }
        }

        await Task.WhenAll(ammoTasks.Append(animTask));
        spine.ZIndex -= 999;
    }
    private async Task CreateAmmon(Texture2D texture, Vector2 from, Vector2 to, float duration)
    {
        var textureR = new TextureRect
        {
            Texture = texture,
            Position = from,
            Visible = true
        };
        Main.Animator.AddChild(textureR);

        Tween tween = CreateTween();
        tween.TweenProperty(textureR, "position", to, duration);
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
    public static async Task<Fighter> Generate(FighterCardModel model, Road road, Location index, Node parent = null)
    {
        parent ??= road.IsUp switch {
            false => index switch
            { 
                Location.Plant => Main.FighterDown,
                Location.PlantFront => Main.FighterFrontDown,
                Location.Zombie => Main.FighterUp,
                _ => Main.FighterFrontUp
            },
            true => index switch
            {
                Location.Plant => Main.FighterUp,
                Location.PlantFront => Main.FighterFrontUp,
                Location.Zombie => Main.FighterDown,
                _ => Main.FighterFrontDown
            }
            };

        Fighter fighter;
        if (Instances.Count < maxInstance)
        {
            fighter = Scene.Instantiate<Fighter>();
            parent.AddChild(fighter);
            Instances.Add(fighter, model);
        }
        else
        {
            fighter = Instances.First(p => p.Key.Model == null).Key;
            if (fighter.GetParent() != parent)
            {
                fighter.Reparent(parent, true);
            }
            Instances[fighter] = model;
        }

        fighter.Position = NodeRoad.GetNode(road).GetFighterLocation(index);
        fighter.Model = model;
        fighter.index = index;
        fighter.isFirstPlace = true;
        fighter.Road = road;
        TargetRegistry.Register(fighter);
        await road.AddFighter(model,fighter);
        fighter.Visible = true;
        fighter.ToVisible();
        fighter.Fresh();
        model.Road = road;
        model.Location = index;
        return fighter;
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
                if (roadFighters.Any(f => f.Model.Camp == Camp.Plant && !f.Model.CardTag.HasFlag(CardTag.Coop)))
                    return false;
                else if (roadFighters.Count(f => f.Model.Camp == Camp.Plant) > 1)
                    return false;
                return true;
            }
        }
        return false;
    }

    public async Task<FighterCardModel> ReturnCard()
    {
        var fighterCard = Model;
        await Road.RemoveFighter(fighterCard,this);

        if (fighterCard != null)
        {
            fighterCard.Road = null;
            fighterCard.Location = default;
        }

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

    private ColorBox _colorBox = new ColorBox(new("f4f4d5"), new("20ff07"), new("94ff42"), new("bf1717"));
    private ColorBox _colorBox2 = new ColorBox(new("bf3b3b"),default,default,default);
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

        _dmgSpine = GetNode<SpineHandler>("%伤害动画");
        _hpSpine = GetNode<SpineHandler>("%血量动画");
        _dmgLabel = GetNode<Label>("%伤害数值");
        _hpLabel = GetNode<Label>("%血量数值");
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
    private bool isInvisible = false;
    private List<Fighter> fighters = new();
    public void Invisible(bool resistIcon)
    {
        if (isInvisible) return;
        isInvisible = true;
        foreach (var node in GetChildren())
        {
            if (node is Control control)
            {
                control.Visible = false;
                control.MouseFilter = MouseFilterEnum.Ignore;
            }
            if (node is Node2D n2)
            {
                n2.Visible = false;
            }
        }

        KillOverlayTween();
        if (resistIcon)
        {
            GetNode<Node2D>("%单位场景").Visible = true;
            GetNode<Node2D>("%土坑").Visible = false;
            GetNode<Node2D>("%特效").Visible = false;
            GetNode<Node2D>("%特效2").Visible = false;
            GetNode<SpineHandler>("%动画").Visible = true;
            GetNode<SpineHandler>("%动画").PauseAnimation();
        }
        foreach (var f in fighters)
        {
            f.Invisible(true);
        }
    }
    public void ToVisible()
    {
        if (!isInvisible) return;
        isInvisible =false;
        foreach (var node in GetChildren())
        {
            if (node is Control control)
            {
                control.Visible = true;
                control.MouseFilter = MouseFilterEnum.Ignore;
            }
            if (node is Node2D n2)
            {
                n2.Visible = true;
            }
        }
        GetNode<Control>("%碰撞").MouseFilter = MouseFilterEnum.Stop;
    }
    private Tween _overlayTween;
    private Tween _overlayTween2;
    private Tween _overlayTween3;
    private Tween _overlayTween4;
    private void KillOverlayTween()
    {

        if (_overlayTween != null && _overlayTween.IsValid())
        {
            _overlayTween.Kill();
        }
        if (_overlayTween2 != null && _overlayTween2.IsValid())
        {
            _overlayTween2.Kill();
        }
        if (_overlayTween3 != null && _overlayTween3.IsValid())
        {
            _overlayTween3.Kill();
        }
        if (_overlayTween4 != null && _overlayTween4.IsValid())
        {
            _overlayTween4.Kill();
        }
    }
    public void Overlay(FighterCardModel fighter,bool resistIcon)
    {
        KillOverlayTween();
        var f = GetNode(fighter);
        f.Invisible(resistIcon);
        fighters.Add(f);
        var cfx = GetNode<Node2D>("%特效");
        cfx.Visible = true;
        Tween tween = CreateTween();
        tween.TweenProperty(cfx,"scale",new Vector2(),0);
        tween.TweenProperty(cfx, "scale", new Vector2(4f,4f), 0.2f).SetEase(Tween.EaseType.Out);
        _overlayTween = CreateTween().BindNode(cfx).SetLoops(-1);
        _overlayTween.TweenProperty(cfx, "scale", new Vector2(3.7f, 3.7f), 2f).SetEase(Tween.EaseType.In);
        _overlayTween.TweenProperty(cfx, "scale", new Vector2(4.3f, 4.3f), 2f).SetEase(Tween.EaseType.Out);
        _overlayTween2 = CreateTween().BindNode(cfx).SetLoops(-1);
        _overlayTween2.TweenProperty(cfx, "rotation", -2*Math.PI, 16f).SetEase(Tween.EaseType.OutIn);
        _overlayTween2.TweenProperty(cfx, "rotation", 0, 16f).SetEase(Tween.EaseType.OutIn);
        var cfx2 = GetNode<Node2D>("%特效2");
        cfx2.Visible = true;
        Tween tween2 = CreateTween();
        tween2.TweenProperty(cfx2, "scale", new Vector2(), 0);
        tween2.TweenProperty(cfx2, "scale", new Vector2(3f, 3f), 0.2f).SetEase(Tween.EaseType.Out);
        _overlayTween3 = CreateTween().BindNode(cfx2).SetLoops(-1);
        _overlayTween3.TweenProperty(cfx2, "scale", new Vector2(3.3f, 3.3f), 2f).SetEase(Tween.EaseType.In);
        _overlayTween3.TweenProperty(cfx2, "scale", new Vector2(2.7f, 2.7f), 2f).SetEase(Tween.EaseType.Out);
        _overlayTween4 = CreateTween().BindNode(cfx2).SetLoops(-1);
        _overlayTween4.TweenProperty(cfx2, "rotation", 0, 16f).SetEase(Tween.EaseType.OutIn);
        _overlayTween4.TweenProperty(cfx2, "rotation", 2*Math.PI, 16f).SetEase(Tween.EaseType.OutIn);
    }
    private void Fresh()
    {
        if (isInvisible) return;
        if (Model == null) return;

        if (isFirstPlace)
        {
            _isOpen = true;
            Scale = new(0.17f, 0.17f);
            if (Model.AnimationScenePath != null)
            {
                var scene = GD.Load<PackedScene>(Model.AnimationScenePath).Instantiate<Node2D>();
                var col1 = scene.GetNode<Node2D>("Battle").Position;
                var col2 = scene.GetNode<Node2D>("Battle2").Position;
                GetNode<SpineHandler>("%动画").Position = (col1 - col2) + GetNode<SpineHandler>("%土坑").Position;
                if (scene.GetChildren().Any(c => c.Name.ToString().Contains("Saved")))
                {
                    GetNode<Control>("%单位场景").AddChild(scene);
                    scene.Position = GetNode<SpineHandler>("%土坑").Position;
                    scene.ZIndex = GetNode<SpineHandler>("%土坑").ZIndex - 2;
                    foreach (var child in scene.GetChildren())
                    {
                        if (!child.Name.ToString().Contains("Saved"))
                        {
                            child.QueueFree();
                        }
                        else if (child is Node2D n2)
                        {
                            n2.Position -= col2;
                        }
                        else if (child is Control control)
                        {
                            control.MouseFilter = MouseFilterEnum.Ignore;
                            control.Position -= col2;
                        }
                    }
                }
            }
            else
            {
                GetNode<SpineHandler>("%动画").Position = new(0f, -58.0f);
            }
            GetNode<SpineHandler>("%土坑").Visible = Model.Camp switch
            {
                Camp.Plant => false,
                Camp.Zombie => true,
                _ => false
            };
            GetNode<SpineHandler>("%动画").LoadSkeletonData(Model.AnimationPath);
            GetNode<SpineHandler>("%土坑").LoadSkeletonData("res://数据资源/僵尸动画/土坑动画.tres");
            Visible = true;
            if (Zombie)
            {
                GetNode<SpineHandler>("%土坑").Visible = true;
                GetNode<SpineHandler>("%土坑").SetAnimation(0, "intro", false);
                GetNode<SpineHandler>("%土坑").SetAttachment("土坑", Road.Type switch
                {
                    RoadType.Ground => "zombie_dirt_back",
                    RoadType.Height => "zombie_roof_back",
                    RoadType.Water => "zombie_water_back",
                    _ => null
                });
            }
            AtkAnim(true);
            HpAnim(true);
            isFirstPlace = false;
        }

        if (Model is FighterCardModel fighterCard)
        {
            GetNode<Node2D>("%基础信息").Visible = true;
            AtkAnim(false);
            HpAnim(false);
            if (fighterCard.StarType != null)
            {
                GetNode<SpineHandler>("%等级").LoadSkeletonData(fighterCard.StarType.IconSkelPath);
                GetNode<SpineHandler>("%等级").SetAnimation(0, "intro", false);
            }
            if (fighterCard.Hp.HasChanged)
            {
                if (fighterCard.Hp.PositiveChanged)
                    GetNode<Label>("%血量数值").SelfModulate = _colorBox.ColorB;
                else
                {
                    if (fighterCard.MaxHp.Current == fighterCard.Hp.Current)
                        GetNode<Label>("%血量数值").SelfModulate = _colorBox2.Default;
                    else
                        GetNode<Label>("%血量数值").SelfModulate = _colorBox.ColorC;
                }
            }
            else
                GetNode<Label>("%血量数值").SelfModulate = _colorBox.Default;

            if (fighterCard.Atk.HasChanged)
            {
                if (fighterCard.Atk.PositiveChanged)
                    GetNode<Label>("%伤害数值").SelfModulate = _colorBox.ColorB;
                else
                    GetNode<Label>("%伤害数值").SelfModulate = _colorBox2.Default;
            }
            else
                GetNode<Label>("%伤害数值").SelfModulate = _colorBox.Default;
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

    private async void AtkAnim(bool isFirst)
    {
        if (Model is not FighterCardModel fighterCard) return;

        KillAtkTween();

        var atkLabel = GetNode<Label>("%伤害数值");
        var atkSpine = GetNode<SpineHandler>("%伤害动画");

        if (isFirst)
        {
            atkLabel.Text = "";
            atkLabel.Scale = Vector2.Zero;

            atkSpine.LoadSkeletonData(fighterCard.AtkType.Current.IconSkelPath);
            atkSpine.Scale = new Vector2(0.85f, 0.85f);
            await atkSpine.SetAnimationTask(0, "intro");

            atkLabel.Text = $"{fighterCard.Atk.Current}";
            _tweenAtk = LabelIntro(atkLabel);
        }
        else
        {
            if (fighterCard.Atk.Current <= 0 && !fighterCard.BornWithAtk)
            {
                atkLabel.Visible = false;
                atkSpine.Visible = false;
            }
            else
            {
                atkLabel.Visible = true;
                atkSpine.Visible = true;
            }
            atkLabel.Text = $"{fighterCard.Atk.Current}";
        }
    }

    private async void HpAnim(bool isFirst)
    {
        if (Model is not FighterCardModel fighterCard) return;

        KillHpTween();

        var hpLabel = GetNode<Label>("%血量数值");
        var hpSpine = GetNode<SpineHandler>("%血量动画");
        var hpSpine2 = GetNode<SpineHandler>("%额外血量动画");

        if (isFirst)
        {
            hpLabel.Text = "";
            hpLabel.Scale = Vector2.Zero;

            hpSpine.LoadSkeletonData(fighterCard.HpType.Current.IconSkelPath);
            hpSpine2.LoadSkeletonData(fighterCard.HpType.Current.IconSkelPath);
            hpSpine.Scale = new Vector2(0.85f, 0.85f);
            hpSpine2.Scale = new Vector2(0.85f, 0.85f);

            await hpSpine.SetAnimationTask(0, "intro");

            hpLabel.Text = $"{fighterCard.Hp.Current}";
            _tweenHp = LabelIntro(hpLabel);
        }
        else
        {
            hpLabel.Text = $"{fighterCard.Hp.Current}";
        }
    }

    private void Clear()
    {
        var sprite = GetNode<SpineHandler>("%动画");
        sprite.ClearTracks();
        GetNode<SpineHandler>("%土坑").ClearTracks();
        TargetRegistry.Unregister(this);
        Instances[this] = null;

        foreach (var f in fighters.ToList())
        {
            fighters.Remove(f);
            f.Clear();
            f.Visible = false;
        }
    }
}

public enum Location
{
    Zombie,
    PlantFront,
    Plant
}