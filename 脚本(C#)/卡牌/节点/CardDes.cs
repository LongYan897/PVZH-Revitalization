using Controller;
using Godot;
using Pack;
using Spine;
using System.Linq;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 卡牌描述的可视化部分
/// </summary>
[GlobalClass]
public partial class CardDes : Control
{
    private static readonly PackedScene Scene = GD.Load<PackedScene>("res://场景(C#)/卡牌界面.tscn");
    private CardModel Model;
    public static CardDes instance { get; private set; }
    /// <summary>卡牌描述字号</summary>
    private const int DesFontSize = 24;
    /// <summary>描述图标尺寸</summary>
    private const int IconSize = 36;
    /// <summary>
    /// 创建卡牌的描述
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="card"></param>
    /// <returns></returns>
    public static void DisplayDescription(CardModel card, Node parent)
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
            instance.Open();
            instance.Fresh();
            instance.Visible = true;
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
            instance.Open();
            instance.Fresh();
            instance.Visible = true;
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
            instance.Open();
            instance.Fresh();
            instance.Visible = true;
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
            instance.Open();
            instance.Fresh();
            instance.Visible = true;
        }
    }
    private Tween _tween;
    private bool _isEnd = false;
    private Node2D _target;
    private bool _targetCanScale = false;
    public static void Close()
    {
        instance?.Clear();
        if (instance != null)
            instance._isEnd = true;
    }
    private void Open()
    {
        _isEnd = false;
        GetNode<Polygon2D>("%底色").TopLevel = true;
        GetNode<Control>("%单位容器").ZIndex = 1036;
        GetNode<Control>("%单位容器").Position = new(360f, 335f);
        GetNode<Control>("%单位容器").TopLevel = true;
        GetNode<Control>("%单位容器").Scale = Vector2.One;
        Scale = Vector2.Zero;
        GetNode<Polygon2D>("%底色").Modulate = new Color(0, 0, 0, 0);
        _tween = CreateTween();
        _tween.SetTrans(Tween.TransitionType.Back);
        _tween.SetEase(Tween.EaseType.Out);
        _tween.Parallel().TweenProperty(this, "scale", Vector2.One * 0.8f, 0.5f);
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
        GetNode<Control>("%单位容器").TopLevel = false;
        GetNode<Control>("%单位容器").Position = new(0f, -378f);
        GetNode<Control>("%单位容器").Scale = new Vector2(1.25f, 1.25f);

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

    private ColorBox _colorBox = new ColorBox(new("f4f4d5"), new("20ff07"), new("94ff42"), new("fc1e1e"));
    private ColorBox _colorBox2 = new ColorBox(new("bf3b3b"), default, default, default);
    public void CardTryFresh(CardModel cardModel)
    {
        if (cardModel == null) return;
        if (cardModel == Model)
        {
            Fresh();
        }
    }
    public static string ReplacePlaceholders(string rawDesc, string arg, char sep, CardModel model)
        => RichTextFormatter.ReplacePlaceholders(rawDesc, arg, sep, model);
    public static bool TryGetKeywordDescription(
        CardModel model, string key, string arg, char sep, out string desc)
        => RichTextFormatter.TryGetKeywordDescription(model, key, arg, sep, out desc);
    /// <summary>
    /// 解析语法并写入。
    /// </summary>
    public static Task AppendWithIcons(
    RichTextLabel label, string raw, bool convertLinks, float extraSize = 0f)
    => RichTextFormatter.AppendAsync(label, raw, convertLinks, extraSize);

    public async Task SetDescription(string raw)
    {
        var label = GetNode<SpecLabel>("%介绍Label");

        await label.SetContentAsync(raw, Model);
    }
    public override void _Ready()
    {
        var label = GetNode<SpecLabel>("%介绍Label");
        label.BbcodeEnabled = true;

        label.AddThemeFontSizeOverride("normal_font_size", DesFontSize);
        label.AddThemeFontSizeOverride("bold_font_size", DesFontSize);
        label.AddThemeFontSizeOverride("italics_font_size", DesFontSize);
        label.AddThemeFontSizeOverride("bold_italics_font_size", DesFontSize);
        label.AddThemeFontSizeOverride("mono_font_size", DesFontSize);

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

    private async void Fresh()
    {
        if (Model != null)
        {
            Position = new(360, 640);
            var cost = GetNode<Label>("%花费数值Label");
            cost.Text = $"{Model.Cost.Current}";
            GetNode<Sprite2D>("%花费数值").Texture = Model.CampCostIcon;
            GetNode<Label>("%名字").Text = Model.Title;
            var exLabel = GetNode<SpecLabel>("%额外标签");
            exLabel.BbcodeEnabled = true;
            exLabel.EnableKeywordClick = false;
            await exLabel.SetContentAsync(Model.ExDescription ?? "");
            await SetDescription(Model.GetDescription());
            GetNode<RichTextLabel>("%故事标签").Text = Model.Flavor;
            FitStoryFontSize(GetNode<RichTextLabel>("%故事标签"));
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
                var cols1 = scene.GetNode<Node2D>("Collection").Scale;
                Vector2 originalPos = (col1 - col2) + GetNode<SpineHandler>("%土坑").Position;
                GetNode<SpineHandler>("%动画").Position = originalPos + new Vector2(0f, 12f);
                GetNode<SpineHandler>("%动画").Scale = cols1 * 0.8f;
                if (scene.GetChildren().Any(c => c.Name.ToString().Contains("Saved")))
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
                GetNode<SpineHandler>("%动画").Position = new(5f, 140.0f + 12f);
                GetNode<SpineHandler>("%动画").Scale = new Vector2(1, 1) * 0.8f;
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

                GetNode<SpineHandler>("%土坑").Visible = Model.Camp switch
                {
                    Camp.Plant => false,
                    Camp.Zombie => true,
                    _ => false
                };
                GetNode<SpineHandler>("%土坑").LoadSkeletonData("res://数据资源/僵尸动画/土坑动画.tres");
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
            if (sprite.HasAnimation("intro"))
                sprite.SetAnimation(0, "intro", false);
            if (sprite.HasAnimation("idle"))
                sprite.AddAnimation(0, "idle", true);

            Visible = true;
        }
    }
    private static void FitStoryFontSize(RichTextLabel label, int baseSize = 38, int minSize = 12, int maxChars = 20)
    {
        int length = label.GetParsedText().Length;
        if (length <= 0) return;

        float ratio = length <= maxChars
            ? 1f
            : Mathf.Sqrt((float)maxChars / length);
        int size = Mathf.Clamp(Mathf.RoundToInt(baseSize * ratio), minSize, baseSize);

        label.AddThemeFontSizeOverride("normal_font_size", size);
        label.AddThemeFontSizeOverride("bold_font_size", size);
        label.AddThemeFontSizeOverride("italics_font_size", size);
        label.AddThemeFontSizeOverride("bold_italics_font_size", size);
        label.AddThemeFontSizeOverride("mono_font_size", size);
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