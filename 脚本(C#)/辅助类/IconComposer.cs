using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pack;


[GlobalClass]
public partial class IconComposer : Node
{
    public static IconComposer Instance { get; private set; }

    private SubViewport _viewport;
    private Label _label;
    private readonly Dictionary<string, Texture2D> _cache = new();

    public override void _Ready()
    {
        Instance = this;

        _viewport = new SubViewport
        {
            Size = new Vector2I(32, 32),
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            RenderTargetClearMode = SubViewport.ClearMode.Always
        };
        AddChild(_viewport);

        _label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _label.AddThemeFontSizeOverride("font_size", 24);
        _label.AddThemeFontOverride("font",GD.Load<Font>("res://素材/字体/CAFBL___.ttf"));
        _label.Position += new Vector2(0, 1);
        _label.Size = new Vector2(32, 32);
        _viewport.AddChild(_label);
    }

    public async Task<Texture2D> ComposeAsync(
        string iconPath,
        string text,
        Color? color = null,
        Color? outlineColor = null,
        int outlineSize = 0)
    {
        string key = $"{iconPath}#{text}#{color}#{outlineColor}#{outlineSize}";
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        _label.AddThemeColorOverride("font_color", color ?? Colors.White);
        _label.AddThemeColorOverride("font_outline_color", outlineColor ?? Colors.Transparent);
        _label.AddThemeConstantOverride("outline_size", outlineSize);

        _label.Text = text;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var textTex = _viewport.GetTexture().GetImage();

        var iconTex = GD.Load<Texture2D>(iconPath);
        if (iconTex == null) return null;

        Image icon = iconTex.GetImage();
        if (icon.GetFormat() != Image.Format.Rgba8)
            icon.Convert(Image.Format.Rgba8);
        icon.Resize(32, 32, Image.Interpolation.Bilinear);

        int x = (icon.GetWidth() - textTex.GetWidth()) / 2;
        int y = (icon.GetHeight() - textTex.GetHeight()) / 2;
        icon.BlendRect(textTex, new Rect2I(0, 0, textTex.GetWidth(), textTex.GetHeight()),
                       new Vector2I(x, y));

        var result = ImageTexture.CreateFromImage(icon);
        _cache[key] = result;
        return result;
    }


    public void ClearCache() => _cache.Clear();
}
