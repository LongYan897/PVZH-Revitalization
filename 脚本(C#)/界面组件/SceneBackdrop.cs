using Godot;
using System;

namespace Widgets;

/// <summary>
/// 子场景背景特效层（1280×720）
/// ==================================================================
/// 组成（从下往上）：
///   1. 竖直渐变底  — 深蓝黑，保证前面的面板/文字有对比度
///   2. 右侧斜纹    — 45° 细斜纹 + 右侧渐隐，做出"纹理"质感
///   3. 底部辉光    — 椭圆形径向光，可随阵营换色（植物绿 / 僵尸紫 / 默认火红）
///   4. 火星粒子    — 从底边升起的余烬，加法混合
///   5. 偶发光晕    — 每隔 6~12 秒扫过一道柔光（"时不时的光线光晕"）
///   6. 细节        — 底部两条发光细线 + 极淡扫描线
///
/// 全部程序化绘制，不依赖任何贴图（粒子用现成的圆形渐变粒子.png）。
/// ==================================================================
[Tool]
[GlobalClass]
public partial class SceneBackdrop : Control
{
    // ==================================================================
    // 可调参数
    // ==================================================================
    private Color _accent = new("ff5a3c");

    /// <summary>强调色：底部辉光 / 火星 / 光晕 / 装饰线（默认火红，按阵营会自动切换）</summary>
    [ExportGroup("整体")]
    [Export]
    public Color AccentColor
    {
        get => _accent;
        set { _accent = value; ApplyAccent(); QueueRedraw(); }
    }

    private Color _bodyTop = new("0b1220");

    /// <summary>背景渐变（上）</summary>
    [Export]
    public Color BodyColorTop
    {
        get => _bodyTop;
        set { _bodyTop = value; QueueRedraw(); }
    }

    private Color _bodyBottom = new("05080e");

    /// <summary>背景渐变（下）</summary>
    [Export]
    public Color BodyColorBottom
    {
        get => _bodyBottom;
        set { _bodyBottom = value; QueueRedraw(); }
    }

    private float _glowStrength = 1f;

    /// <summary>底部辉光强度（0 = 关）</summary>
    [Export(PropertyHint.Range, "0,2,0.05")]
    public float GlowStrength
    {
        get => _glowStrength;
        set { _glowStrength = Mathf.Max(0f, value); QueueRedraw(); }
    }

    private bool _breathing = true;

    /// <summary>辉光缓慢呼吸</summary>
    [Export(PropertyHint.Range, "0,1,0.01")]
    public bool Breathing
    {
        get => _breathing;
        set { _breathing = value; }
    }

    // ---- 纹理 ----
    private bool _showTexture = true;

    /// <summary>右侧斜纹</summary>
    [ExportGroup("纹理")]
    [Export]
    public bool ShowTexture
    {
        get => _showTexture;
        set { _showTexture = value; QueueRedraw(); }
    }

    private float _textureAlpha = 0.028f;

    /// <summary>斜纹透明度</summary>
    [Export(PropertyHint.Range, "0,0.3,0.005")]
    public float TextureAlpha
    {
        get => _textureAlpha;
        set { _textureAlpha = Mathf.Max(0f, value); QueueRedraw(); }
    }

    private float _textureSpacing = 15f;

    /// <summary>斜纹间距</summary>
    [Export(PropertyHint.Range, "4,80,1")]
    public float TextureSpacing
    {
        get => _textureSpacing;
        set { _textureSpacing = Mathf.Max(2f, value); QueueRedraw(); }
    }

    // ---- 粒子 ----
    private bool _showParticles = true;

    /// <summary>火星粒子</summary>
    [ExportGroup("粒子")]
    [Export]
    public bool ShowParticles
    {
        get => _showParticles;
        set { _showParticles = value; if (_particles != null) _particles.Emitting = value; }
    }

    private int _particleAmount = 80;

    /// <summary>粒子数量（移动端建议 ≤100）</summary>
    [Export(PropertyHint.Range, "0,300,1")]
    public int ParticleAmount
    {
        get => _particleAmount;
        set { _particleAmount = Mathf.Clamp(value, 0, 300); if (_particles != null) _particles.Amount = _particleAmount; }
    }

    // ---- 光晕 ----
    private bool _showSweep = false;

    /// <summary>偶发扫光</summary>
    [ExportGroup("光晕")]
    [Export]
    public bool ShowSweep
    {
        get => _showSweep;
        set => _showSweep = value;
    }

    private float _sweepIntervalMin = 6f;

    /// <summary>扫光最短间隔（秒）</summary>
    [Export(PropertyHint.Range, "2,60,0.5")]
    public float SweepIntervalMin
    {
        get => _sweepIntervalMin;
        set => _sweepIntervalMin = Mathf.Max(1f, value);
    }

    private float _sweepIntervalMax = 12f;

    /// <summary>扫光最长间隔（秒）</summary>
    [Export(PropertyHint.Range, "2,120,0.5")]
    public float SweepIntervalMax
    {
        get => _sweepIntervalMax;
        set => _sweepIntervalMax = Mathf.Max(_sweepIntervalMin, value);
    }

    private float _sweepPeakAlpha = 0.11f;

    /// <summary>扫光峰值亮度</summary>
    [Export(PropertyHint.Range, "0,0.5,0.005")]
    public float SweepPeakAlpha
    {
        get => _sweepPeakAlpha;
        set => _sweepPeakAlpha = Mathf.Max(0f, value);
    }

    // ==================================================================
    // 内部状态
    // ==================================================================
    private Texture2D _glowTex;      // 径向渐变（辉光/扫光共用）
    private GpuParticles2D _particles;
    private readonly Random _rng = new(20240701);

    private float _time;              // 呼吸用
    private float _sweepTimer;        // 距离下一次扫光
    private float _sweepT = -1f;      // 0~1 扫光进度，<0 表示没在扫
    private bool _sweepRightToLeft;

    private const float SweepDuration = 1.7f;

    // ==================================================================
    // 生命周期
    // ==================================================================
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        BuildGlowTexture();

        if (!Engine.IsEditorHint())
        {
            BuildParticles();
            _sweepTimer = RandomSweepDelay();
            SetProcess(true);
        }
        else
        {
            SetProcess(false);
        }
    }

    public override void _ExitTree()
    {
        if (_particles != null && GodotObject.IsInstanceValid(_particles))
            _particles.QueueFree();
        _particles = null;
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;

        // 呼吸：0.5s 周期取正弦，幅度很小，只影响辉光透明度
        // 扫光计时
        if (_showSweep)
        {
            if (_sweepT >= 0f)
            {
                _sweepT += (float)delta / SweepDuration;
                if (_sweepT >= 1f)
                {
                    _sweepT = -1f;
                    _sweepTimer = RandomSweepDelay();
                }
            }
            else
            {
                _sweepTimer -= (float)delta;
                if (_sweepTimer <= 0f)
                {
                    _sweepT = 0f;
                    _sweepRightToLeft = _rng.Next(2) == 0;
                }
            }
        }

        QueueRedraw();
    }

    private float RandomSweepDelay()
        => _sweepIntervalMin + (float)_rng.NextDouble() * Mathf.Max(0.01f, _sweepIntervalMax - _sweepIntervalMin);

    // ==================================================================
    // 对外：按阵营换色
    // ==================================================================
    /// <summary>植物 / 僵尸 / 中性 三套配色</summary>
    public void SetCampTheme(Card.Camp camp)
    {
        switch (camp)
        {
            case Card.Camp.Plant:
                SetAccent(new Color("4ef08a"));
                break;
            case Card.Camp.Zombie:
                SetAccent(new Color("a05cff"));
                break;
            default:
                SetAccent(new Color("ff5a3c"));
                break;
        }
    }

    /// <summary>直接换强调色（辉光/火星/扫光/装饰线都跟着变）</summary>
    public void SetAccent(Color accent)
    {
        _accent = accent;
        ApplyAccent();
        QueueRedraw();
    }

    private void ApplyAccent()
    {
        if (_particles == null || !GodotObject.IsInstanceValid(_particles)) return;
        if (_particles.ProcessMaterial is ParticleProcessMaterial mat)
            mat.ColorRamp = BuildParticleRamp(_accent);
    }

    // ==================================================================
    // 构建资源
    // ==================================================================
    private void BuildGlowTexture()
    {
        var gradient = new Gradient();
        gradient.SetOffset(0, 0f);
        gradient.SetColor(0, new Color(1f, 1f, 1f, 1f));
        gradient.SetOffset(1, 1f);
        gradient.SetColor(1, new Color(1f, 1f, 1f, 0f));

        _glowTex = new GradientTexture2D
        {
            Gradient = gradient,
            Width = 256,
            Height = 256,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(0.5f, 0f),
        };
    }

    private GradientTexture1D BuildParticleRamp(Color c)
    {
        var g = new Gradient
        {
            Offsets = new[] { 0f, 0.18f, 0.75f, 1f },
            Colors = new[]
            {
                new Color(c.R, c.G, c.B, 0f),
                new Color(c.R, c.G, c.B, 1f),
                new Color(c.R, c.G, c.B, 0.55f),
                new Color(c.R, c.G, c.B, 0f),
            },
        };
        return new GradientTexture1D { Gradient = g };
    }

    private void BuildParticles()
    {
        var tex = ResourceLoader.Exists("res://素材/粒子特效/圆形渐变粒子.png")
            ? ResourceLoader.Load<Texture2D>("res://素材/粒子特效/圆形渐变粒子.png")
            : null;

        var mat = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            EmissionBoxExtents = new Vector3(Size.X * 0.5f, 6f, 0f),
            Direction = new Vector3(0f, -1f, 0f),
            Spread = 22f,
            InitialVelocityMin = 22f,
            InitialVelocityMax = 78f,
            Gravity = new Vector3(0f, -9f, 0f),
            ScaleMin = 0.09f,
            ScaleMax = 0.26f,
            LifetimeRandomness = 0.45f,
            ColorRamp = BuildParticleRamp(_accent),
        };
        mat.Set("particle_flag_disable_z", true);

        _particles = new GpuParticles2D
        {
            Name = "火星粒子",
            Texture = tex,
            ProcessMaterial = mat,
            Amount = _particleAmount,
            Lifetime = 6.5f,
            Preprocess = 3f,
            Emitting = _showParticles,
            Position = new Vector2(Size.X * 0.5f, Size.Y + 8f),
            VisibilityRect = new Rect2(-Size.X, -Size.Y * 2f, Size.X * 3f, Size.Y * 3f),
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
        };
        AddChild(_particles);
        _particles.Owner = null;
        _particles.Scale = new Vector2(1f, 1f);
    }

    // ==================================================================
    // 绘制
    // ==================================================================
    public override void _Draw()
    {
        Vector2 size = Size;
        if (size.X < 4f || size.Y < 4f) return;

        DrawBase(size);
        DrawVignette(size);
        if (_showTexture) DrawSideTexture(size);
        DrawGlow(size);
        DrawSweep(size);
        DrawDetailLines(size);
    }

    /// <summary>1. 竖直渐变底</summary>
    private void DrawBase(Vector2 size)
    {
        Vector2[] pts =
        {
            new(0f, 0f), new(size.X, 0f), new(size.X, size.Y), new(0f, size.Y),
        };
        Color[] cols =
        {
            _bodyTop, _bodyTop, _bodyBottom, _bodyBottom,
        };
        DrawPolygon(pts, cols);

        // 顶部压暗一点，让顶栏更突出
        DrawRect(new Rect2(0f, 0f, size.X, size.Y * 0.16f), new Color(0f, 0f, 0f, 0.20f));
    }

    /// <summary>2. 四边压暗（暗角），让中间的面板更“浮”起来</summary>
    private void DrawVignette(Vector2 size)
    {
        float ex = size.X * 0.10f;
        float ey = size.Y * 0.16f;
        Color dark = new(0f, 0f, 0f, 0.38f);
        Color clear = new(0f, 0f, 0f, 0f);

        DrawPolygon(
            new[] { new Vector2(0f, 0f), new Vector2(size.X, 0f), new Vector2(size.X, ey), new Vector2(0f, ey) },
            new[] { dark, dark, clear, clear });

        DrawPolygon(
            new[] { new Vector2(0f, size.Y - ey), new Vector2(size.X, size.Y - ey), new Vector2(size.X, size.Y), new Vector2(0f, size.Y) },
            new[] { clear, clear, dark, dark });

        DrawPolygon(
            new[] { new Vector2(0f, 0f), new Vector2(ex, 0f), new Vector2(ex, size.Y), new Vector2(0f, size.Y) },
            new[] { dark, clear, clear, dark });

        DrawPolygon(
            new[] { new Vector2(size.X - ex, 0f), new Vector2(size.X, 0f), new Vector2(size.X, size.Y), new Vector2(size.X - ex, size.Y) },
            new[] { clear, dark, dark, clear });
    }

    /// <summary>3. 斜纹纹理（整体极淡，右侧加强）</summary>
    private void DrawSideTexture(Vector2 size)
    {
        float spacing = Mathf.Max(4f, _textureSpacing);
        float diag = size.Y;

        // 全屏铺一层极淡斜纹（会从面板缝隙里透出来）
        Color faint = new(_accent.R, _accent.G, _accent.B, _textureAlpha * 0.22f);
        for (float x = -diag; x < size.X; x += spacing)
            DrawLine(new Vector2(x, 0f), new Vector2(x + diag, diag), faint, 1f, true);

        // 右侧 1/4 加强，并逐段渐隐
        float startX = size.X * 0.72f;
        int bands = 3;
        for (int b = 0; b < bands; b++)
        {
            float t0 = (float)b / bands;
            float x0 = startX + (size.X - startX) * t0;
            float a = _textureAlpha * (1.15f - t0 * 0.5f);
            Color c = new(_accent.R, _accent.G, _accent.B, a);
            for (float x = x0 - diag; x < x0 + (size.X - startX) / bands + diag; x += spacing)
                DrawLine(new Vector2(x, 0f), new Vector2(x + diag, diag), c, 1f, true);
        }

        // 右侧竖向亮带，把纹理和边缘分开
        float edgeX = size.X - 34f;
        DrawRect(new Rect2(edgeX, 0f, 1.5f, size.Y),
            new Color(_accent.R, _accent.G, _accent.B, 0.14f));
    }

    /// <summary>3. 底部辉光（椭圆，两处）</summary>
    private void DrawGlow(Vector2 size)
    {
        if (_glowStrength <= 0f || _glowTex == null) return;

        float breath = _breathing ? 0.9f + 0.1f * Mathf.Sin(_time * 1.35f) : 1f;
        float strength = _glowStrength * breath;

        Rect2 src = new(Vector2.Zero, _glowTex.GetSize());

        // 主辉光：中心压在底边偏左，只露出上半部分，形成"从底部烧上来"的感觉
        var main = new Rect2(
            new Vector2(-size.X * 0.42f, size.Y * 0.42f),
            new Vector2(size.X * 1.20f, size.Y * 1.20f));
        DrawTextureRectRegion(_glowTex, main, src,
            new Color(_accent.R, _accent.G, _accent.B, 0.62f * strength));

        // 副辉光：右下，呼应右侧纹理
        var second = new Rect2(
            new Vector2(size.X * 0.55f, size.Y * 0.55f),
            new Vector2(size.X * 0.72f, size.Y * 0.95f));
        DrawTextureRectRegion(_glowTex, second, src,
            new Color(_accent.R, _accent.G, _accent.B, 0.34f * strength));

        // 顶部一层冷色，避免整屏发红
        var top = new Rect2(
            new Vector2(-size.X * 0.15f, -size.Y * 0.55f),
            new Vector2(size.X * 1.30f, size.Y * 1.10f));
        DrawTextureRectRegion(_glowTex, top, src,
            new Color(0.35f, 0.55f, 0.85f, 0.10f * strength));
    }

    /// <summary>4. 偶发扫光</summary>
    private void DrawSweep(Vector2 size)
    {
        if (!_showSweep || _sweepT < 0f || _glowTex == null) return;

        float t = Mathf.Clamp(_sweepT, 0f, 1f);
        float alpha = Mathf.Sin(t * Mathf.Pi) * _sweepPeakAlpha;
        if (alpha <= 0.001f) return;

        float x = _sweepRightToLeft
            ? Mathf.Lerp(size.X * 1.25f, -size.X * 0.35f, t)
            : Mathf.Lerp(-size.X * 0.35f, size.X * 1.25f, t);

        var rect = new Rect2(
            new Vector2(x - size.X * 0.30f, -size.Y * 0.15f),
            new Vector2(size.X * 0.60f, size.Y * 1.30f));
        DrawTextureRectRegion(_glowTex, rect, new Rect2(Vector2.Zero, _glowTex.GetSize()),
            new Color(1f, 1f, 1f, alpha));
    }

    /// <summary>5. 底部装饰细线 + 极淡扫描线</summary>
    private void DrawDetailLines(Vector2 size)
    {
        // 极淡横向扫描线（和面板的质感统一）
        Color scan = new(1f, 1f, 1f, 0.012f);
        for (float y = 0f; y < size.Y; y += 4f)
            DrawRect(new Rect2(0f, y, size.X, 1f), scan);

        // 底部两条发光细线
        float y1 = size.Y - 3f;
        DrawRect(new Rect2(0f, y1, size.X, 1.5f),
            new Color(_accent.R, _accent.G, _accent.B, 0.35f));
        DrawRect(new Rect2(size.X * 0.18f, y1 - 5f, size.X * 0.34f, 1f),
            new Color(_accent.R, _accent.G, _accent.B, 0.18f));
        DrawRect(new Rect2(size.X * 0.62f, y1 - 5f, size.X * 0.22f, 1f),
            new Color(_accent.R, _accent.G, _accent.B, 0.14f));
    }
}



