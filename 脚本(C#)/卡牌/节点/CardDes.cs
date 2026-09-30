using Battle.Entity;
using Controller;
using Godot;
using Pack;
using Spine;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Godot.OpenXRCompositionLayer;

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
    private Tween _tween;
    private bool _isEnd = false;
    private Node2D _target;
    private bool _targetCanScale = false;

    private void Open()
    {
        _isEnd = false;
        GetNode<Control>("%单位容器").ZIndex = 1036;
        GetNode<Control>("%单位容器").Position = new(360f, 305f);
        GetNode<Control>("%单位容器").TopLevel = true;
        GetNode<ColorRect>("%底色").MouseFilter = MouseFilterEnum.Stop;
        Scale = Vector2.Zero;
        GetNode<ColorRect>("%底色").Modulate = new Color(0, 0, 0, 0);
        _tween = CreateTween();
        _tween.SetTrans(Tween.TransitionType.Back);
        _tween.SetEase(Tween.EaseType.Out);
        _tween.Parallel().TweenProperty(this, "scale", Vector2.One, 0.5f);
        _tween.Parallel().TweenProperty(GetNode<ColorRect>("%底色"), "modulate", new Color(0, 0, 0, 0.5f), 0.5f);
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
        _tween.Parallel().TweenProperty(GetNode<ColorRect>("%底色"), "modulate", new Color(0, 0, 0, 0), 0.3f);

        await ToSignal(GetTree().CreateTimer(0.1f), Timer.SignalName.Timeout);
        GetNode<ColorRect>("%底色").MouseFilter = MouseFilterEnum.Ignore;

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

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventScreenTouch touch)
        {
            if (!touch.Pressed) return;
            if (!_isEnd)
            {
                Clear();
                _isEnd = true;
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private ColorBox _colorBox = new ColorBox(new("f4f4d5"), new("20ff07"), new Color(0, 243, 0), new Color(243, 0, 0));
    public void CardTryFresh(CardModel cardModel)
    {
        if (cardModel == null) return;
        if (cardModel == Model)
        {
            Fresh();
        }
    }
    public async Task SetDescription(string raw)
    {
        var matches = Regex.Matches(raw, @"=(\w+)=\{(\d+)\}");
        var label = GetNode<RichTextLabel>("%介绍Label");

        label.Clear();
        label.BbcodeEnabled = true;

        int last = 0;
        foreach (Match m in matches)
        {
            if (m.Index > last)
            {
                string textPart = raw.Substring(last, m.Index - last);
                textPart = Regex.Replace(textPart, @"_([^_]+)_",
                    mm => $"[color=#44ffff][url={mm.Groups[1].Value}]{mm.Groups[1].Value}[/url][/color]");
                label.AppendText(textPart);
            }

            string name = m.Groups[1].Value;
            string value = m.Groups[2].Value;

            if (Icon.TryGet(name, out var path, out var attr))
            {
                var tex = await IconComposer.Instance.ComposeAsync(path, value, attr.Color, attr.OutlineColor, attr.OutlineSize);
                if (tex != null)
                    label.AddImage(tex, tex.GetWidth(), tex.GetHeight());
            }

            last = m.Index + m.Length;
        }
        if (last < raw.Length)
        {
            string tail = raw.Substring(last);
            tail = Regex.Replace(tail, @"_([^_]+)_",
                mm => $"[color=#44ffff][url={mm.Groups[1].Value}]{mm.Groups[1].Value}[/url][/color]");
            label.AppendText(tail);
        }
    }
    private async void Fresh()
    {
        if (Model != null)
        {
            var cost = GetNode<Label>("%花费数值Label");
            cost.Text = $"{Model.Cost.Current}";
            GetNode<Sprite2D>("%花费数值").Texture = Model.CampCostIcon;
            GetNode<Label>("%名字").Text = Model.Title;
            await SetDescription(Model.Description);
            GetNode<RichTextLabel>("%故事标签").Text = Model.Flavor;
            if (Model.Cost.HasChanged)
            {
                if (Model.Cost.PositiveChanged)
                    cost.AddThemeColorOverride("default_color", _colorBox.ColorB);
                else
                    cost.AddThemeColorOverride("default_color", _colorBox.ColorC);
            }
            if (Model is FighterCardModel fighterCard)
            {
                GetNode<Node2D>("%基础信息").Visible = true;
                GetNode<SpineHandler>("%伤害动画").LoadSkeletonData(fighterCard.AtkType.Current.IconSkelPath);
                GetNode<SpineHandler>("%伤害动画").SetAnimation(0, $"intro", false);
                GetNode<Label>("%伤害数值").Text = $"{fighterCard.Atk.Current}";
                GetNode<SpineHandler>("%血量动画").LoadSkeletonData(fighterCard.HpType.Current.IconSkelPath);
                GetNode<SpineHandler>("%血量动画").SetAnimation(0, $"intro", false);
                GetNode<Label>("%血量数值").Text = $"{fighterCard.Hp.Current}";
                if (fighterCard.StarType != null)
                {
                    GetNode<SpineHandler>("%等级").LoadSkeletonData(fighterCard.StarType.IconSkelPath);
                    GetNode<SpineHandler>("%等级").SetAnimation(0, $"intro", false);
                }
                if (fighterCard.Hp.HasChanged)
                {
                    if (fighterCard.Hp.PositiveChanged)
                        GetNode<Label>("%血量数值").AddThemeColorOverride("default_color", _colorBox.ColorB);
                    else
                        GetNode<Label>("%血量数值").AddThemeColorOverride("default_color", _colorBox.ColorC);
                }

                if (fighterCard.Atk.HasChanged)
                {
                    if (fighterCard.Atk.PositiveChanged)
                        GetNode<Label>("%伤害数值").AddThemeColorOverride("default_color", _colorBox.ColorB);
                    else
                        GetNode<Label>("%伤害数值").AddThemeColorOverride("default_color", _colorBox.ColorC);
                }
            }
            else
            {
                GetNode<Node2D>("%基础信息").Visible = false;
            }
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
            sprite.AddAnimation(0, "idle", true);
        }
    }
}
