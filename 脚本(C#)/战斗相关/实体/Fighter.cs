using Card;
using Card.Cmd;
using Controller;
using Godot;
using Hero;
using Pack;
using Phrases;
using Play;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Target;

namespace Battle.Entity;

[GlobalClass]
/// <summary>
/// 战斗单位的实体
/// </summary>
public partial class Fighter : Control, ITarget, IAttackable
{
    private static readonly PackedScene Scene = GD.Load<PackedScene>("res://场景(C#)/战斗单位.tscn");
    private static readonly List<Fighter> Instances = new();
    private const int maxInstance = 40;
    public static void ClearAll()
    {
        foreach (var p in Instances)
        {
            if (GodotObject.IsInstanceValid(p))
                p.QueueFree();
        }
    }
    public static bool CanTargetedBy(CardModel card, bool fromBuff)
    {
        if (card.TargetType.HasFlag(TargetType.Fighters) || card.TargetType.HasFlag(TargetType.GraveStone))
            return TargetRegistry.AnyCanBeTarget(card, card.TargetType, fromBuff, card.TargetFilter);
        else
            return false;
    }
    /// <summary>
    /// 检测是否有某种单位
    /// </summary>
    /// <param name="filter">筛选器</param>
    /// <returns></returns>
    public static bool HasFighter(Func<Fighter, bool> filter) => Instances.Any(filter);

    public FighterCardModel Model { get; private set; }
    public Road Road { get; private set; }
    private Location index;

    public bool Zombie => Model.Camp == Camp.Zombie;
    public bool Plant1 => Model.Camp == Camp.Plant && index == Location.PlantFront;
    public bool Plant2 => Model.Camp == Camp.Plant && index == Location.Plant;

    public TargetType TargetType => TargetType.Fighters | TargetType.GraveStone;
    public bool IsHiding { get; private set; }
    public bool Calling { get; private set; }

    private bool _isOpen = false;
    private Vector2 _scale;
    private bool isFirstPlace = false;
    private bool isInvisible = false;
    private List<Fighter> fighters = new();
    private bool _busy = false;

    private SpineHandler _dmgSpine;
    private SpineHandler _hpSpine;
    private Label _dmgLabel;
    private Label _hpLabel;

    private Tween _hurtTween;
    private Tween _tweenAtk;
    private Tween _tweenHp;
    private Tween _tweenTargeted;
    private Tween _tweenTargeted2;

    private ColorBox _colortgBox = new ColorBox(new Color("ffffff"), new Color("37ff00"), new Color("6ef0ff"), default);
    private ColorBox _colorBox = new ColorBox(new("f4f4d5"), new("20ff07"), new("94ff42"), new("fc1e1e"));
    private ColorBox _colorBox2 = new ColorBox(new("bf3b3b"), default, default, default);

    public Func<CardModel, Func<ITarget, bool>, bool, bool> CanBeTarget =>
        (t, f, from) =>
        {
            if (Model.IsDie)
                return false;
            bool? a;
            a = f?.Invoke(this);
            bool b;
            b = Model.CanbeTarget(t, from);
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
            bool isGraveHunt = IsHiding;

            if (!t.FriendTarget && !fusOrEvoQuery)
                b = b && camp;
            if (!t.TargetType.HasFlag(TargetType.GraveStone) && IsHiding)
                b = false;
            return b;
        };

    public static async Task Generate(FighterCardModel model, Road road, Location index, Node parent = null)
    {
        parent ??= road.IsUp switch
        {
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
            Instances.Add(fighter);
        }
        else
        {
            fighter = Instances.First(f => f.Model == null);
            if (fighter.GetParent() != parent)
            {
                fighter.Reparent(parent, true);
            }
        }

        fighter.Position = NodeRoad.GetNode(road).GetFighterLocation(index);
        fighter.Model = model;
        fighter.index = index;
        fighter.isFirstPlace = true;
        fighter.Road = road;
        model.Road = road;
        model.Location = index;
        TargetRegistry.Register(fighter);
        if (model.CardTag.HasFlag(CardTag.GraveStone))
        {
            fighter.Fresh();
            fighter.Visible = true;
            await fighter.HideToGrave();
        }
        else
        {
            fighter.Fresh();
            fighter.ToVisible();
            fighter.Visible = true;
        }
        await road.AddFighter(model, fighter);
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

    public static Fighter GetNode(FighterCardModel fighterCard)
    {
        return Instances.FirstOrDefault(f => f.Model == fighterCard);
    }

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
            CallDeferred("reparent", parent);
        }
    }

    /// <summary>
    /// 将当前战斗单位变化为另一个战斗单位
    /// </summary>
    /// <param name="newModel">变化后绑定的卡牌</param>
    public async Task TransformTo(FighterCardModel newModel)
    {
        if (Model == null || Model.CardTag.HasFlag(CardTag.GraveStone)) return;
        if (newModel == null || newModel == Model) return;
        if (newModel.IsDie) return;

        var oldModel = Model;
        var road = Road;
        var location = index;

        await road.RemoveFighter(oldModel, this);

        oldModel.Road = null;
        oldModel.Location = default;

        Model = newModel;
        newModel.Road = road;
        newModel.Location = location;

        isFirstPlace = true;

        Fresh();

        await road.AddFighter(newModel, this);
    }

    /// <summary>
    /// 死亡表现
    /// </summary>
    public async Task Die()
    {
        _isOpen = false;
        _busy = true;
        var fighterCard = Model;
        await Road.RemoveFighter(fighterCard, this);

        if (!IsHiding)
        {
            await PlayAnimation("die", false);

            if (Model.Camp == Camp.Plant)
            {
                Tween tween = CreateTween();
                tween.TweenProperty(GetNode<SpineHandler>("%动画"), "modulate", new Color(1, 1, 1, 0), 0.3f);
                await ToSignal(tween, Tween.SignalName.Finished);
            }

            GetNode<SpineHandler>("%伤害动画").SetAnimation(0, "die", false);
            GetNode<SpineHandler>("%血量动画").SetAnimation(0, "die", false);

            GetNode<Label>("%伤害数值").Visible = false;
            GetNode<Label>("%血量数值").Visible = false;

            if (Model.Camp == Camp.Zombie)
                await GetNode<SpineHandler>("%土坑").SetAnimationTask(0, "clear", false);

            Visible = false;
            Clear();

            await AnimaActor.PlayInstantAnimationTask("res://数据资源/属性动画/死亡特效.tres", GlobalPosition + new Vector2(0, -30), "intro");
        }
        else
        {
            await GraveOn(true);
            GetNode<Label>("%伤害数值").Visible = false;
            GetNode<Label>("%血量数值").Visible = false;
            GetNode<SpineHandler>("%伤害动画").Visible = false;
            GetNode<SpineHandler>("%血量动画").Visible = false;
            await PlayAnimation("die", false);
            Visible = false;
            Clear();
            await AnimaActor.PlayInstantAnimationTask("res://数据资源/属性动画/死亡特效.tres", GlobalPosition + new Vector2(0, -30), "intro");
        }
    }

    private void KillHurtTween()
    {
        if (_hurtTween != null && _hurtTween.IsValid())
            _hurtTween.Kill();
    }

    /// <summary>
    /// 受伤表现
    /// </summary>
    public async Task Hurt(int amount)
    {
        _busy = true;
        KillHurtTween();
        List<string> k1s = ["hurt", "hit", "damaged"];
        GetNode<Node2D>("%额外动画").Modulate = new Color(1, 1, 1, 1);
        GetNode<Node2D>("%额外动画").Position = new Vector2();
        Tween tween = CreateTween();
        tween.TweenProperty(GetNode<Node2D>("%额外动画"), "position", new Vector2(0, -300), 0.3f);
        GetNode<Label>("%额外血量数值").Text = $"-{amount}";
        GetNode<Node2D>("%额外动画").Visible = true;

        var animSpine = GetNode<SpineHandler>("%动画");
        float hurtDuration = animSpine.GetFirstAvailableDuration([.. k1s]);
        _dmgSpine.SetAnimation(0, "hurt", false);
        _hpSpine.SetAnimation(0, "hurt", false);
        await Task.WhenAll(FollowOffset(hurtDuration), PlayAnimation(k1s, false));
        GetNode<Node2D>("%额外动画").Position = new Vector2();
        GetNode<Node2D>("%额外动画").Visible = false;
        _busy = false;
        if (Visible && !IsHiding && Model != null)
            _ = PlayAnimation("idle", false);
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

    public void OnCallTargeted(TargetType targetType, CardModel card,Camp? camp)
    {
        if (!targetType.HasFlag(TargetType.Fighters) && !targetType.HasFlag(TargetType.GraveStone)) return;
        CallTargeted();
    }

    public void OnDeleteTargeted(TargetType targetType, Camp? camp)
    {
        if (targetType.HasFlag(TargetType.Fighters) || targetType.HasFlag(TargetType.GraveStone))
            DeleteTargeted();
    }

    /// <summary>
    /// 播放攻击动画（表现层）
    /// </summary>
    /// <param name="shots">子弹路径表：目标 → 若干条路径</param>
    /// <param name="isInstantAmmo">是否是立刻到达目标的子弹</param>
    public async Task Battle(IReadOnlyDictionary<IAttackable, List<AtkStack.ShotCurve>> shots, bool isInstantAmmo)
    {
        if (shots == null || shots.Count == 0) return;

        _busy = true;
        var spine = GetNode<SpineHandler>("%动画");
        spine.ZIndex += 999;
        List<string> k1s = ["attack_up", "attack"];
        List<string> k2s = ["attack_down", "attack1"];
        var key = Road.IsUp
            ? (Model.Camp == Camp.Zombie ? k1s : k2s)
            : (Model.Camp == Camp.Zombie ? k2s : k1s);
        spine.ClearTracks();
        var eventWait = spine.WaitForAnimEvent("attack");

        Task animTask;
        if (key.Any(k => spine.HasAnimation(k)))
            animTask = PlayAnimation(key, true);
        else
            animTask = PlayAnimation("attack", true);

        var ammoTasks = new List<Task>();

        if (Model.AmmoPath != null)
        {
            float remaining = await eventWait;
            Vector2 defaultFirePoint = spine.GetBoneWorldPos("FirePoint");

            foreach (var kv in shots)
            {
                var hit = kv.Key;
                var curves = kv.Value;
                if (hit == null) continue;

                bool hasValidCurve = false;

                if (curves != null)
                {
                    foreach (var sc in curves)
                    {
                        if (sc?.Curve == null || sc.Curve.PointCount == 0) continue;
                        hasValidCurve = true;

                        Vector2 firePoint = GetFirePoint(spine, sc.FirePointBone, defaultFirePoint);
                        var full = new Curve2D();
                        full.AddPoint(firePoint);
                        for (int i = 0; i < sc.Curve.PointCount; i++)
                            full.AddPoint(sc.Curve.GetPointPosition(i));

                        if (GD.Load(Model.AmmoPath) is Texture2D texture)
                            ammoTasks.Add(CreateAmmonAlongCurve(texture, full, remaining));
                    }
                }

                if (!hasValidCurve)
                {
                    Vector2 endPos = hit switch
                    {
                        Fighter f => f.GlobalPosition,
                        HeroInstance h => NodeRoad.GetNode(Road).GetHeroLocation(PlayerManager.GetFromHero(h.Hero)),
                        _ => Vector2.Zero
                    };

                    if (GD.Load(Model.AmmoPath) is Texture2D texture)
                        ammoTasks.Add(CreateAmmon(texture, defaultFirePoint, endPos, remaining));
                    else if (spine.HasAnimation("attackEffect"))
                        AnimaActor.PlayInstantAnimationAmmo(Model.AnimationPath, endPos, "attackEffect");
                    else
                        AnimaActor.PlayInstantAnimationAmmo(Model.AmmoPath, endPos, "");
                }
            }
        }

        await Task.WhenAll(ammoTasks.Append(animTask));
        spine.ZIndex -= 999;
        _busy = false;
        if (Visible && !IsHiding && Model != null)
            _ = PlayAnimation("idle", false);
    }

    /// <summary>
    /// 获取指定名称的发射骨骼世界坐标，骨骼为空或不存在时回落到默认骨骼
    /// </summary>
    /// <param name="spine">动画节点</param>
    /// <param name="bone">骨骼名</param>
    /// <param name="fallback">默认坐标</param>
    private static Vector2 GetFirePoint(SpineHandler spine, string bone, Vector2 fallback)
    {
        if (string.IsNullOrEmpty(bone)) return fallback;
        try
        {
            var pos = spine.GetBoneWorldPos(bone);
            if (pos != Vector2.Zero) return pos;
        }
        catch { }
        return fallback;
    }
    public async Task CreateAmmonAlongCurve(Texture2D texture, Curve2D curve, float duration)
    {
        var textureR = new TextureRect
        {
            Texture = texture,
            Visible = true
        };
        var size = textureR.Size;

        var path = new Path2D { Curve = curve };
        var follower = new PathFollow2D { Loop = false, Rotates = false };
        path.AddChild(follower);
        follower.AddChild(textureR);
        textureR.Position = -size / 2;

        if (Model != null && Model.ArrowAmmo)
        {
            textureR.PivotOffset = size / 2;
            follower.Rotates = true;
        }

        Main.AmmoAnimator.AddChild(path);

        Tween tween = CreateTween();
        tween.TweenProperty(follower, "progress_ratio", 1f, duration);
        await ToSignal(tween, Tween.SignalName.Finished);

        path.QueueFree();
    }

    public async Task CreateAmmon(Texture2D texture, Vector2 from, Vector2 to, float duration)
    {
        var textureR = new TextureRect
        {
            Texture = texture,
            Position = from,
            Visible = true
        };
        var size = textureR.Size;
        textureR.Position -= size / 2;

        if (Model != null && Model.ArrowAmmo)
        {
            textureR.PivotOffset = size / 2;
            Vector2 dir = to - from;
            textureR.Rotation = dir.Angle() - Mathf.Pi / 2;
        }

        Main.AmmoAnimator.AddChild(textureR);

        Tween tween = CreateTween();
        tween.TweenProperty(textureR, "position", to - size / 2, duration);
        await ToSignal(tween, Tween.SignalName.Finished);

        textureR.QueueFree();
    }

    private async Task GraveIn()
    {
        IsHiding = true;
        await GetNode<SpineHandler>("%墓碑").SetAnimationTask(0, "intro");
    }

    private async Task GraveOn(bool isForced = false)
    {
        await GetNode<SpineHandler>("%墓碑").SetAnimationTask(0, "clear");

        var grave = GetNode<SpineHandler>("%墓碑");
        var unit = GetNode<SpineHandler>("%动画");

        grave.Modulate = new Color(1, 1, 1, 1);
        unit.Modulate = new Color(1, 1, 1, 0);

        var fadeTween = CreateTween().SetParallel(true);
        fadeTween.TweenProperty(grave, "modulate", new Color(1, 1, 1, 0), 0.3f);
        fadeTween.TweenProperty(unit, "modulate", new Color(1, 1, 1, 1), 0.3f);
        await ToSignal(fadeTween, Tween.SignalName.Finished);

        grave.Visible = false;
        grave.Modulate = new Color(1, 1, 1, 1);
        unit.Modulate = new Color(1, 1, 1, 1);

        VisibleNotGrave();
        ToVisible();
        Fresh();
        await Model.FireTiming(Timing.WhenRevealed, Model, isForced);
        await CardCmd.TimingOnCards(Model, Timing.WhenRevealed, Model, isForced);
        IsHiding = false;
        await Model.FireTiming(Timing.AfterRevealed, Model, isForced);
        await CardCmd.TimingOnCards(Model, Timing.AfterRevealed, Model, isForced);
        if (Visible && Model != null)
            _ = PlayAnimation("idle", false);
    }

    private async void Hiding()
    {
        await PhraseManager.WaitForPhrase(Phrase.Trick, () => GraveOn(false), 1050 - Road.Index);
        await Model.PlayFighterAnimationTask("intro");
    }

    /// <summary>
    /// 把该单位藏入墓碑
    /// </summary>
    /// <returns></returns>
    public async Task HideToGrave()
    {
        _busy = true;
        InvisibleNotGrave();
        InitGravePit();
        await GraveIn();
        Hiding();
    }

    public async Task ReturnCard()
    {
        _busy = true;
        var fighterCard = Model;

        await Road.RemoveFighter(fighterCard, this);

        if (fighterCard != null)
        {
            fighterCard.Road = null;
            fighterCard.Location = default;
        }

        CardCmd.Add(fighterCard);
        Clear();
        Model = null;
        Visible = false;
        await fighterCard.Player.Status.ReturnHand(fighterCard, Position - new Vector2(256, 193) * 0.56f - new Vector2(0, 60f));
        await fighterCard.FireTiming(Timing.WhenBounce, fighterCard);
        await CardCmd.TimingOnCards(fighterCard, Timing.WhenBounce, fighterCard);

    }

    public async void Boost()
    {
        GetNode<SpineHandler>("%加成动画").Visible = true;
        await GetNode<SpineHandler>("%加成动画").SetAnimationTask(0, "intro", false);
        GetNode<SpineHandler>("%加成动画").Visible = false;
    }

    private Tween _tweenBoostAtk;
    public async void BoostAtk()
    {
        GetNode<SpineHandler>("%加成动画").Visible = true;
        BoostAtkAnim();
        await GetNode<SpineHandler>("%加成动画").SetAnimationTask(0, "intro", false);
        GetNode<SpineHandler>("%加成动画").Visible = false;
    }

    private void BoostAtkAnim()
    {
        if (Model == null || isInvisible || !Visible) return;

        var atkLabel = GetNode<Label>("%伤害数值");
        var atkSpine = GetNode<SpineHandler>("%伤害动画");

        if (_tweenBoostAtk != null && _tweenBoostAtk.IsValid())
            _tweenBoostAtk.Kill();

        atkLabel.Scale = Vector2.One;
        _tweenBoostAtk = CreateTween().BindNode(atkLabel);
        _tweenBoostAtk.SetTrans(Tween.TransitionType.Back);
        _tweenBoostAtk.SetEase(Tween.EaseType.Out);
        _tweenBoostAtk.TweenProperty(atkLabel, "scale", new Vector2(2.0f, 2.0f), 0.15f)
            .SetEase(Tween.EaseType.Out);
        _tweenBoostAtk.TweenProperty(atkLabel, "scale", Vector2.One, 0.3f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        var baseScale = atkSpine.Scale;
        var spineTween = CreateTween().BindNode(atkSpine);
        spineTween.TweenProperty(atkSpine, "scale", baseScale * 1.6f, 0.15f)
            .SetEase(Tween.EaseType.Out);
        spineTween.TweenProperty(atkSpine, "scale", baseScale, 0.25f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
    }

    public async Task PlayAnimation(List<string> name, bool returnToIdle, int track = 0)
    {
        var spine = GetNode<SpineHandler>("%动画");
        var valid = name.Where(n => spine.HasAnimation(n)).ToList();
        if (valid.Count == 0)
        {
            if (returnToIdle)
                spine.SetAnimation(track, "idle", true);
            return;
        }
        await spine.SetFirstAvailableAnimationTask(track, false, [.. valid]);
        if (returnToIdle)
            spine.SetAnimation(track, "idle", true);
    }

    public Task PlayAnimation(string name, bool returnToIdle, int track = 0)
    {
        var spine = GetNode<SpineHandler>("%动画");

        if (name == "idle")
        {
            spine.SetAnimation(track, "idle", true);
            return Task.CompletedTask;
        }

        return PlayOnce(spine, name, returnToIdle, track);
    }

    private async Task PlayOnce(SpineHandler spine, string name, bool returnToIdle, int track)
    {
        await spine.SetAnimationTask(track, name, false);
        if (returnToIdle)
            spine.SetAnimation(track, "idle", true);
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
        GetNode<SpineHandler>("%加成动画").LoadSkeletonData("res://数据资源/杂项/加成动画.tres");
        GetNode<SpineHandler>("%墓碑").LoadSkeletonData("res://素材/墓碑/墓碑.tres");

        _dmgSpine = GetNode<SpineHandler>("%伤害动画");
        _hpSpine = GetNode<SpineHandler>("%血量动画");
        _dmgLabel = GetNode<Label>("%伤害数值");
        _hpLabel = GetNode<Label>("%血量数值");
        _scale = GetNode<SpineHandler>("%动画").Scale;
    }

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

    public void CardTryFresh(CardModel cardModel)
    {
        if (cardModel == null) return;
        if (cardModel != Model) return;
        Fresh();
    }

    private void InvisibleNotGrave()
    {
        foreach (var node in GetChildren().Where(n => !n.Name.ToString().Contains("碰撞箱") && !n.Name.ToString().Contains("ExSprite") && !n.Name.ToString().Contains("墓碑") && !n.Name.ToString().Contains("单位场景")))
        {
            if (node is Control con)
            {
                con.MouseFilter = MouseFilterEnum.Ignore;
                con.Visible = false;
            }
            else if (node is Node2D n2)
            {
                n2.Visible = false;
            }
        }
        GetNode<SpineHandler>("墓碑").Visible = true;
        GetNode<SpineHandler>("%动画").Visible = false;
        GetNode<Sprite2D>("墓碑透视").Visible = false;
    }

    private void VisibleNotGrave()
    {
        foreach (var node in GetChildren().Where(n => !n.Name.ToString().Contains("碰撞箱") && !n.Name.ToString().Contains("ExSprite") && !n.Name.ToString().Contains("墓碑") && !n.Name.ToString().Contains("单位场景")))
        {
            if (node is Control con)
            {
                if (node.Name.ToString().Contains("碰撞"))
                    con.MouseFilter = MouseFilterEnum.Stop;
                con.Visible = true;
            }
            else if (node is Node2D n2)
            {
                n2.Visible = true;
            }
        }
        GetNode<SpineHandler>("墓碑").Visible = false;
        GetNode<SpineHandler>("%动画").Visible = true;
        GetNode<Sprite2D>("墓碑透视").Visible = false;

        var unit = GetNode<SpineHandler>("%动画");
        unit.Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(unit, "modulate", new Color(1, 1, 1, 1), 0.3f);
        tween.TweenCallback(Callable.From(() =>
        {
            unit.Modulate = new Color(1, 1, 1, 1);
        }));
    }

    private void InitGravePit()
    {
        if (Model == null) return;
        if (Model.Camp != Camp.Zombie) return;

        var tu = GetNode<SpineHandler>("%土坑");
        tu.Visible = true;
        tu.LoadSkeletonData("res://数据资源/僵尸动画/土坑动画.tres");
        tu.SetAnimation(0, "intro", false);

        tu.SetAttachment("土坑", Road.Type switch
        {
            RoadType.Ground => "zombie_dirt_back",
            RoadType.Height => "zombie_roof_back",
            RoadType.Water => "zombie_water_back",
            _ => "zombie_dirt_back"
        });
    }

    public void Invisible(Texture2D resistIcon)
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

        if (resistIcon != null)
        {
            GetNode<Node2D>("%单位场景").Visible = true;
            GetNode<Sprite2D>("%特效").Visible = true;
            GetNode<Sprite2D>("%特效").Texture = resistIcon;
            GetNode<SpineHandler>("%动画").Visible = false;
            GetNode<SpineHandler>("%土坑").Visible = false;
        }
        foreach (var f in fighters)
        {
            f.Invisible(null);
        }
    }

    public void GroupWith(FighterCardModel fighter)
    {
        var f = GetNode(fighter);
        fighters.Add(f);
    }

    public void ToVisible()
    {
        if (!isInvisible) return;
        isInvisible = false;
        foreach (var node in GetChildren())
        {
            if (node is Control control)
            {
                control.Visible = true;
                control.MouseFilter = MouseFilterEnum.Ignore;
            }
            if (node is Node2D n2)
            {
                if (node.Name == "%特效")
                { n2.Visible = false; }
                n2.Visible = true;
            }
        }
        GetNode<Control>("%碰撞").MouseFilter = MouseFilterEnum.Stop;
    }

    public static void Overlay(FighterCardModel fighter, Texture2D resistIcon)
    {
        var f = GetNode(fighter);
        f.Invisible(resistIcon);
    }

    public static async Task MoveTo(FighterCardModel model,Road road,Location location)
    {
        var f = GetNode(model);
        if (road == f.Road) return;
        if (!CanGenerate(model, road)) return;
        await f.Road.RemoveFighter(model, f);
        var node = NodeRoad.GetNode(road);
        Tween tween = f.CreateTween();
        if (model.Camp.Zombie())
            tween.TweenProperty(f,"position", node.GetFighterLocation(location) ,0.2f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        else
        {
            var p = road.GetFighters().FirstOrDefault(f => f.Model.Location == location);
            if (p != null)
            {
                if (location == Location.PlantFront)
                {
                    node.SwapForCoopGrid();
                }
                else
                {
                    node.SwapForGrid();
                }
            }
            tween.TweenProperty(f, "position", node.GetFighterLocation(location), 0.2f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        }
        await tween.ToTask();
        await Task.WhenAll(f.PlayAnimation("intro",true),model.Camp.Zombie() ? f.GetNode<SpineHandler>("%土坑").SetAnimationTask(0,"intro") : Task.CompletedTask);
        f.Road = road;
        await road.AddFighter(model, f);
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
                var cols1 = scene.GetNode<Node2D>("Battle").Scale;
                GetNode<SpineHandler>("%动画").Position = (col1 - col2) + GetNode<SpineHandler>("%土坑").Position;
                GetNode<SpineHandler>("%动画").Scale = _scale * cols1;
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
                GetNode<SpineHandler>("%动画").Scale = _scale * new Vector2(1, 1);
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
            if (fighterCard.MaxHp.HasChanged || fighterCard.Hp.Current != fighterCard.MaxHp.Current)
            {
                if (fighterCard.MaxHp.PositiveChanged && fighterCard.MaxHp.Current == fighterCard.Hp.Current)
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
                    GetNode<Label>("%伤害数值").SelfModulate = _colorBox.ColorC;
            }
            else
                GetNode<Label>("%伤害数值").SelfModulate = _colorBox.Default;
        }
    }

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
        _busy = false;
        var sprite = GetNode<SpineHandler>("%动画");
        sprite.ClearTracks();
        GetNode<SpineHandler>("%土坑").ClearTracks();
        TargetRegistry.Unregister(this);

        foreach (var f in fighters.ToList())
        {
            fighters.Remove(f);
            f.Clear();
            f.Visible = false;
        }
    }
    public SpineHandler AnimationNode => GetNode<SpineHandler>("%动画");
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

public enum Location
{
    Zombie,
    PlantFront,
    Plant
}