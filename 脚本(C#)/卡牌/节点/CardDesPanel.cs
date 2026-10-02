using Controller;
using Godot;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Card;

[GlobalClass]
public partial class CardDesPanel : Panel
{
    private static readonly PackedScene Scene =
        GD.Load<PackedScene>("res://场景(C#)/卡牌描述.tscn");

    private static readonly Regex LinkRegex =
        new Regex(@"_([^_]+)_", RegexOptions.Compiled);

    private RichTextLabel _rtl;
    private Label _title;

    public override void _Ready()
    {
        _rtl = GetNode<RichTextLabel>("%描述");
        _title = GetNode<Label>("%标题");

        MouseFilter = MouseFilterEnum.Stop;
        _rtl.MouseFilter = MouseFilterEnum.Ignore;
        _title.MouseFilter = MouseFilterEnum.Ignore;
    }

    public async Task SetContent(string title, string desc)
    {
        _title.Text = title;
        await CardDes.AppendWithIcons(_rtl, StripLinks(desc), convertLinks: false);
    }

    private static string StripLinks(string text) =>
        LinkRegex.Replace(text, m => m.Groups[1].Value);

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            QueueFree();
            AcceptEvent();
        }
    }

    public static async void ShowAt(string title, string desc, Vector2 globalPos)
    {
        var panel = Scene.Instantiate<CardDesPanel>();
        Main.DesLayer.AddChild(panel);
        panel.ZIndex = 2048;

        await panel.SetContent(title, desc);

        panel.PositionPanelAbove(globalPos);
    }
    private void PositionPanelAbove(Vector2 globalPos)
    {
        CallDeferred(nameof(DoPosition), globalPos);
    }

    private void DoPosition(Vector2 globalPos)
    {
        Vector2 size = Size;
        Vector2 pos = globalPos - new Vector2(size.X / 2f, size.Y);
        Position = pos;

        var viewport = GetViewportRect().Size;
        if (Position.X < 0) Position = new Vector2(0, Position.Y);
        if (Position.X + size.X > viewport.X)
            Position = new Vector2(viewport.X - size.X, Position.Y);
        if (Position.Y < 0) Position = new Vector2(Position.X, 0);
    }
}