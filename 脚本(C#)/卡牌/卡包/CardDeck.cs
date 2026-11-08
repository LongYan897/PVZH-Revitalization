using Controller;
using Godot;
using Pack;
using Play;
using Scene;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Card.Pack;

[GlobalClass]
public partial class CardDeck : Control
{
    private static readonly PackedScene Scene = "res://场景(C#)/卡组卡包.tscn".Load<PackedScene>();
    private static CardDeck CurrentEnable;

    public PresetPack Deck;
    private readonly Dictionary<Control, Control.MouseFilterEnum> _originMouseFilters = new();

    public bool IsPendingDelete { get; private set; } = false;
    private Button _topButton;
    private NinePatchRect _operateButton;

    public static CardDeck Instance(PresetPack preset)
    {
        CardDeck deck = Scene.Instantiate<CardDeck>();
        deck.Deck = preset;
        deck.Predeck(preset);
        return deck;
    }

    private async void Pressed(PresetPack preset)
    {
        if (IsPendingDelete) return;

        if (CurrentEnable == this)
        {
            UnPressed();
            CurrentEnable = null;
            return;
        }

        GetNode<Sprite2D>("%背景").SelfModulate = new("084259");
        GetNode<NinePatchRect>("%操作按钮").PivotOffsetRatio = new(0.5f, 0.5f);
        GetNode<Node2D>("%表层").Visible = false;
        GetNode<Node2D>("%内层").Visible = true;
        Scale = new(1, 1);
        Tween tween = CreateTween();
        tween.TweenProperty(this, "scale", new Vector2(1, 1) * 0.98f, 0.1f).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", new Vector2(1, 1), 0.1f).SetEase(Tween.EaseType.In);
        await tween.ToTask();

        if (CurrentEnable != this)
            CurrentEnable?.UnPressed();
        CurrentEnable = this;
        GetNode<Button>("Button").MouseFilter = MouseFilterEnum.Ignore;
        SetInnerMouseEnabled(true);
        if (preset is DIYPack)
        {
            DeckEditSubMenuScene.CurrentPack = preset;
            GetNode<SubMenu>("%编辑").SubScene = "res://场景(C#)/定制卡组.tscn";
        }
        else
        {
            DeckPreviewSubMenuScene.CurrentPack = preset;
            GetNode<SubMenu>("%信息").SubScene = "res://场景(C#)/官方卡组.tscn";
        }
    }

    private void UnPressed()
    {
        GetNode<Sprite2D>("%背景").SelfModulate = new("ffffff");
        GetNode<Node2D>("%表层").Visible = true;
        GetNode<Node2D>("%内层").Visible = false;
        GetNode<Button>("Button").MouseFilter = MouseFilterEnum.Stop;
        SetInnerMouseEnabled(false);
    }

    private async void OnOperateGuiInput(InputEvent @event)
    {
        if (IsPendingDelete) return;
        if (_operateButton == null) return;
        if (@event is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left || !mb.Pressed) return;

        _operateButton.AcceptEvent();

        var tween = CreateTween();
        tween.TweenProperty(_operateButton, "scale", new Vector2(0.92f, 0.92f), 0.08f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(_operateButton, "scale", Vector2.One, 0.12f)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        await tween.ToTask();

        PlayerManager.HostPlayer.SelectedPreset = Deck;

        SceneManager.ReturnToSub();

        if (CurrentEnable == this)
        {
            UnPressed();
            CurrentEnable = null;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            if (CurrentEnable != this) return;

            var mouse = GetGlobalMousePosition();
            foreach (var kv in _originMouseFilters)
            {
                if (!IsInstanceValid(kv.Key)) continue;
                if (kv.Key.MouseFilter == MouseFilterEnum.Ignore) continue;
                if (kv.Key.GetGlobalRect().HasPoint(mouse)) return;
            }

            var button = GetNode<Button>("Button");
            if (!button.GetGlobalRect().HasPoint(mouse))
            {
                CurrentEnable?.UnPressed();
                CurrentEnable = null;
            }
        }
    }

    private void Predeck(PresetPack preset)
    {
        GetNode<Button>("Button").Pressed += () => Pressed(preset);
        GetNode<TextureRect>("%封皮").Texture = (preset.CustomFrame ?? preset.Hero.Data.DeckIcon).Load<Texture2D>();
        GetNode<Sprite2D>("%背景").Texture = preset.GetBg();
        GetNode<Sprite2D>("%卡套").Texture = preset.GetFrame();
        GetNode<Sprite2D>("%卡套").Modulate = preset is DIYPack ? new Color("1d658a") : new Color(1, 1, 1, 1);
        GetNode<SubMenu>("%编辑").Visible = preset is DIYPack;
        GetNode<SubMenu>("%信息").Visible = preset is not DIYPack;
        GetNode<SubMenu>("%编辑").Function(preset is DIYPack);
        GetNode<SubMenu>("%信息").Function(preset is not DIYPack);
        GetNode<Label>("%Label").Text = preset.PackName;
        GetNode<Node2D>("%进度").Visible = preset is not DIYPack;

        _operateButton = GetNodeOrNull<NinePatchRect>("%操作按钮");
        if (_operateButton != null)
        {
            _operateButton.MouseFilter = MouseFilterEnum.Stop;
            foreach (Node child in _operateButton.GetChildren())
                if (child is Control c) c.MouseFilter = MouseFilterEnum.Ignore;
            _operateButton.GuiInput += OnOperateGuiInput;
        }

        RegisterInner("%操作按钮");
        RegisterInner("%信息");
        RegisterInner("%编辑");
        SetInnerMouseEnabled(false);

        if (preset is not DIYPack)
        {
            var progress = preset.LockedCard;
            GetNode<Label>("%进度数字").Text = progress.ToString();
            GetNode<Panel>("%进度绿色条").Size = new(progress * (122f / 40f), 41f);
            GetNode<Panel>("%进度绿色亮条").Size = new(progress * (120f / 40f), 20f);
        }

        _topButton = GetNodeOrNull<Button>("%顶层按钮");
        if (_topButton != null)
        {
            _topButton.Pressed += OnTopButtonPressed;
            _topButton.Visible = false;
            _topButton.MouseFilter = MouseFilterEnum.Ignore;
        }
    }

    private void OnTopButtonPressed()
    {
        SetPendingDelete(!IsPendingDelete);
    }

    public void SetDeleteMode(bool active)
    {
        if (Deck is not DIYPack)
        {
            if (_topButton != null)
            {
                _topButton.MouseFilter = MouseFilterEnum.Ignore;
                _topButton.Visible = false;
            }
            return;
        }

        if (_topButton != null)
        {
            _topButton.MouseFilter = active ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
            _topButton.Visible = active;
        }
    }

    public void SetPendingDelete(bool pending)
    {
        if (IsPendingDelete == pending) return;
        IsPendingDelete = pending;

        if (_topButton != null)
        {
            var tween = CreateTween();
            tween.TweenProperty(_topButton, "scale", pending ? new Vector2(0.8f, 0.8f) : Vector2.One, 0.1f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
        }

        if (pending && CurrentEnable == this)
        {
            UnPressed();
            CurrentEnable = null;
        }
    }

    private void RegisterInner(string path)
    {
        var control = GetNodeOrNull<Control>(path);
        if (control == null) return;
        if (!_originMouseFilters.ContainsKey(control))
            _originMouseFilters[control] = control.MouseFilter;
    }

    private void SetInnerMouseEnabled(bool enabled)
    {
        foreach (var kv in _originMouseFilters)
        {
            if (!IsInstanceValid(kv.Key)) continue;
            kv.Key.MouseFilter = enabled ? kv.Value : Control.MouseFilterEnum.Ignore;
        }
    }
}