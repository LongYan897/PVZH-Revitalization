
using Godot;
using Pack;
using System.Collections.Generic;
using System.Linq;

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
	private static readonly Dictionary<NodeCard, CardModel> instances = new();
	private const int maxInstance = 20;
	/// <summary>
	/// 从一个卡牌中创建一个卡牌节点并自动挂载到父节点
	/// 若场上卡牌节点数量大于等于20,则从20个节点中取暂时无绑定卡牌的节点
	/// </summary>
	/// <param name="parent">父节点</param>
	/// <param name="cardModel">卡牌</param>
	public static NodeCard DisplayCard(Node parent,CardModel cardModel)
	{
		if (instances.Count < maxInstance)
		{
			var node = Scene.Instantiate<NodeCard>();
			node.Model = cardModel;
			node.Scale *= 0.5f;
			node.Fresh();
			instances.Add(node, cardModel);
			parent.AddChild(node);
			return node;
		}
		else
		{
			var node = instances.First(p => p.Value == null).Key;
			node.Model = cardModel;
			node.Scale *= 0.5f;
			node.Fresh();
			parent.AddChild(node);
			return node;
		}
	}
	/// <summary>
	/// 从一个卡牌中创建一个卡牌节点并自动挂载到父节点(播放抽卡动画)
	/// 若场上卡牌节点数量大于等于20,则从20个节点中取暂时无绑定卡牌的节点
	/// </summary>
	/// <param name="parent">父节点</param>
	/// <param name="cardModel">卡牌</param>
	public static NodeCard DrawACard(Node parent, CardModel cardModel, Vector2 position)
	{
		if (instances.Count < maxInstance)
		{
			var node = Scene.Instantiate<NodeCard>();
			node.Model = cardModel;
			node.Scale *= 0.5f;
			node.Position = position;
			instances.Add(node, cardModel);
			parent.AddChild(node);
			node.DrawAnimation();
			return node;
		}
		else
		{
			var node = instances.First(p => p.Value == null).Key;
			node.Model = cardModel;
			node.Scale *= 0.5f;
			node.Position = position;
			parent.AddChild(node);
			node.DrawAnimation();
			return node;
		}
	}
	private async void DrawAnimation()
	{
		var drawFrame = GetNode<Sprite2D>("%特效");
		drawFrame.Visible = true;
		GetNode<Node2D>("正面卡牌").Visible = false;
		GetNode<TextureRect>("牌背").Visible = false;
		drawFrame.Modulate = new Color(0, 0, 0, 0);
		drawFrame.Rotation = 0;
		drawFrame.Scale = new Vector2(1.5f, 1.5f);

		var tween = drawFrame.CreateTween();
		var tween1 = drawFrame.CreateTween();

		tween.TweenProperty(drawFrame, "modulate", new Color(1, 1, 1, 1), 0.3f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		tween1.TweenProperty(drawFrame, "rotation", Mathf.Tau, 0.3f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);

		await ToSignal(tween1, Tween.SignalName.Finished);

		tween1 = CreateTween();
		tween1.TweenProperty(drawFrame, "rotation", 1.5f * Mathf.Tau, 0.4f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		var tween2 = CreateTween();
		tween2.TweenProperty(drawFrame, "scale", new Vector2(0.2f, 0.2f), 0.4f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.In);

		await ToSignal(GetTree().CreateTimer(0.15f), Timer.SignalName.Timeout);

		tween = CreateTween();
		tween.TweenProperty(drawFrame, "modulate", new Color(0, 0, 0, 0), 0.3f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		await ToSignal(tween, Tween.SignalName.Finished);
		drawFrame.Visible = false;
		Fresh();
	}
	private bool _isDragging;
	private Vector2 _cardOriginPos;
	private Vector2 _mouseDownGlobal;
	private Tween _returnTween;
	private Tween _colorTween;
	private const float DragThreshold = 10f;
	public override void _Ready()
	{
		var hitBtn = GetNode<Button>("碰撞");
		hitBtn.GuiInput += OnHitButtonGuiInput;
	}
	private ColorBox _colorBox = new ColorBox(new("f4f4d5"),new("20ff07"), new Color(0, 243, 0), new Color(243, 0, 0));
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

					var glowNode = GetNode<TextureRect>("%卡牌发光背景");
					if (_colorTween != null && _colorTween.IsValid())
						_colorTween.Kill();
					_colorTween = glowNode.CreateTween();
					_colorTween.TweenProperty(glowNode, "modulate", _colorBox.Default, 0.5f)
						.SetTrans(Tween.TransitionType.Cubic)
						.SetEase(Tween.EaseType.Out);
					GetNode<GpuParticles2D>("%粒子特效").Emitting = false;
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

					var glowNode = GetNode<TextureRect>("%卡牌发光背景");
					if (_colorTween != null && _colorTween.IsValid())
						_colorTween.Kill();
					_colorTween = glowNode.CreateTween();
					_colorTween.TweenProperty(glowNode, "modulate", _colorBox.ColorA, 0.5f)
						.SetTrans(Tween.TransitionType.Cubic)
						.SetEase(Tween.EaseType.Out);
					GetNode<GpuParticles2D>("%粒子特效").Emitting = true;
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
				GetNode<TextureRect>("%卡牌发光背景").Modulate = _colorBox.Default;
				GetNode<GpuParticles2D>("%粒子特效").Emitting = false;
				if (Model.Cost.HasChanged)
				{
					if (Model.Cost.PositiveChanged)
						costs.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.ColorB);
					else
						costs.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.ColorC);
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
							hps.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.ColorB);
						else
							hps.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.ColorC);
					}
					if (fighter.Atk.HasChanged)
					{
						if (fighter.Atk.PositiveChanged)
							atks.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.ColorB);
						else
							atks.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.ColorC);
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
