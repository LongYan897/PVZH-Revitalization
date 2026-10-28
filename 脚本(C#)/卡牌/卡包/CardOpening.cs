using Card;
using Extension;
using Godot;
using System;
using System.Threading.Tasks;
public partial class CardOpening : Node2D
{
    //节点
	public TextureRect Crack { get; set; }
	public TextureRect CrackBlur { get; set; }
	public TextureRect ExplodeEffect { get; set; }
    public TextureRect CardNode { get; set; }
    //属性
    public Vector2 UltimatePos { get; set; }
    public float MoveHeight { get; set; } = -1200f;
    public float SineDistance { get; set; } = 200f;
    private bool _isClicked { get; set; }
    public Tween Tween1 { get; set; }
    //委托
    public event Action Finished;
    public async override void _Ready()
    {
        Crack = GetNode<TextureRect>("卡包/裂缝/裂缝本体");
        CrackBlur = GetNode<TextureRect>("卡包/裂缝/裂缝发光");
        ExplodeEffect = GetNode<TextureRect>("卡包/爆炸特效/爆炸特效");
        CardNode = GetNode<TextureRect>("卡包");
        UltimatePos = GetViewportRect().Size / 2 - new Vector2(0,100);
        Crack.Scale = new Vector2(0, 1.242f);
        await IntroAn();
        await IdleAn();
    }
    public async Task IntroAn()
    {
        Position = UltimatePos + new Vector2(0, MoveHeight);
        QuickTween().TweenProperty(this, "position", UltimatePos - new Vector2(0,-SineDistance), 1.5f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(Tween1, Tween.SignalName.Finished);
    }
    public async Task IdleAn()
    {
        QuickTween();
        Tween1.SetLoops();

        Tween1.TweenProperty(this, "position", UltimatePos - new Vector2(0,SineDistance), 1f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        Tween1.TweenProperty(this, "position", UltimatePos + new Vector2(0, SineDistance), 1f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }
    public override async void _Input(InputEvent @event)
    {
        if (_isClicked) return;
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            _isClicked = true;
            QuickTween().TweenProperty(this, "position", UltimatePos, 0.5f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
            await ToSignal(Tween1, Tween.SignalName.Finished);
            QuickTween().TweenProperty(this, "rotation", Math.PI * -0.075f, 0.1f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
            Tween1.Parallel().TweenProperty(Crack, "scale", new Vector2(1.242f, 1.242f),0.1f);
            await ToSignal(Tween1, Tween.SignalName.Finished);
            QuickTween().TweenProperty(this, "rotation", 0 ,1f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
            _ = Blink();
            await CrackAndExplodeAn();
            Finished?.Invoke();

        }
    }
    public async Task Blink()
    {
        Tween tween = CreateTween();
        float duration = 0.25f;

        for (int i = 0; i < 6; i++)
        {
            tween.TweenProperty(CardNode, "self_modulate", new Color(2, 2, 2, 1), duration * 0.5f);
            tween.TweenProperty(CardNode, "self_modulate", new Color(1, 1, 1, 1), duration * 0.5f);
            duration *= 0.9f;
        }
        tween.TweenProperty(CardNode, "self_modulate", new Color(2, 2, 2, 1), duration * 0.5f);
        await ToSignal(tween, Tween.SignalName.Finished);
    }
    public async Task CrackAndExplodeAn()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(CrackBlur, "self_modulate", new Color(1, 1, 1, 1), 1f);
        tween.TweenProperty(ExplodeEffect, "scale",new Vector2(3,3), 0.5f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
        await this.Wait(1f);
        Tween tween1 = CreateTween();
        tween1.TweenProperty(CardNode, "scale", new Vector2(1.5f, 1.5f), 0.5f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
        tween1.Parallel().TweenProperty(CardNode, "modulate", new Color(1, 1, 1, 0), 0.5f);
        await ToSignal(tween1, Tween.SignalName.Finished);
    }
    public Tween QuickTween()
    {
        if (Tween1 != null && Tween1.IsValid() && Tween1.IsRunning()) Tween1.Kill();
        Tween1 = CreateTween();
        return Tween1;
    }
}
