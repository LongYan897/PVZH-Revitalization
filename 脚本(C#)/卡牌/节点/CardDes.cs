
using Battle.Entity;
using Godot;
using Pack;
using Target;
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
    private static CardDes instance;
    /// <summary>
    /// 创建卡牌的描述
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="card"></param>
    /// <returns></returns>
    public static CardDes DisplayDescription(Node parent, CardModel card)
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
            return des;
        }
        else
        {
            instance.Model = card;
            if (parent != instance.GetParent())
            {
                instance.GetParent().RemoveChild(instance);
                parent.AddChild(instance);
            }
            instance.Visible = true;
            instance.Open();
            instance.Fresh();
            return instance;
        }
    }
    private Tween _tween;
    private bool _isEnd = false;
    private Node2D _target;
    private bool _targetCanScale = false;

    private void Open()
    {
        _isEnd = false;
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
    private void Fresh()
    {
        if (Model != null)
        {
            var cost = GetNode<Label>("%花费数值Label");
            cost.Text = $"{Model.Cost.Current}";
            GetNode<Sprite2D>("%花费数值").Texture = Model.CampCostIcon;
            GetNode<Label>("%名字").Text = Model.Title;
            GetNode<RichTextLabel>("%介绍Label").Text = Model.Description ?? "";
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
                GetNode<TextureRect>("%伤害").Texture = fighterCard.AtkType.Current.Icon;
                GetNode<Label>("%伤害数值").Text = $"{fighterCard.Atk.Current}";
                GetNode<TextureRect>("%血量").Texture = fighterCard.HpType.Current.Icon;
                GetNode<Label>("%血量数值").Text = $"{fighterCard.Hp.Current}";

                if (fighterCard.AtkType.Current.AddtiveIcon != null)
                    GetNode<TextureRect>("%攻击力特效").Texture = fighterCard.AtkType.Current.AddtiveIcon;
                else
                    GetNode<TextureRect>("%攻击力特效").Visible = false;

                if (fighterCard.HpType.Current.AddtiveIcon != null)
                    GetNode<TextureRect>("%生命值特效").Texture = fighterCard.HpType.Current.AddtiveIcon;
                else
                    GetNode<TextureRect>("%生命值特效").Visible = false;

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
            GetNode<Label>("%属性标签").Text = Model.Camp switch
            {
                Camp.Plant => "植物 ",
                Camp.Zombie => "僵尸",
                _ => "???"
            } + string.Join(" ",Model.Labels.Current);
            GetNode<Label>("%稀有标签Label").Text = Model.Pack ?? "基础" + "-" + Model.Rarity switch 
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

            GetNode<Sprite2D>("%单位框").Modulate = Model.Class switch
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
        }
    }
}
