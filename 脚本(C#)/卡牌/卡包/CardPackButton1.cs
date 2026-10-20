using Godot;
using System.Threading.Tasks;
namespace Pack;

public partial class CardPackButton1 : Panel
{
	private bool _isPressed = false;
	private Tween _scaleTween; // 存储 Tween，防止多次点击/悬停导致动画冲突

	public override void _Ready()
	{
		MouseEntered += OnMouseEnter;
		MouseExited += OnMouseExit;
		PivotOffset = Size / 2;
		// 初始弹出动画
		Scale = new Vector2(0.7f, 0.7f);
		PlayScaleTween(new Vector2(1.0f, 1.0f), 0.5f);
	}

	private void OnMouseEnter()
	{
		// 悬停时放大
		if (!_isPressed)
		{
			SelfModulate = new Color(1.2f, 1.2f, 1.2f);
			PlayScaleTween(new Vector2(1.05f, 1.05f), 0.1f);
		}
	}

	private void OnMouseExit()
	{
		// 移出时恢复颜色和大小
		SelfModulate = new Color(1f, 1f, 1f);
		if (!_isPressed)
		{
			PlayScaleTween(new Vector2(1.0f, 1.0f), 0.1f);
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			if (mouseEvent.Pressed)
			{
				_isPressed = true;
				// 按下时缩小
				PlayScaleTween(new Vector2(0.95f, 0.95f), 0.1f);
			}
			else
			{
				if (_isPressed)
				{
					_isPressed = false;

					if (IsMouseInside())
					{
						OnButtonClicked();
					}
					else
					{
						// 移出且松开，恢复默认大小
						PlayScaleTween(new Vector2(1.0f, 1.0f), 0.1f);
					}
				}
			}
		}
	}

	private bool IsMouseInside()
	{
		Vector2 mousePos = GetLocalMousePosition();
		return new Rect2(Vector2.Zero, Size).HasPoint(mousePos);
	}

	private void OnButtonClicked()
	{
		// 点击后的回弹动画（假设点击后应该恢复到悬停大小 1.05）
		// 如果你想让点击后变回普通大小 1.0，这里就改成 1.0
		PlayScaleTween(new Vector2(1.05f, 1.05f), 0.3f, Tween.TransitionType.Back);
	}

	// 封装一个统一的 Tween 播放函数，防止动画重叠导致的 Bug
	private void PlayScaleTween(Vector2 targetScale, float duration, Tween.TransitionType transType = Tween.TransitionType.Sine)
	{
		// 如果上一次的 Tween 还在跑，先把它杀掉
		if (_scaleTween != null && _scaleTween.IsValid())
		{
			_scaleTween.Kill();
		}

		_scaleTween = CreateTween();
		_scaleTween.TweenProperty(this, "scale", targetScale, duration)
			.SetTrans(transType)
			.SetEase(Tween.EaseType.Out);
	}
}
