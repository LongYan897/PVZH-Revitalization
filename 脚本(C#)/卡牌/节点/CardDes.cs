using Battle;
using Battle.Entity;
using Controller;
using Godot;
using Pack;
using Spine;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Godot.OpenXRCompositionLayer;

namespace Card;

/// <summary>
/// 用于给卡牌描述的URL设置描述<br/>
/// 比如<br/>
/// 我希望给致命设置描述<br/>
/// 那么可以在某个类里面写<br/>
/// [URL]<br/>
/// public string Lethal => "攻击到的单位立即死亡"<br/>
/// 这个就代表了 _Lethal_ 这个标签点开后显示 "攻击到的单位立即死亡"<br/>
/// static的属性也可以这样注册<br/>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false,Inherited = false)]
public class URLAttribute : Attribute
{

}
/// <summary>
/// 卡牌描述的可视化部分
/// </summary>
[GlobalClass]
public partial class CardDes : Control
{
    private static readonly PackedScene Scene = GD.Load<PackedScene>("res://场景(C#)/卡牌界面.tscn");
    private CardModel Model;
    public static CardDes instance { get; private set; }
    /// <summary>
    /// 创建卡牌的描述
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="card"></param>
    /// <returns></returns>
    public static void DisplayDescription(CardModel card,Node parent)
    {
        if (instance == null)
        {
            var des = Scene.Instantiate<CardDes>();
            des.Model = card;
            parent.AddChild(des);
            des.Open();
            des.Fresh();
            des.ZIndex = 1024;
            instance = des;
        }
        else
        {
            if (instance.Visible == true) return;

            if (instance.GetParent() != parent)
            {
                instance.Reparent(parent, true);
            }

            instance.Model = card;
            instance.Visible = true;
            instance.Open();
            instance.Fresh();
        }
    }
    /// <summary>
    /// 交换卡牌的描述
    /// </summary>
    /// <param name="card"></param>
    public static void ExchangeDescription(CardModel card, Node parent)
    {
        if (instance == null)
        {
            return;
        }
        else
        {

            if (instance.GetParent() != parent)
            {
                instance.Reparent(parent, true);
            }

            instance.Model = card;
            instance.Visible = true;
            instance.Open();
            instance.Fresh();
        }
    }
    /// <summary>
    /// 创建卡牌的描述
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="card"></param>
    /// <returns></returns>
    public static void DisplayDescription(CardModel card)
    {
        if (instance == null)
        {
            var des = Scene.Instantiate<CardDes>();
            des.Model = card;
            Main.DesLayer.AddChild(des);
            des.Open();
            des.Fresh();
            des.ZIndex = 1024;
            instance = des;
        }
        else
        {
            if (instance.Visible == true) return;
            instance.Model = card;
            instance.Visible = true;
            instance.Open();
            instance.Fresh();
        }
    }
    /// <summary>
    /// 交换卡牌的描述
    /// </summary>
    /// <param name="card"></param>
    public static void ExchangeDescription(CardModel card)
    {
        if (instance == null)
        {
            return;
        }
        else
        {
            instance.Model = card;
            instance.Visible = true;
            instance.Open();
            instance.Fresh();
        }
    }
    private Tween _tween;
    private bool _isEnd = false;
    private Node2D _target;
    private bool _targetCanScale = false;

    private void Open()
    {
        _isEnd = false;
        GetNode<Polygon2D>("%底色").TopLevel = true;
        GetNode<Control>("%单位容器").ZIndex = 1036;
        GetNode<Control>("%单位容器").Position = new(360f, 305f);
        GetNode<Control>("%单位容器").TopLevel = true;
        Scale = Vector2.Zero;
        GetNode<Polygon2D>("%底色").Modulate = new Color(0, 0, 0, 0);
        _tween = CreateTween();
        _tween.SetTrans(Tween.TransitionType.Back);
        _tween.SetEase(Tween.EaseType.Out);
        _tween.Parallel().TweenProperty(this, "scale", Vector2.One, 0.5f);
        _tween.Parallel().TweenProperty(GetNode<Polygon2D>("%底色"), "modulate", new Color(0, 0, 0, 0.5f), 0.5f);
    }

    private async void Clear()
    {
        if (_isEnd) return;
        _targetCanScale = true;
        if (_tween != null && _tween.IsValid())
        {
            _tween.Kill();
        }
        GetNode<Control>("%单位容器").ZIndex = 12;
        GetNode<Control>("%单位容器").Position = new(0f, -325f);
        GetNode<Control>("%单位容器").TopLevel = false;
        var sprite = GetNode<SpineHandler>("%动画");
        sprite.ClearTrack(0);
        _tween = CreateTween();
        _tween.SetTrans(Tween.TransitionType.Back);
        _tween.SetEase(Tween.EaseType.In);
        _tween.Parallel().TweenProperty(this, "scale", Vector2.Zero, 0.3f);
        _tween.Parallel().TweenProperty(GetNode<Polygon2D>("%底色"), "modulate", new Color(0, 0, 0, 0), 0.3f);

        await ToSignal(GetTree().CreateTimer(0.1f), Timer.SignalName.Timeout);

        await ToSignal(GetTree().CreateTimer(0.4f), Timer.SignalName.Timeout);
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (_target != null && !_targetCanScale)
        {
            _target.GlobalScale = Vector2.One * 0.3f;
        }
        else if (_target != null)
        {
            _target.Scale = Vector2.One * 0.3f;
        }
    }

    private ColorBox _colorBox = new ColorBox(new("f4f4d5"), new("20ff07"), new("94ff42"), new("bf1717"));
    private ColorBox _colorBox2 = new ColorBox(new("bf3b3b"), default, default, default);
    public void CardTryFresh(CardModel cardModel)
    {
        if (cardModel == null) return;
        if (cardModel == Model)
        {
            Fresh();
        }
    }
    private static readonly Regex IconRegex =
        new Regex(@"=(\w+)=(?:\{([+-]?\d+)\})?", RegexOptions.Compiled);

    private static readonly Regex LinkRegex =
        new Regex(@"_([^_]+)_", RegexOptions.Compiled);

    private static readonly Regex LabelRegex =
        new Regex(@"\$([^$]+)\$", RegexOptions.Compiled);

    private static string ConvertLabels(string text) =>
        LabelRegex.Replace(text,
            mm => $"[color=#b3ff30][u]{mm.Groups[1].Value}[/u][/color]");

    private static string ConvertLinks(string text) =>
        LinkRegex.Replace(text,
            mm => $"[color=#44ffff][url={mm.Groups[1].Value}]{mm.Groups[1].Value}[/url][/color]");

    /// <summary>
    /// 解析语法并写入。
    /// convertLinks = true：_XXX_ 变为 URL（一般卡牌主描述用）
    /// convertLinks = false：_XXX_ 保留（一般弹出面板用）
    /// </summary>
    public static async Task AppendWithIcons(RichTextLabel label, string raw, bool convertLinks)
    {
        label.Clear();
        label.BbcodeEnabled = true;

        int last = 0;
        foreach (Match m in IconRegex.Matches(raw))
        {
            if (m.Index > last)
            {
                string seg = raw.Substring(last, m.Index - last);
                seg = ConvertLabels(seg);
                label.AppendText(convertLinks ? ConvertLinks(seg) : seg);
            }

            string name = m.Groups[1].Value;
            bool hasNum = m.Groups[2].Success;
            string value = hasNum ? m.Groups[2].Value : null;

            if (Icon.TryGet(name, out var path, out var attr))
            {
                if (hasNum)
                {
                    var tex = await IconComposer.Instance.ComposeAsync(
                        path, value, attr.Color, attr.OutlineColor, attr.OutlineSize);
                    if (tex != null)
                        label.AddImage(tex, tex.GetWidth(), tex.GetHeight());
                }
                else
                {
                    label.AppendText($"[img=32]{path}[/img]");
                }
            }

            last = m.Index + m.Length;
        }

        if (last < raw.Length)
        {
            string seg = raw.Substring(last);
            seg = ConvertLabels(seg);
            label.AppendText(convertLinks ? ConvertLinks(seg) : seg);
        }
    }

    public async Task SetDescription(string raw)
    {
        var label = GetNode<RichTextLabel>("%介绍Label");

        await AppendWithIcons(label, raw, convertLinks: true);
    }
    private bool _keywordClicked = false;
    public override void _Ready()
    {
        var label = GetNode<RichTextLabel>("%介绍Label");
        label.BbcodeEnabled = true;
        label.MouseFilter = MouseFilterEnum.Stop;
        label.MetaClicked += OnKeywordClicked;
        var contr = GetNode<Control>("%额外区域");
        contr.GuiInput += ControlInput;
    }

    public void ControlInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mb || !mb.Pressed)
            return;

        if (_isEnd) return;

        Clear();
        _isEnd = true;
        GetViewport().SetInputAsHandled();
    }
    private void OnKeywordClicked(Variant meta)
    {
        string key = meta.AsString();

        if (CardModel.HasTemplate(key))
        {
            GetViewport().SetInputAsHandled();
            _keywordClicked = true;

            var template = CardModel.GetTemplate(key);
            if (template != null)
                ExchangeDescription(template);
            return;
        }

        if (TryGetKeywordDescription(key, out string desc))
        {
            GetViewport().SetInputAsHandled();
            _keywordClicked = true;

            var label = GetNode<RichTextLabel>("%介绍Label");
            Vector2 globalPos = label.GlobalPosition + label.GetLocalMousePosition();
            CardDesPanel.ShowAt(desc, globalPos);
        }
    }
    private static Dictionary<string, PropertyInfo> _urlCache;
    private static readonly object _urlCacheLock = new();

    /// <summary>
    /// 扫描当前程序集，缓存所有带 [URL] 的静态属性：key -> PropertyInfo
    /// </summary>
    private static Dictionary<string, PropertyInfo> GetURLCache()
    {
        if (_urlCache != null) return _urlCache;

        lock (_urlCacheLock)
        {
            if (_urlCache != null) return _urlCache;

            var flags = BindingFlags.Public
                      | BindingFlags.NonPublic
                      | BindingFlags.Static
                      | BindingFlags.Instance
                      | BindingFlags.FlattenHierarchy;

            var cache = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var type in typeof(CardDes).Assembly.GetTypes())
            {
                foreach (var prop in type.GetProperties(flags))
                {
                    if (!Attribute.IsDefined(prop, typeof(URLAttribute))) continue;
                    if (prop.PropertyType != typeof(string)) continue;

                    var getter = prop.GetGetMethod(true);
                    if (getter == null) continue;

                    if (!getter.IsStatic) continue;

                    if (!cache.ContainsKey(prop.Name))
                        cache[prop.Name] = prop;
                }
            }

            _urlCache = cache;
            return _urlCache;
        }
    }
    public static bool TryGetKeywordDescription(CardModel model, string key, out string desc)
    {
        desc = null;
        if (string.IsNullOrEmpty(key)) return false;

        if (model != null)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic
                      | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase;

            var prop = model.GetType().GetProperty(key, flags);
            if (TryGetFromProp(prop, model, out desc))
                return true;
        }

        var cache = GetURLCache();
        if (cache.TryGetValue(key, out var staticProp))
            return TryGetFromProp(staticProp, null, out desc);

        return false;
    }
    public bool TryGetKeywordDescription(string key, out string desc)
    {
        return TryGetKeywordDescription(Model,key, out desc);
    }
    private static bool TryGetFromProp(
    System.Reflection.PropertyInfo prop, object instance, out string desc)
    {
        desc = null;
        if (prop == null) return false;
        if (!Attribute.IsDefined(prop, typeof(URLAttribute))) return false;
        if (prop.PropertyType != typeof(string)) return false;

        var getter = prop.GetGetMethod(true);
        if (getter == null) return false;

        object target = getter.IsStatic ? null : instance;
        if (!getter.IsStatic && target == null) return false;

        desc = prop.GetValue(target) as string;
        return !string.IsNullOrEmpty(desc);
    }

    private async void Fresh()
    {
        if (Model != null)
        {
            var cost = GetNode<Label>("%花费数值Label");
            cost.Text = $"{Model.Cost.Current}";
            GetNode<Sprite2D>("%花费数值").Texture = Model.CampCostIcon;
            GetNode<Label>("%名字").Text = Model.Title;
            var exLabel = GetNode<RichTextLabel>("%额外标签");
            exLabel.BbcodeEnabled = true;
            await AppendWithIcons(exLabel, Model.ExDescription ?? "", convertLinks: true);
            await SetDescription(Model.GetDescription());
            GetNode<RichTextLabel>("%故事标签").Text = Model.Flavor;
            if (Model.Cost.HasChanged)
            {
                if (Model.Cost.PositiveChanged)
                    cost.Modulate = _colorBox.ColorB;
                else
                    cost.Modulate = _colorBox.ColorC;
            }
            foreach (var node in GetNode<SubViewport>("%裁剪").GetChildren())
            {
                node.QueueFree();
            }
            if (Model.AnimationScenePath != null)
            {
                var scene = GD.Load<PackedScene>(Model.AnimationScenePath).Instantiate<Node2D>();
                var col1 = scene.GetNode<Node2D>("Collection").Position;
                var col2 = scene.GetNode<Node2D>("Collection2").Position;
                GetNode<SpineHandler>("%动画").Position = (col1-col2)+ GetNode<SpineHandler>("%土坑").Position;
                if (scene.GetChildren().Any(c=>c.Name.ToString().Contains("Saved")))
                {
                    GetNode<SubViewport>("%裁剪").AddChild(scene);
                    scene.Position = GetNode<Marker2D>("%中心位置").Position - col1;
                    scene.ZIndex = GetNode<SpineHandler>("%土坑").ZIndex - 3;
                    foreach (var child in scene.GetChildren())
                    {
                        if (!child.Name.ToString().Contains("Saved"))
                        {
                            child.QueueFree();
                        }
                        else if (child is Control control)
                        {
                            control.MouseFilter = MouseFilterEnum.Ignore;
                        }
                    }
                }
            }
            else
            {
                GetNode<SpineHandler>("%动画").Position = new(5f, 140.0f);
            }
            if (Model is FighterCardModel fighterCard)
            {
                GetNode<Node2D>("%基础信息").Visible = true;
                AtkAnim();
                HpAnim();
                if (fighterCard.StarType != null)
                {
                    GetNode<SpineHandler>("%等级").LoadSkeletonData(fighterCard.StarType.IconSkelPath);
                    GetNode<SpineHandler>("%等级").SetAnimation(0, $"intro", false);
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

                GetNode<SpineHandler>("%土坑").Visible = Model.Camp switch
                {
                    Camp.Plant => false,
                    Camp.Zombie => true,
                    _ => false
                };
                GetNode<SpineHandler>("%土坑").LoadSkeletonData("res://数据资源/僵尸动画/土坑动画.tres");
                Visible = true;
                if (Model.Camp == Camp.Zombie)
                {
                    GetNode<SpineHandler>("%土坑").Visible = true;
                    GetNode<SpineHandler>("%土坑").SetAnimation(0, "intro", false);
                    if (fighterCard.CardTag.HasFlag(CardTag.Amphibious))
                        GetNode<SpineHandler>("%土坑").SetAttachment("土坑", "zombie_water_back");
                    else
                        GetNode<SpineHandler>("%土坑").SetAttachment("土坑", "zombie_dirt_back");
                }
            }
            else
            {
                GetNode<Node2D>("%基础信息").Visible = false;
                GetNode<SpineHandler>("%土坑").Visible = false;
            }
            GetNode<Sprite2D>("Shadow").Visible = Model.CardType.HasFlag(CardType.Fighter);
            GetNode<Sprite2D>("Shadow").Position = Model.Camp switch
            {
                Camp.Zombie => new(0, -229.0f),
                Camp.Plant => new(0, -233.0f),
                _ => new()
            };
            GetNode<Label>("%属性标签").Text = $"-{string.Join(" ", Model.Labels.Current)} {Model.CardType switch
            {
                CardType.Hero | CardType.Fighter => Model.Camp switch
                {
                    Camp.Plant => "超能力植物",
                    Camp.Zombie => "超能力僵尸",
                    _ => "???"
                },
                CardType.Hero | CardType.Trick => "超能力锦囊牌",
                CardType.Hero | CardType.Environment => "超能力环境",
                CardType.None | CardType.Trick => "锦囊牌",
                CardType.None | CardType.Environment => "环境",
                CardType.None | CardType.Fighter => Model.Camp switch
                {
                    Camp.Plant => "植物",
                    Camp.Zombie => "僵尸",
                    _ => "???"
                },
                _ => ""
            }} -";

            GetNode<Label>("%稀有标签Label").Text = (Model.Pack ?? Model.Rarity switch
            {
                Rarity.Token => null,
                _ => Model.CardType switch
                {
                    CardType.Hero | CardType.Fighter => null,
                    CardType.Hero | CardType.Environment => null,
                    CardType.Hero | CardType.Trick => null,
                    _ => "基础-"
                }
            })
            + Model.Rarity switch
            {
                Rarity.Basic => "常见",
                Rarity.Common => "常见",
                Rarity.Uncommon => "罕见",
                Rarity.Rare => "稀有",
                Rarity.SuperRare => "超稀有",
                Rarity.Legend => "传说",
                Rarity.Activity => "活动",
                Rarity.Token => "令牌",
                _ => "令牌"
            };

            GetNode<Sprite2D>("%单位框").SelfModulate = Model.Class switch
            {
                Class.Smarty => new Color("ffffff"),
                Class.Solar => new Color("f5c728"),
                Class.Guardian => new Color("844837"),
                Class.MegaGrow => new Color("3d9854"),
                Class.Kabloom => new Color("f43939"),
                Class.Brainy => new Color("e652c8"),
                Class.Sneaky => new Color("393a39"),
                Class.Beastly => new Color("2ebbda"),
                Class.Crazy => new Color("661dd1"),
                Class.Hearty => new Color("f19409"),
                _ => new Color("ffffff")
            };
            GetNode<TextureRect>("%左背景").Modulate = Model.Class switch
            {
                Class.Smarty => new Color("ffffff"),
                Class.Solar => new Color("f5c728"),
                Class.Guardian => new Color("844837"),
                Class.MegaGrow => new Color("3d9854"),
                Class.Kabloom => new Color("f43939"),
                Class.Brainy => new Color("e652c8"),
                Class.Sneaky => new Color("393a39"),
                Class.Beastly => new Color("2ebbda"),
                Class.Crazy => new Color("661dd1"),
                Class.Hearty => new Color("f19409"),
                _ => new Color("ffffff")
            };
            GetNode<TextureRect>("%右背景").Modulate = Model.Class switch
            {
                Class.Smarty => new Color("ffffff"),
                Class.Solar => new Color("f5c728"),
                Class.Guardian => new Color("844837"),
                Class.MegaGrow => new Color("3d9854"),
                Class.Kabloom => new Color("f43939"),
                Class.Brainy => new Color("e652c8"),
                Class.Sneaky => new Color("393a39"),
                Class.Beastly => new Color("2ebbda"),
                Class.Crazy => new Color("661dd1"),
                Class.Hearty => new Color("f19409"),
                _ => new Color("ffffff")
            };
            GetNode<TextureRect>("%属性图片").Texture = Model.Class switch
            {
                Class.Smarty => GD.Load<Texture2D>("res://素材/ui/界面ui/聪明.png"),
                Class.Solar => GD.Load<Texture2D>("res://素材/ui/界面ui/光能.png"),
                Class.Guardian => GD.Load<Texture2D>("res://素材/ui/界面ui/守卫.png"),
                Class.MegaGrow => GD.Load<Texture2D>("res://素材/ui/界面ui/猛长.png"),
                Class.Kabloom => GD.Load<Texture2D>("res://素材/ui/界面ui/爆花.png"),
                Class.Brainy => GD.Load<Texture2D>("res://素材/ui/界面ui/有脑.png"),
                Class.Sneaky => GD.Load<Texture2D>("res://素材/ui/界面ui/狡猾.png"),
                Class.Beastly => GD.Load<Texture2D>("res://素材/ui/界面ui/野兽.png"),
                Class.Crazy => GD.Load<Texture2D>("res://素材/ui/界面ui/疯狂.png"),
                Class.Hearty => GD.Load<Texture2D>("res://素材/ui/界面ui/健壮.png"),
                _ => GD.Load<Texture2D>("res://素材/ui/界面ui/聪明.png")
            };
            GetNode<TextureRect>("%稀有标签").Texture = Model.Rarity switch
            {
                Rarity.Basic => GD.Load<Texture2D>("res://素材/ui/稀有标签/rarity_0.png"),
                Rarity.Common => GD.Load<Texture2D>("res://素材/ui/稀有标签/rarity_0.png"),
                Rarity.Uncommon => GD.Load<Texture2D>("res://素材/ui/稀有标签/rarity_1.png"),
                Rarity.Rare => GD.Load<Texture2D>("res://素材/ui/稀有标签/rarity_2.png"),
                Rarity.SuperRare => GD.Load<Texture2D>("res://素材/ui/稀有标签/rarity_3.png"),
                Rarity.Legend => GD.Load<Texture2D>("res://素材/ui/稀有标签/rarity_4.png"),
                Rarity.Activity => GD.Load<Texture2D>("res://素材/ui/稀有标签/rarity_E.png"),
                Rarity.Token => GD.Load<Texture2D>("res://素材/ui/稀有标签/rarity_0.png"),
                _ => GD.Load<Texture2D>("res://素材/ui/稀有标签/rarity_0.png")
            };
            var rarity = GetNode<TextureRect>("%稀有标签");
            rarity.AnchorLeft = 0;
            rarity.AnchorRight = 0;
            rarity.AnchorTop = 0;
            rarity.AnchorBottom = 0;
            GetNode<TextureRect>("%稀有标签").Position = Model.Rarity switch
            {
                Rarity.Rare => new(-274.0f, 163.0f),
                Rarity.SuperRare => new(-290.0f, 144.0f),
                Rarity.Legend => new(-303.0f, 164.0f),
                Rarity.Activity => new(-290.0f, 97.0f),
                _ => new(-255.5f, 163.0f)
            };
            GetNode<TextureRect>("%稀有标签").Size = Model.Rarity switch
            {
                Rarity.Rare => new(551.0f, 109.0f),
                Rarity.SuperRare => new(580.0f, 149.0f),
                Rarity.Legend => new(606.0f, 164.0f),
                Rarity.Activity => new(580.0f, 187.0f),
                _ => new(511.0f, 75.0f)
            };
            var sprite = GetNode<SpineHandler>("%动画");
            sprite.LoadSkeletonData(Model.AnimationPath);
            sprite.SetAnimation(0, "intro", false);
            if (!Model.CardType.HasFlag(CardType.Trick))
                sprite.AddAnimation(0, "idle", true);
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
        hpSpine.Scale = new Vector2(0.85f, 0.85f);

        await hpSpine.SetAnimationTask(0, "intro");

        hpLabel.Text = $"{fighterCard.Hp.Current}";
        _tweenHp = LabelIntro(hpLabel);
    }
}
