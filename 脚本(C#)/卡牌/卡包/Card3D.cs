using Godot;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 开包动画里的单张"3D 卡/卡包"精灵：从下方飞入到 <see cref="UltimatePos"/>。<br/>
/// 注意：<see cref="UltimatePos"/> 如果在 AddChild 之前设置过（开包控制器会按序号错开位置），
/// _Ready 不会再覆盖它，只有没设置（默认 0,0）时才默认落在屏幕中心。
/// </summary>
public partial class Card3D : Sprite2D
{
	/// <summary>飞入后停留的位置（屏幕坐标）</summary>
	public Vector2 UltimatePos { get; set; }

	/// <summary>从下方多远飞上来</summary>
	public float MoveHeight { get; set; } = -1200f;

	/// <summary>飞入动画间隔/延迟（秒），做依次飞入的效果</summary>
	public float StartDelay { get; set; } = 0f;

	private Tween _tween;

	public override void _Ready()
	{
		if (UltimatePos == Vector2.Zero)
			UltimatePos = GetViewportRect().Size / 2;
		_ = MoveAn();
	}

	public async Task MoveAn()
	{
		if (_tween != null && _tween.IsValid())
			_tween.Kill();

		_tween = CreateTween();
		Position = UltimatePos + new Vector2(0, MoveHeight);
		_tween.TweenProperty(this, "position", UltimatePos, 0.5f)
			.SetDelay(StartDelay)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		await ToSignal(_tween, Tween.SignalName.Finished);
	}
}
