
using Godot;
using Logger;

namespace Card;

/// <summary>
/// 卡牌的可视化部分
/// </summary>
[GlobalClass]
public partial class NodeCard : Control
{
	private static readonly PackedScene Scene = GD.Load<PackedScene>("res://场景(C#)/卡牌.tscn");
	private static NodeCard ChoiceCard;
	public CardModel Model { get;private set; }
	/// <summary>
	/// 从一个卡牌中创建一个卡牌节点并自动挂载到父节点
	/// </summary>
	/// <param name="parent">父节点</param>
	/// <param name="cardModel">卡牌</param>
	public static NodeCard Create(Node parent,CardModel cardModel)
	{
		var node = Scene.Instantiate<NodeCard>();
		node.Model = cardModel;
		node.Scale *= 0.5f;
		node.Fresh();
		parent.AddChild(node);
		return node;
	}
	private bool _isDragging;
	private Vector2 _cardOriginPos;
	private Vector2 _mouseDownGlobal;
	private Tween _returnTween;
	private const float DragThreshold = 10f;
	public override void _Ready()
	{
		var hitBtn = GetNode<Button>("碰撞");
		hitBtn.GuiInput += OnHitButtonGuiInput;
	}
	private void OnHitButtonGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.Left)
			{
				if (mb.Pressed)
				{
					KillReturnTween();
					_mouseDownGlobal = GetGlobalMousePosition();
					_cardOriginPos = Position;
				}
				else
				{
					if (_isDragging)
					{
						float dist = (_mouseDownGlobal - GetGlobalMousePosition()).Length();
						if (dist < DragThreshold)
						{
							OnCardClick();
						}
						else
						{
							EndDrag();
						}
					}
					_isDragging = false;
				}
			}
		}
		else if (@event is InputEventMouseMotion)
		{
			if (!Input.IsMouseButtonPressed(MouseButton.Left))
				return;

			if (!_isDragging)
			{
				float dist = (_mouseDownGlobal - GetGlobalMousePosition()).Length();
				if (dist > DragThreshold)
				{
					_isDragging = true;
					ChoiceCard = this;
					ZIndex += 100;
				}
			}
			if (_isDragging)
			{
				Vector2 delta = GetGlobalMousePosition() - _mouseDownGlobal;
				Position = _cardOriginPos + delta;
			}
		}
	}


	private void OnCardClick()
	{
		ChoiceCard = this;
	}

	private void EndDrag()
	{
		ZIndex -= 100;
		if (ChoiceCard == this) ChoiceCard = null;
		ReturnToOrigin();
	}

	private void ReturnToOrigin()
	{
		KillReturnTween();
		_returnTween = CreateTween();
		_returnTween.SetEase(Tween.EaseType.Out);
		_returnTween.SetTrans(Tween.TransitionType.Quad);
		_returnTween.TweenProperty(this, "position", _cardOriginPos, 0.2f);
	}

	private void KillReturnTween()
	{
		if (_returnTween != null && _returnTween.IsValid())
		{
			_returnTween.Kill();
		}
	}

	/// <summary>
	/// 每次修改卡牌时调用 用于刷新卡牌属性
	/// </summary>
	public void Fresh()
	{
		if (Model != null)
		{
			
			if (Model.Status == Status.FaceUp)
			{
				var costs = GetNode<Node2D>("%费用容器");
				costs.GetNode<RichTextLabel>("数值").Text = $"[center]{Model.Cost.Current}[/center]";
				costs.GetNode<TextureRect>("费用").Texture = Model.CampCostIcon;
				GetNode<Node2D>("正面卡牌").Visible = true;
				GetNode<TextureRect>("牌背").Visible = false;
				GetNode<TextureRect>("%卡牌图标").Texture = Model.Icon;
				if (Model.Cost.HasChanged)
				{
					if (Model.Cost.PositiveChanged)
						costs.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", new Color(0, 243, 0));
					else
						costs.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", new Color(243, 0, 0));
				}
				if (Model is FighterCardModel fighter)
				{
					var atks = GetNode<Node2D>("%攻击力容器");
					var hps = GetNode<Node2D>("%生命值容器");
					atks.GetNode<RichTextLabel>("数值").Text = $"[center]{fighter.Atk.Current}[/center]";
					atks.GetNode<TextureRect>("攻击力").Texture = fighter.AtkType.Current.Icon;
					atks.GetNode<TextureRect>("攻击力特效").Visible = true;
					if (fighter.AtkType.Current.AddtiveIcon != null)
						atks.GetNode<TextureRect>("攻击力特效").Texture = fighter.AtkType.Current.AddtiveIcon;
					else
						atks.GetNode<TextureRect>("攻击力特效").Visible = false;
					hps.GetNode<RichTextLabel>("数值").Text = $"[center]{fighter.Hp.Current}[/center]";
					hps.GetNode<TextureRect>("生命值").Texture = fighter.HpType.Current.Icon;
					if (fighter.HpType.Current.AddtiveIcon != null)
						hps.GetNode<TextureRect>("生命值特效").Texture = fighter.HpType.Current.AddtiveIcon;
					else
						hps.GetNode<TextureRect>("生命值特效").Visible = false;
					if (fighter.Hp.HasChanged)
					{
						if (fighter.Hp.PositiveChanged)
							hps.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", new Color(0, 243, 0));
						else
							hps.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", new Color(243, 0, 0));
					}
					if (fighter.Atk.HasChanged)
					{
						if (fighter.Atk.PositiveChanged)
							atks.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", new Color(0, 243, 0));
						else
							atks.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", new Color(243, 0, 0));
					}
				}
			}
			else if (Model.Status == Status.FaceDown)
			{
				GetNode<Node2D>("正面卡牌").Visible = false;
				GetNode<TextureRect>("牌背").Visible = true;
			}
		}
	}
}
