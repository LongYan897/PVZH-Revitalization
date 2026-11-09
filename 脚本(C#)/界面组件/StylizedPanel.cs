using Godot;
using System;

namespace Widgets;

/// <summary>
/// 可切换样式的风格化 Panel（纯外框，不预设任何内容分区）
/// ==================================================================
/// 定位：一个「好看的容器」——只负责画出底版和边框，里面放什么完全由你决定。
///
/// · 7 种样式：Tech 科技切角 / Frame 厚边框 / Glow 霓虹 / Bracket 四角框
///              Rounded 圆角弹窗 / Double 双线画框 / Notch 斜切 HUD
/// · 全部在 _Draw() 里程序化绘制 → 任意尺寸都清晰，不拉伸变形，不依赖贴图
/// · 继承 Container → 子节点自动铺满内容区（内容不会被边框盖住）
/// · [Tool] → 编辑器里拖 size 立刻看到效果
///
/// 用法：添加节点搜 StylizedPanel，或拖 场景(C#)/组件/StylizedPanel.tscn，
///       然后把 Label / TextureRect / 按钮 等任意节点丢进去即可。
/// ==================================================================
[Tool]
[GlobalClass]
public partial class StylizedPanel : Container
{
    /// <summary>面板样式</summary>
    public enum PanelStyle
    {
        /// <summary>科技切角：深蓝渐变 + 细线 + 45°切角 + 扫描线（原「风格化Panel」那套）</summary>
        Tech,
        /// <summary>厚边框：外圈深色宽框 + 内嵌强调线 + 角块</summary>
        Frame,
        /// <summary>霓虹：外发光 + 双色描边</summary>
        Glow,
        /// <summary>四角框：只有四个 L 形角标，中间几乎透明（可叠在背景上）</summary>
        Bracket,
        /// <summary>圆角弹窗：圆角 + 投影，适合对话框</summary>
        Rounded,
        /// <summary>双线画框：外粗线 + 内细线，古典画框感</summary>
        Double,
        /// <summary>斜切 HUD：只切对角两个大角，科幻界面感</summary>
        Notch
    }

    /// <summary>尺寸缩放策略</summary>
    public enum ScaleMode
    {
        /// <summary>固定像素：边框粗细/切角/留白在任何尺寸下都一样（推荐，多个面板放一起才协调）</summary>
        Fixed,
        /// <summary>等比缩放：整体按面板大小缩放（小面板细节不至于太粗）</summary>
        Proportional
    }

    private const float RefW = 400f;
    private const float RefH = 300f;

    // ==================================================================
    // 基础
    // ==================================================================
    private PanelStyle _style = PanelStyle.Tech;

    /// <summary>面板样式</summary>
    [ExportGroup("基础")]
    [Export]
    public PanelStyle Style
    {
        get => _style;
        set { _style = value; QueueRedraw(); QueueSort(); }
    }

    private ScaleMode _scaling = ScaleMode.Fixed;

    /// <summary>尺寸缩放策略：Fixed = 边框永远一样粗；Proportional = 整体等比缩放</summary>
    [Export]
    public ScaleMode Scaling
    {
        get => _scaling;
        set { _scaling = value; QueueRedraw(); QueueSort(); }
    }

    private float _contentPadding = 16f;

    /// <summary>内容区留白（内容离边框多远）</summary>
    [Export(PropertyHint.Range, "0,80,1")]
    public float ContentPadding
    {
        get => _contentPadding;
        set { _contentPadding = Mathf.Max(0f, value); QueueRedraw(); QueueSort(); }
    }

    private bool _autoFitChildren = true;

    /// <summary>子节点自动铺满内容区（关掉就自己摆位置）</summary>
    [Export]
    public bool AutoFitChildren
    {
        get => _autoFitChildren;
        set { _autoFitChildren = value; QueueSort(); }
    }

    private bool _showSizeLabel;

    /// <summary>左上角显示当前尺寸（调试用）</summary>
    [Export]
    public bool ShowSizeLabel
    {
        get => _showSizeLabel;
        set { _showSizeLabel = value; QueueRedraw(); }
    }

    // ==================================================================
    // 填充
    // ==================================================================
    private bool _filled = true;

    /// <summary>是否画底版（关掉 = 只有边框，中间透明）</summary>
    [ExportGroup("填充")]
    [Export]
    public bool Filled
    {
        get => _filled;
        set { _filled = value; QueueRedraw(); }
    }

    private Texture2D _backdrop;

    /// <summary>可选底纹（填 素材(C#)/其他/bg.png 就是原图那层柔光）</summary>
    [Export]
    public Texture2D BackdropTexture
    {
        get => _backdrop;
        set { _backdrop = value; QueueRedraw(); }
    }

    private Color _backdropTint = new(0.067f, 0.242f, 0.341f);

    /// <summary>底纹染色</summary>
    [Export]
    public Color BackdropTint
    {
        get => _backdropTint;
        set { _backdropTint = value; QueueRedraw(); }
    }

    private Color _bodyTop = new("1c3a58");

    /// <summary>底版渐变（上）</summary>
    [Export]
    public Color BodyColorTop
    {
        get => _bodyTop;
        set { _bodyTop = value; QueueRedraw(); }
    }

    private Color _bodyBottom = new("08121e");

    /// <summary>底版渐变（下）</summary>
    [Export]
    public Color BodyColorBottom
    {
        get => _bodyBottom;
        set { _bodyBottom = value; QueueRedraw(); }
    }

    private bool _scanlines = true;

    /// <summary>横向扫描线</summary>
    [Export]
    public bool ShowScanlines
    {
        get => _scanlines;
        set { _scanlines = value; QueueRedraw(); }
    }

    private float _scanlineStep = 4f;

    /// <summary>扫描线间距</summary>
    [Export(PropertyHint.Range, "2,16,0.5")]
    public float ScanlineStep
    {
        get => _scanlineStep;
        set { _scanlineStep = Mathf.Max(2f, value); QueueRedraw(); }
    }

    // ==================================================================
    // 边框
    // ==================================================================
    private Color _frameColor = new Color(0.85f, 0.91f, 0.96f, 0.85f);

    /// <summary>主边框颜色</summary>
    [ExportGroup("边框")]
    [Export]
    public Color FrameColor
    {
        get => _frameColor;
        set { _frameColor = value; QueueRedraw(); }
    }

    private Color _borderColor = new("0d1724");

    /// <summary>外圈深色边框带的颜色</summary>
    [Export]
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; QueueRedraw(); }
    }

    private Color _accent = new("e6c568");

    /// <summary>强调色：切角高光 / 发光 / 角标 / 装饰线</summary>
    [Export]
    public Color AccentColor
    {
        get => _accent;
        set { _accent = value; QueueRedraw(); }
    }

    private float _frameWidth = 1.4f;

    /// <summary>边框线宽</summary>
    [Export(PropertyHint.Range, "0.5,8,0.1")]
    public float FrameWidth
    {
        get => _frameWidth;
        set { _frameWidth = Mathf.Max(0.2f, value); QueueRedraw(); }
    }

    private float _inset = 6f;

    /// <summary>边框离外沿的距离（Frame 样式下就是边框带宽度）</summary>
    [Export(PropertyHint.Range, "0,40,0.5")]
    public float Inset
    {
        get => _inset;
        set { _inset = Mathf.Max(0f, value); QueueRedraw(); QueueSort(); }
    }

    private float _chamfer = 16f;

    /// <summary>切角大小（Tech / Notch 样式用，0 = 直角）</summary>
    [Export(PropertyHint.Range, "0,80,0.5")]
    public float Chamfer
    {
        get => _chamfer;
        set { _chamfer = Mathf.Max(0f, value); QueueRedraw(); QueueSort(); }
    }

    private float _cornerRadius = 10f;

    /// <summary>圆角半径（Rounded 样式用）</summary>
    [Export(PropertyHint.Range, "0,60,0.5")]
    public float CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = Mathf.Max(0f, value); QueueRedraw(); }
    }

    private bool _cornerAccent = true;

    /// <summary>角上的强调装饰（不同样式含义不同）</summary>
    [Export]
    public bool ShowCornerAccent
    {
        get => _cornerAccent;
        set { _cornerAccent = value; QueueRedraw(); }
    }

    private float _glowStrength;

    /// <summary>外发光强度</summary>
    [Export(PropertyHint.Range, "0,3,0.05")]
    public float GlowStrength
    {
        get => _glowStrength;
        set { _glowStrength = Mathf.Max(0f, value); QueueRedraw(); }
    }

    private bool _shadow = true;

    /// <summary>投影（Rounded / Tech / Double 有效）</summary>
    [Export]
    public bool Shadow
    {
        get => _shadow;
        set { _shadow = value; QueueRedraw(); }
    }

    // ==================================================================
    // 生命周期
    // ==================================================================

    public override void _Ready()
    {
        ClipContents = true;
        if (CustomMinimumSize == Vector2.Zero && Size == Vector2.Zero)
        {
            CustomMinimumSize = new Vector2(360f, 240f);
            Size = new Vector2(360f, 240f);
        }
    }

    public override Vector2 _GetMinimumSize() => new(80f, 60f);

    public override void _Notification(int what)
    {
        if (what == NotificationSortChildren)
        {
            LayoutChildren();
        }
        else if (what == NotificationResized)
        {
            QueueRedraw();
            QueueSort();
        }
        else if (what == NotificationThemeChanged || what == NotificationTransformChanged)
        {
            QueueRedraw();
        }
    }

    private void LayoutChildren()
    {
        if (!_autoFitChildren) return;
        Rect2 area = GetContentRect();
        foreach (Node child in GetChildren())
        {
            if (child is Control c) FitChildInRect(c, area);
        }
    }

    // ==================================================================
    // 内容区
    // ==================================================================

    private float ScaleFactor()
        => _scaling == ScaleMode.Fixed
            ? 1f
            : Mathf.Clamp(Mathf.Min(Size.X / RefW, Size.Y / RefH), 0.3f, 3f);

    /// <summary>内容区矩形（子节点会被放在这里）</summary>
    public Rect2 GetContentRect()
    {
        if (Size.X < 4f || Size.Y < 4f) return new Rect2();
        float s = ScaleFactor();
        float margin = Style switch
        {
            PanelStyle.Frame => _inset * 1.6f,
            PanelStyle.Rounded => _inset + _cornerRadius * 0.5f,
            _ => _inset,
        };
        float pad = (margin + _contentPadding) * s + _frameWidth * s * 2f;
        // 面板极小时留白不能超过自身尺寸，否则内容区会翻面
        pad = Mathf.Min(pad, Mathf.Min(Size.X, Size.Y) * 0.35f);
        return new Rect2(Vector2.Zero, Size).Grow(-pad);
    }

    // ==================================================================
    // 绘制
    // ==================================================================

    public override void _Draw()
    {
        Vector2 size = Size;
        if (size.X < 4f || size.Y < 4f) return;

        float s = ScaleFactor();
        Rect2 full = new(Vector2.Zero, size);
        Rect2 content = GetContentRect();

        switch (Style)
        {
            case PanelStyle.Tech: DrawTech(full, content, s); break;
            case PanelStyle.Frame: DrawFrame(full, content, s); break;
            case PanelStyle.Glow: DrawGlow(full, content, s); break;
            case PanelStyle.Bracket: DrawBracket(full, content, s); break;
            case PanelStyle.Rounded: DrawRounded(full, content, s); break;
            case PanelStyle.Double: DrawDouble(full, content, s); break;
            case PanelStyle.Notch: DrawNotch(full, content, s); break;
        }

        if (ShowSizeLabel)
        {
            var f = ThemeDB.FallbackFont;
            int fs = Mathf.Max(8, Mathf.RoundToInt(10f * s));
            DrawString(f, new Vector2(6f * s, 13f * s),
                $"{Mathf.RoundToInt(size.X)}×{Mathf.RoundToInt(size.Y)}",
                HorizontalAlignment.Left, -1, fs, new Color(1f, 1f, 1f, 0.5f));
        }
    }

    // ---------- 1. 科技切角（复刻原「风格化Panel」） ----------
    private void DrawTech(Rect2 full, Rect2 content, float s)
    {
        float inset = Mathf.Max(1f, _inset * s);
        float cham = Mathf.Clamp(_chamfer * s, 0f, Mathf.Min(Size.X, Size.Y) * 0.35f);
        Rect2 body = full.Grow(-inset);

        // 外圈深色带（卡片轮廓）
        FillChamfer(full, _borderColor, _borderColor.Darkened(0.55f), cham + inset);

        if (_glowStrength > 0f) GlowOuter(body, cham, s);
        if (_shadow) DropShadow(body, cham, s);

        DrawBody(body, cham, s);

        // 内阴影
        StrokeChamfer(body, cham, new Color(0f, 0f, 0f, 0.25f), 10f * s);
        StrokeChamfer(body, cham, new Color(0f, 0f, 0f, 0.10f), 22f * s);

        // 细边框
        StrokeChamfer(body, cham, _frameColor, Mathf.Max(1f, _frameWidth * s));

        // 切角高光
        if (_cornerAccent)
            CornerAccents(body, cham, _accent, Mathf.Max(_frameWidth * 2.4f * s, 2.2f * s), cham * 0.85f);
    }

    // ---------- 2. 厚边框 ----------
    private void DrawFrame(Rect2 full, Rect2 content, float s)
    {
        float band = Mathf.Max(2f, _inset * 1.6f * s);
        float cham = Mathf.Clamp(_chamfer * s * 0.5f, 0f, Mathf.Min(Size.X, Size.Y) * 0.25f);
        Rect2 body = full.Grow(-band);

        if (_glowStrength > 0f) GlowOuter(full, cham + band, s, 1.2f);
        if (_shadow) DropShadow(full, cham + band, s);

        // 外框带（上亮下暗）
        FillChamfer(full, _borderColor.Lightened(0.10f), _borderColor.Darkened(0.45f), cham + band);

        DrawBody(body, cham, s);

        // 内沿强调线 + 外侧细线
        StrokeChamfer(body, cham, _accent, Mathf.Max(1.5f, _frameWidth * 1.8f * s));
        StrokeChamfer(body.Grow(-3f * s), cham, new Color(_frameColor.R, _frameColor.G, _frameColor.B, 0.28f),
            Mathf.Max(1f, _frameWidth * s));

        // 四个角块（四角对称）
        if (_cornerAccent)
        {
            float b = band * 0.62f;
            float block = Mathf.Min(band * 1.5f, Mathf.Min(Size.X, Size.Y) * 0.16f);
            float dot = block * 0.34f;
            Color c1 = _accent;
            Color c2 = _accent.Darkened(0.30f);
            Vector2[] corners =
            {
                new(b, b),
                new(Size.X - b - block, b),
                new(b, Size.Y - b - block),
                new(Size.X - b - block, Size.Y - b - block),
            };
            foreach (Vector2 p in corners)
            {
                DrawRect(new Rect2(p, new Vector2(block, block)), c2);
                DrawRect(new Rect2(p + new Vector2(block - dot, block - dot), new Vector2(dot, dot)), c1);
            }
        }
    }

    // ---------- 3. 霓虹发光 ----------
    private void DrawGlow(Rect2 full, Rect2 content, float s)
    {
        float inset = Mathf.Max(2f, _inset * s);
        float cham = Mathf.Clamp(_chamfer * s, 0f, Mathf.Min(Size.X, Size.Y) * 0.3f);
        Rect2 body = full.Grow(-inset);

        DrawBody(body, cham, s, 0.86f);

        // 外发光：往内叠几层，越靠外越淡
        float strength = Mathf.Max(_glowStrength, 1f);
        for (int i = 4; i >= 1; i--)
        {
            float grow = inset * 0.9f * i / 4f;
            Color c = A(_accent, 0.075f * strength * (1f - i * 0.16f));
            StrokeChamfer(body.Grow(grow), cham, c, (2f + i * 2.6f) * s);
        }

        // 主描边 + 内侧细白线
        StrokeChamfer(body, cham, _accent, Mathf.Max(1.5f, _frameWidth * 1.9f * s));
        StrokeChamfer(body.Grow(-3f * s), cham, A(_frameColor, 0.35f), Mathf.Max(1f, _frameWidth * s));

        // 上下接口短线
        float seg = Mathf.Min(Size.X * 0.18f, 70f * s);
        float y1 = body.Position.Y, y2 = body.End.Y;
        DrawLine(new Vector2(body.Position.X + cham + 6f * s, y1),
                 new Vector2(body.Position.X + cham + 6f * s + seg, y1),
                 A(_accent, 0.9f), Mathf.Max(1.5f, 2.4f * s), true);
        DrawLine(new Vector2(body.End.X - cham - 6f * s - seg, y2),
                 new Vector2(body.End.X - cham - 6f * s, y2),
                 A(_accent, 0.9f), Mathf.Max(1.5f, 2.4f * s), true);
    }

    // ---------- 4. 四角框 ----------
    private void DrawBracket(Rect2 full, Rect2 content, float s)
    {
        float inset = Mathf.Max(2f, _inset * s);
        Rect2 body = full.Grow(-inset);

        if (_filled) DrawBody(body, 0f, s, 0.55f);
        if (_glowStrength > 0f) GlowOuter(body, 0f, s);

        // 极淡的整体轮廓
        StrokeChamfer(body, 0f, new Color(_frameColor.R, _frameColor.G, _frameColor.B, 0.16f),
            Mathf.Max(1f, _frameWidth * s));

        // 四个 L 形角标
        float len = Mathf.Min(Mathf.Min(body.Size.X, body.Size.Y) * 0.22f, 56f * s);
        float w = Mathf.Max(2f, 2.6f * s);
        Color c = _accent;
        float x0 = body.Position.X, y0 = body.Position.Y, x1 = body.End.X, y1 = body.End.Y;
        float tick = w * 1.6f;

        DrawLine(new Vector2(x0, y0 + len), new Vector2(x0, y0), c, w, true);
        DrawLine(new Vector2(x0, y0), new Vector2(x0 + len, y0), c, w, true);
        DrawLine(new Vector2(x1 - len, y0), new Vector2(x1, y0), c, w, true);
        DrawLine(new Vector2(x1, y0), new Vector2(x1, y0 + len), c, w, true);
        DrawLine(new Vector2(x1, y1 - len), new Vector2(x1, y1), c, w, true);
        DrawLine(new Vector2(x1, y1), new Vector2(x1 - len, y1), c, w, true);
        DrawLine(new Vector2(x0 + len, y1), new Vector2(x0, y1), c, w, true);
        DrawLine(new Vector2(x0, y1), new Vector2(x0, y1 - len), c, w, true);

        if (_cornerAccent)
        {
            foreach (Vector2 p in new[] { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x0, y1), new Vector2(x1, y1) })
                DrawRect(new Rect2(p - new Vector2(tick, tick) * 0.5f, new Vector2(tick, tick)), c);
        }
    }

    // ---------- 5. 圆角弹窗 ----------
    private void DrawRounded(Rect2 full, Rect2 content, float s)
    {
        float pad = Mathf.Max(2f, _inset * s);
        Rect2 body = full.Grow(-pad);
        int radius = Mathf.RoundToInt(Mathf.Clamp(_cornerRadius * s, 0f, Mathf.Min(body.Size.X, body.Size.Y) * 0.4f));

        var styleBox = new StyleBoxFlat
        {
            BgColor = _filled ? _bodyTop : new Color(0f, 0f, 0f, 0f),
            BorderColor = _frameColor,
            DrawCenter = _filled,
        };
        styleBox.SetCornerRadiusAll(radius);
        int bw = Mathf.Max(1, Mathf.RoundToInt(_frameWidth * s));
        styleBox.SetBorderWidthAll(bw);
        if (_shadow)
        {
            styleBox.ShadowColor = new Color(0f, 0f, 0f, 0.38f);
            styleBox.ShadowSize = Mathf.RoundToInt(10f * s);
            styleBox.ShadowOffset = new Vector2(0f, 3f * s);
        }
        else
        {
            styleBox.ShadowSize = 0;
        }
        DrawStyleBox(styleBox, body);

        // 顶部高光
        var hl = new StyleBoxFlat { BgColor = new Color(0f, 0f, 0f, 0f) };
        hl.SetCornerRadiusAll(Mathf.Max(0, radius - bw));
        hl.BorderWidthTop = Mathf.Max(1, Mathf.RoundToInt(1.2f * s));
        hl.BorderColor = new Color(1f, 1f, 1f, 0.12f);
        DrawStyleBox(hl, body.Grow(-bw));

        // 底部压暗，模拟竖向渐变（多层细分，避免出现硬边）
        float maxFade = Mathf.Min(body.Size.Y * 0.42f, 36f * s);
        for (int i = 1; i <= 12; i++)
        {
            var band = new StyleBoxFlat
            {
                BgColor = new Color(0f, 0f, 0f, 0f),
                DrawCenter = false,
                BorderColor = A(_bodyBottom, 0.045f),
            };
            band.SetCornerRadiusAll(radius);
            band.BorderWidthBottom = Mathf.Max(1, Mathf.RoundToInt(maxFade * i / 12f));
            DrawStyleBox(band, body);
        }

        if (_glowStrength > 0f) GlowOuter(body, 0f, s, 0.8f);
    }

    // ---------- 6. 双线画框 ----------
    private void DrawDouble(Rect2 full, Rect2 content, float s)
    {
        float inset = Mathf.Max(2f, _inset * s);
        Rect2 body = full.Grow(-inset);

        if (_shadow) DropShadow(body, 0f, s);
        DrawBody(body, 0f, s);

        // 外粗线 / 间隔暗带 / 内细线
        StrokeChamfer(body, 0f, _borderColor.Lightened(0.18f), Mathf.Max(2f, 2.4f * s));
        StrokeChamfer(body.Grow(-4f * s), 0f, new Color(0f, 0f, 0f, 0.25f), 3f * s);
        StrokeChamfer(body.Grow(-8f * s), 0f, A(_frameColor, 0.8f), Mathf.Max(1f, _frameWidth * s));

        if (_cornerAccent)
        {
            float l = Mathf.Min(body.Size.X, body.Size.Y) * 0.12f;
            Color c = A(_accent, 0.9f);
            float w = Mathf.Max(1f, 1.4f * s);
            DrawLine(body.Position, body.Position + new Vector2(l, l), c, w, true);
            DrawLine(new Vector2(body.End.X, body.Position.Y), new Vector2(body.End.X - l, body.Position.Y + l), c, w, true);
            DrawLine(body.End, body.End - new Vector2(l, l), c, w, true);
            DrawLine(new Vector2(body.Position.X, body.End.Y), new Vector2(body.Position.X + l, body.End.Y - l), c, w, true);
        }
    }

    // ---------- 7. 斜切 HUD ----------
    private void DrawNotch(Rect2 full, Rect2 content, float s)
    {
        float inset = Mathf.Max(1f, _inset * s);
        float cut = Mathf.Clamp(_chamfer * s * 2.2f, 0f, Mathf.Min(Size.X, Size.Y) * 0.4f);
        Rect2 body = full.Grow(-inset);

        Vector2[] outer = ChamferPoints(full, cut, 0f, cut, 0f);
        FillPoly(outer, _borderColor, _borderColor.Darkened(0.55f), full);

        if (_glowStrength > 0f) GlowOuter(body, cut, s);

        Vector2[] inner = ChamferPoints(body, cut, 0f, cut, 0f);
        DrawBodyPoly(inner, body, s);
        StrokePoly(inner, _frameColor, Mathf.Max(1f, _frameWidth * s));

        if (_cornerAccent)
        {
            Color c = _accent;
            float w = Mathf.Max(2f, 2.6f * s);
            DrawLine(new Vector2(body.Position.X, body.Position.Y + cut),
                     new Vector2(body.Position.X + cut, body.Position.Y), c, w, true);
            DrawLine(new Vector2(body.End.X - cut, body.End.Y),
                     new Vector2(body.End.X, body.End.Y - cut), c, w, true);

            float l = Mathf.Min(body.Size.X, body.Size.Y) * 0.1f;
            DrawLine(new Vector2(body.End.X, body.Position.Y), new Vector2(body.End.X - l, body.Position.Y), A(c, 0.65f), w * 0.7f, true);
            DrawLine(new Vector2(body.Position.X, body.End.Y), new Vector2(body.Position.X + l, body.End.Y), A(c, 0.65f), w * 0.7f, true);
        }
    }

    // ==================================================================
    // 底版 & 通用绘制
    // ==================================================================

    private void DrawBody(Rect2 body, float chamfer, float s, float bodyAlpha = 1f)
    {
        if (!_filled) return;
        if (_backdrop != null)
        {
            FillChamferTextured(body, chamfer, _backdrop,
                new Color(_backdropTint.R, _backdropTint.G, _backdropTint.B, bodyAlpha));
            FillChamfer(body, new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0.42f * bodyAlpha), chamfer);
        }
        else
        {
            FillChamfer(body, A(_bodyTop, _bodyTop.A * bodyAlpha),
                A(_bodyBottom, _bodyBottom.A * bodyAlpha), chamfer);
        }

        if (_scanlines) DrawScanlines(body, chamfer, s);
    }

    private void DrawBodyPoly(Vector2[] pts, Rect2 body, float s)
    {
        if (!_filled) return;
        FillPoly(pts, _bodyTop, _bodyBottom, body);
        if (_scanlines) DrawScanlines(body, 0f, s);
    }

    private void DrawScanlines(Rect2 body, float chamfer, float s)
    {
        float step = Mathf.Max(2f, _scanlineStep * (Size.Y / RefH));
        Color lineColor = new(1f, 1f, 1f, 0.022f);
        float left = body.Position.X + chamfer * 0.7f;
        float width = body.Size.X - chamfer * 1.4f;
        if (width <= 0f) return;
        for (float y = body.Position.Y; y < body.End.Y; y += step)
            DrawRect(new Rect2(left, y, width, Mathf.Max(1f, step * 0.35f)), lineColor);
    }

    /// <summary>向内的投影（画在控件内部，不会溢出到邻居）</summary>
    private void DropShadow(Rect2 body, float chamfer, float s)
    {
        for (int i = 0; i < 3; i++)
        {
            float grow = (3f + i * 4f) * s;
            StrokeChamfer(body.Grow(-grow), chamfer, new Color(0f, 0f, 0f, 0.10f - i * 0.03f), (6f + i * 4f) * s);
        }
    }

    /// <summary>外发光（向内叠几层描边）</summary>
    private void GlowOuter(Rect2 body, float chamfer, float s, float mul = 1f)
    {
        float strength = _glowStrength * mul;
        if (strength <= 0f) return;
        for (int i = 3; i >= 1; i--)
        {
            Color c = A(_accent, 0.05f * strength);
            StrokeChamfer(body.Grow(i * 2.5f * s), chamfer, c, (2f + i * 2f) * s);
        }
    }

    // ----- 几何 -----

    private static Color A(Color c, float a) => new(c.R, c.G, c.B, a);

    private static Vector2[] ChamferPoints(Rect2 r, float c) => ChamferPoints(r, c, c, c, c);

    private static Vector2[] ChamferPoints(Rect2 r, float tl, float tr, float br, float bl)
    {
        float x0 = r.Position.X, y0 = r.Position.Y, x1 = r.End.X, y1 = r.End.Y;
        float lim = Mathf.Min(r.Size.X, r.Size.Y) * 0.45f;
        tl = Mathf.Clamp(tl, 0f, lim);
        tr = Mathf.Clamp(tr, 0f, lim);
        br = Mathf.Clamp(br, 0f, lim);
        bl = Mathf.Clamp(bl, 0f, lim);
        return new[]
        {
            new Vector2(x0 + tl, y0), new Vector2(x1 - tr, y0),
            new Vector2(x1, y0 + tr), new Vector2(x1, y1 - br),
            new Vector2(x1 - br, y1), new Vector2(x0 + bl, y1),
            new Vector2(x0, y1 - bl), new Vector2(x0, y0 + tl)
        };
    }

    private void FillChamfer(Rect2 r, Color top, Color bottom, float c) => FillPoly(ChamferPoints(r, c), top, bottom, r);

    private void FillPoly(Vector2[] pts, Color top, Color bottom, Rect2 r)
    {
        if (r.Size.X <= 0f || r.Size.Y <= 0f) return;
        Color[] cols = new Color[pts.Length];
        for (int i = 0; i < pts.Length; i++)
            cols[i] = top.Lerp(bottom, Mathf.InverseLerp(r.Position.Y, r.End.Y, pts[i].Y));
        DrawPolygon(pts, cols);
    }

    private void FillChamferTextured(Rect2 r, float c, Texture2D tex, Color tint)
    {
        if (tex == null || r.Size.X <= 0f || r.Size.Y <= 0f) return;
        Vector2[] pts = ChamferPoints(r, c);
        Vector2[] uvs = new Vector2[pts.Length];
        Color[] cols = new Color[pts.Length];
        for (int i = 0; i < pts.Length; i++)
        {
            uvs[i] = new Vector2((pts[i].X - r.Position.X) / r.Size.X, (pts[i].Y - r.Position.Y) / r.Size.Y);
            cols[i] = tint;
        }
        DrawPolygon(pts, cols, uvs, tex);
    }

    private void StrokeChamfer(Rect2 r, float c, Color col, float w)
    {
        if (w <= 0f || r.Size.X <= 0f || r.Size.Y <= 0f) return;
        StrokePoly(ChamferPoints(r, c), col, w);
    }

    private void StrokePoly(Vector2[] pts, Color col, float w)
    {
        if (w <= 0f) return;
        Vector2[] loop = new Vector2[pts.Length + 1];
        Array.Copy(pts, loop, pts.Length);
        loop[^1] = pts[0];
        DrawPolyline(loop, col, w, true);
    }

    private void CornerAccents(Rect2 r, float c, Color col, float w, float len)
    {
        if (c <= 0.5f) return;
        len = Mathf.Clamp(len, 1f, c);
        float x0 = r.Position.X, y0 = r.Position.Y, x1 = r.End.X, y1 = r.End.Y;
        Color c1 = A(col, 0.95f);
        DrawLine(new Vector2(x0, y0 + c - len), new Vector2(x0 + c - len, y0), c1, w, true);
        DrawLine(new Vector2(x1 - c + len, y0), new Vector2(x1, y0 + c - len), c1, w, true);
        DrawLine(new Vector2(x1, y1 - c + len), new Vector2(x1 - c + len, y1), c1, w, true);
        DrawLine(new Vector2(x0 + c - len, y1), new Vector2(x0, y1 - c + len), c1, w, true);
    }
}
