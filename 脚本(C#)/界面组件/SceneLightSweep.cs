using Godot;
using System;

namespace Widgets;

/// <summary>
/// 覆盖在 UI 之上的偶发光晕层（"时不时的光线特效"）
/// ==================================================================
/// 每隔 7~14 秒，一团柔光从左或右横扫过整个画面，峰值透明度很低（默认 0.14），
/// 只做质感点缀，不影响阅读。整层 mouse_filter = Ignore，不会挡点击。
///
/// 放在场景里时 z_index 要高于所有面板（例如 4000），并且铺满 1280×720。
/// </summary>
[Tool]
[GlobalClass]
public partial class SceneLightSweep : Control
{
    private Color _accent = new Color(1f, 1f, 1f, 1f);

    /// <summary>光晕颜色（默认近白，按阵营可换成绿/紫）</summary>
    [ExportGroup("光晕")]
    [Export]
    public Color AccentColor
    {
        get => _accent;
        set { _accent = value; QueueRedraw(); }
    }

    private bool _enabled = true;

    /// <summary>开关</summary>
    [Export]
    public bool Enabled
    {
        get => _enabled;
        set { _enabled = value; if (!value) _t = -1f; }
    }

    private float _intervalMin = 7f;

    /// <summary>最短间隔（秒）</summary>
    [Export(PropertyHint.Range, "1,120,0.5")]
    public float IntervalMin
    {
        get => _intervalMin;
        set => _intervalMin = Mathf.Max(0.1f, value);
    }

    private float _intervalMax = 14f;

    /// <summary>最长间隔（秒）</summary>
    [Export(PropertyHint.Range, "1,180,0.5")]
    public float IntervalMax
    {
        get => _intervalMax;
        set => _intervalMax = Mathf.Max(_intervalMin, value);
    }

    private float _peakAlpha = 0.14f;

    /// <summary>峰值亮度</summary>
    [Export(PropertyHint.Range, "0,0.6,0.005")]
    public float PeakAlpha
    {
        get => _peakAlpha;
        set => _peakAlpha = Mathf.Max(0f, value);
    }

    private float _duration = 1.6f;

    /// <summary>一次扫光持续多少秒</summary>
    [Export(PropertyHint.Range, "0.4,6,0.1")]
    public float Duration
    {
        get => _duration;
        set => _duration = Mathf.Max(0.2f, value);
    }

    // ---------------- 内部 ----------------
    private Texture2D _glowTex;
    private readonly Random _rng = new(20240815);
    private float _timer;
    private float _t = -1f;
    private bool _rightToLeft;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        BuildGlowTexture();
        _timer = RandomDelay();
        if (Engine.IsEditorHint()) SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (!_enabled)
        {
            if (_t >= 0f) { _t = -1f; QueueRedraw(); }
            return;
        }

        if (_t >= 0f)
        {
            _t += (float)delta / Mathf.Max(0.2f, _duration);
            if (_t >= 1f)
            {
                _t = -1f;
                _timer = RandomDelay();
            }
            QueueRedraw();
        }
        else
        {
            _timer -= (float)delta;
            if (_timer <= 0f)
            {
                _t = 0f;
                _rightToLeft = _rng.Next(2) == 0;
                QueueRedraw();
            }
        }
    }

    /// <summary>立刻扫一次（测试用，也可以在打牌等时机手动触发）</summary>
    public void TriggerNow()
    {
        if (!_enabled) return;
        _t = 0f;
        _rightToLeft = _rng.Next(2) == 0;
        QueueRedraw();
    }

    private float RandomDelay()
        => _intervalMin + (float)_rng.NextDouble() * Mathf.Max(0.01f, _intervalMax - _intervalMin);

    /// <summary>按阵营换光晕颜色</summary>
    public void SetCampTheme(Card.Camp camp)
    {
        switch (camp)
        {
            case Card.Camp.Plant:
                AccentColor = new Color(0.62f, 1f, 0.78f);
                break;
            case Card.Camp.Zombie:
                AccentColor = new Color(0.78f, 0.62f, 1f);
                break;
            default:
                AccentColor = new Color(1f, 0.86f, 0.80f);
                break;
        }
    }

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

    public override void _Draw()
    {
        if (!_enabled || _t < 0f || _glowTex == null) return;

        Vector2 size = Size;
        if (size.X < 4f || size.Y < 4f) return;

        float t = Mathf.Clamp(_t, 0f, 1f);
        float alpha = Mathf.Sin(t * Mathf.Pi) * _peakAlpha;
        if (alpha <= 0.002f) return;

        float x = _rightToLeft
            ? Mathf.Lerp(size.X * 1.20f, -size.X * 0.20f, t)
            : Mathf.Lerp(-size.X * 0.20f, size.X * 1.20f, t);

        // 主体光团：竖长椭圆
        var body = new Rect2(
            new Vector2(x - size.X * 0.28f, -size.Y * 0.25f),
            new Vector2(size.X * 0.56f, size.Y * 1.50f));
        DrawTextureRectRegion(_glowTex, body, new Rect2(Vector2.Zero, _glowTex.GetSize()),
            new Color(_accent.R, _accent.G, _accent.B, alpha));

        // 核心更亮一点点，形成"光心"
        var core = new Rect2(
            new Vector2(x - size.X * 0.10f, -size.Y * 0.10f),
            new Vector2(size.X * 0.20f, size.Y * 1.20f));
        DrawTextureRectRegion(_glowTex, core, new Rect2(Vector2.Zero, _glowTex.GetSize()),
            new Color(_accent.R, _accent.G, _accent.B, alpha * 0.8f));
    }
}
