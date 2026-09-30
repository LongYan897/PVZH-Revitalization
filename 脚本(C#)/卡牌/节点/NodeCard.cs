
using Godot;
using Logger;
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
			node.PivotOffsetRatio = new(0.5f, 0.5f);
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
	private Tween _clickTween;
	private bool _clickAnimationPlayed;
	private const float ClickScaleDown = 0.9f;
	private const float ClickScaleTime = 0.08f;
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
					KillClickTween();

					_mouseDownGlobal = GetGlobalMousePosition();
					_cardOriginPos = Position;
					_isDragging = false;
					_clickAnimationPlayed = false;

					_clickTween = CreateTween();
					_clickTween.TweenProperty(this, "scale", Scale * ClickScaleDown, ClickScaleTime)
						.SetTrans(Tween.TransitionType.Quad)
						.SetEase(Tween.EaseType.Out);
				}
				else
				{
					if (_isDragging)
					{
						EndDrag();
					}
					else
					{
						float dist = (_mouseDownGlobal - GetGlobalMousePosition()).Length();

						if (dist < DragThreshold)
						{
							KillClickTween();
							_clickTween = CreateTween();
							_clickTween.TweenProperty(this, "scale", new Vector2(0.5f, 0.5f), ClickScaleTime)
								.SetTrans(Tween.TransitionType.Quad)
								.SetEase(Tween.EaseType.Out);

							OnCardClick();
						}
						else
						{
							KillClickTween();
							_clickTween = CreateTween();
							_clickTween.TweenProperty(this, "scale", new Vector2(0.5f, 0.5f), ClickScaleTime)
								.SetTrans(Tween.TransitionType.Quad)
								.SetEase(Tween.EaseType.Out);
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

					KillClickTween();
					Scale = new Vector2(0.5f, 0.5f);

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
		CardDes.DisplayDescription(GetParent(), Model);
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
	private void KillClickTween()
	{
		if (_clickTween != null && _clickTween.IsValid())
		{
			_clickTween.Kill();
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
				{
					var frame = GetNode<TextureRect>("%卡框");
					var frameGlowBg = GetNode<TextureRect>("%卡牌发光背景");
					var frameBg = GetNode<TextureRect>("%卡牌底板");
					frame.Texture = Model.CardType switch
					{
						CardType.Hero | CardType.Trick => Model.Rarity switch
						{
							Rarity.Legend => Model.Camp switch
							{
								Camp.Plant => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_HeroPower_plant.png"),
								Camp.Zombie => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_HeroPower_zombie.png"),
								_ => null
							},
							_ => Model.Camp switch
							{
								Camp.Plant => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Superpower_plant.png"),
								Camp.Zombie => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Superpower_zombie.png"),
								_ => null
							}
						},
						CardType.Hero | CardType.Fighter => Model.Rarity switch
						{
							Rarity.Legend => Model.Camp switch
							{
								Camp.Plant => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_HeroPower_plant.png"),
								Camp.Zombie => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_HeroPower_zombie.png"),
								_ => null
							},
							_ => Model.Camp switch
							{
								Camp.Plant => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Superpower_plant.png"),
								Camp.Zombie => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Superpower_zombie.png"),
								_ => null
							}
						},
						CardType.Hero | CardType.Environment => Model.Rarity switch
						{
							Rarity.Legend => Model.Camp switch
							{
								Camp.Plant => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_HeroPower_plant.png"),
								Camp.Zombie => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_HeroPower_zombie.png"),
								_ => null
							},
							_ => Model.Camp switch
							{
								Camp.Plant => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Superpower_plant.png"),
								Camp.Zombie => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Superpower_zombie.png"),
								_ => null
							}
						},
						CardType.None | CardType.Fighter => Model.Rarity switch
						{
							Rarity.Basic => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_FRONT.png"),
							Rarity.Common => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_FRONT.png"),
							Rarity.Uncommon => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_R2.png"),
							Rarity.Rare => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_R3.png"),
							Rarity.SuperRare => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_R4.png"),
							Rarity.Legend => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_R5.png"),
							Rarity.Activity => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Event.png"),
							_ => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_FRONT.png"),
						},
						CardType.None | CardType.Environment => Model.Rarity switch
						{
							Rarity.Basic => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/Environment_R1.png"),
							Rarity.Common => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/Environment_R1.png"),
							Rarity.Uncommon => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/Environment_R2.png"),
							Rarity.Rare => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/Environment_R3.png"),
							Rarity.SuperRare => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/Environment_R4.png"),
							Rarity.Legend => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/Environment_R5.png"),
							Rarity.Activity => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/Environment_Event.png"),
							_ => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/Environment_R1.png"),
						},
						CardType.None | CardType.Trick => Model.Rarity switch
						{
							Rarity.Basic => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_onetime_R1.png"),
							Rarity.Common => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_onetime_R1.png"),
							Rarity.Uncommon => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_onetime_R2.png"),
							Rarity.Rare => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_onetime_R3.png"),
							Rarity.SuperRare => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_onetime_R4.png"),
							Rarity.Legend => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_onetime_R5.png"),
							Rarity.Activity => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_onetime_Event.png"),
							_ => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_onetime_R1.png"),
						},
						_ => null
					};
					frame.Scale = Model.CardType switch
					{
						CardType.None | CardType.Environment => new(0.55f,0.55f),
						_ => new(1f,1f)
					};
					frame.Position = Model.CardType switch
					{
						CardType.None | CardType.Environment => new(128.0f, 92f),
						_ => new(128.0f, 96f)
					};
					frameBg.Modulate = new("d6db23");
					frameBg.Texture = Model.CardType switch
					{
						CardType.Hero | CardType.Trick => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Back_onetime.png"),
						CardType.Hero | CardType.Fighter => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Back_onetime.png"),
						CardType.Hero | CardType.Environment => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Back_onetime.png"),
						CardType.None | CardType.Fighter => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_BACK.png"),
						CardType.None | CardType.Environment => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/Environment_Back.png"),
						CardType.None | CardType.Trick => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Back_onetime.png"),
						_ => null
					};
					frameGlowBg.Modulate = new("20ff07");
					frameGlowBg.Texture = Model.CardType switch
					{
						CardType.Hero | CardType.Trick => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Back_onetime_glow.png"),
						CardType.Hero | CardType.Fighter => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Back_onetime_glow.png"),
						CardType.Hero | CardType.Environment => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Back_onetime_glow.png"),
						CardType.None | CardType.Fighter => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/背景底版1.png"),
						CardType.None | CardType.Environment => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/Environment_GlowBack.png"),
						CardType.None | CardType.Trick => GD.Load<Texture2D>("res://素材(C#)/卡牌属性/SEEDPACKET_Back_onetime_glow.png"),
						_ => null
					};
				}
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
				GetNode<TextureRect>("牌背").Texture = Model.CardType switch
				{
					CardType.None | CardType.Trick => Model.Camp switch
					{
						Camp.Plant => GD.Load<Texture2D>("res://素材/牌背、卡面/cardback_plants_trick.png"),
						Camp.Zombie => GD.Load<Texture2D>("res://素材/牌背、卡面/cardback_zombies_trick.png"),
						_ => null
					},
					_ => Model.Camp switch
					{
						Camp.Plant => GD.Load<Texture2D>("res://素材/牌背、卡面/cardback_plants.png"),
						Camp.Zombie => GD.Load<Texture2D>("res://素材/牌背、卡面/cardback_zombies.png"),
						_ => null
					}
				};
			}
		}
	}
}
