using Controller;
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Card;

[GlobalClass]
public partial class CardDesPanel : Panel
{
    private static readonly PackedScene Scene =
        GD.Load<PackedScene>("res://场景(C#)/卡牌描述.tscn");

    private static readonly List<CardDesPanel> OpenPanels = new();

    private const float PanelScale = 0.8f;

    private RichTextLabel _rtl;
    private Control _blocker;
    private bool _closing;

    public override void _Ready()
    {
        _rtl = GetNode<RichTextLabel>("Control/RichTextLabel");
        _blocker = GetNode<Control>("Control");

        MouseFilter = MouseFilterEnum.Ignore;
        _rtl.MouseFilter = MouseFilterEnum.Stop;
        _rtl.MetaClicked += OnKeywordClicked;

        _blocker.MouseFilter = MouseFilterEnum.Ignore;

        _rtl.FitContent = true;
        _rtl.ScrollActive = false;

        Scale = Vector2.Zero;
        Modulate = new Color(1, 1, 1, 0);

        OpenPanels.Add(this);
        ZIndex = 2048 + OpenPanels.Count - 1;
    }

    public override void _Input(InputEvent @event)
    {
        if (_closing) return;
        if (@event is not InputEventMouseButton mb || !mb.Pressed) return;
        if (OpenPanels[^1] != this) return;
        if (GetGlobalRect().HasPoint(mb.GlobalPosition)) return;

        GetViewport().SetInputAsHandled();
        Close();
    }

    private void OnKeywordClicked(Variant meta)
    {
        string key = meta.AsString();

        if (CardModel.HasTemplate(key))
        {
            GetViewport().SetInputAsHandled();
            var template = CardModel.GetTemplate(key);
            if (template != null)
                CardDes.ExchangeDescription(template);
            return;
        }

        if (CardDes.TryGetKeywordDescription(null, key, out string desc))
        {
            GetViewport().SetInputAsHandled();

            var globalPos = _rtl.GlobalPosition + _rtl.GetLocalMousePosition();

            ShowAt(desc, globalPos);
        }
    }

    private const float MaxTextWidth = 400f;
    private const float PadX = 20f;
    private const float PadY = 16f;

    private void FitToContent()
    {
        _blocker.AnchorLeft = 0;
        _blocker.AnchorTop = 0;
        _blocker.AnchorRight = 1;
        _blocker.AnchorBottom = 1;
        _blocker.OffsetLeft = 0;
        _blocker.OffsetTop = 0;
        _blocker.OffsetRight = 0;
        _blocker.OffsetBottom = 0;
        _blocker.Position = Vector2.Zero;

        _rtl.AnchorLeft = 0;
        _rtl.AnchorTop = 0;
        _rtl.AnchorRight = 0;
        _rtl.AnchorBottom = 0;

        _rtl.FitContent = true;
        _rtl.ScrollActive = false;

        float h = _rtl.GetContentHeight();

        _rtl.Position = new Vector2(PadX, PadY);
        _rtl.Size = new Vector2(MaxTextWidth, h);

        Size = new Vector2(MaxTextWidth + PadX * 2f, h + PadY * 2f);
    }

    private void PlayOpen()
    {
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetTrans(Tween.TransitionType.Back);
        tween.SetEase(Tween.EaseType.Out);

        tween.TweenProperty(this, "scale", new Vector2(PanelScale, PanelScale), 0.35f);
        tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.2f);
    }

    private void Close()
    {
        if (_closing) return;
        _closing = true;

        OpenPanels.Remove(this);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetTrans(Tween.TransitionType.Back);
        tween.SetEase(Tween.EaseType.In);

        tween.TweenProperty(this, "scale", Vector2.Zero, 0.25f);
        tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.2f);

        tween.Finished += QueueFree;
    }

    public async Task SetContent(string desc)
    {
        await CardDes.AppendWithIcons(_rtl, desc, convertLinks: true);
        FitToContent();
    }

    public static async void ShowAt(string desc, Vector2 globalPos)
    {
        var panel = Scene.Instantiate<CardDesPanel>();
        Main.DesLayer.AddChild(panel);

        await panel.SetContent(desc);
        panel.PositionPanelAbove(globalPos);
        panel.PlayOpen();
    }

    private void PositionPanelAbove(Vector2 globalPos)
    {
        CallDeferred(nameof(DoPosition), globalPos);
    }

    private void DoPosition(Vector2 globalPos)
    {
        Vector2 size = Size * PanelScale;
        Vector2 pos = globalPos - new Vector2(size.X / 2f, size.Y + 30f);
        Position = pos;

        var viewport = GetViewportRect().Size;
        if (Position.X < 0) Position = new Vector2(0, Position.Y);
        if (Position.X + size.X > viewport.X)
            Position = new Vector2(viewport.X - size.X, Position.Y);
        if (Position.Y < 0) Position = new Vector2(Position.X, 0);
    }
}