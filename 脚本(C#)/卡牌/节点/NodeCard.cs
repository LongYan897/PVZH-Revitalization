
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
	public static NodeCard ChoiceCard;
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
	/// <summary>
	/// 卡牌目标锚点（持续移动的目标位置）
	/// </summary>
	private Vector2 _targetAnchor;
	private const float SpringK = 2.4f;
	/// <summary>
	/// 设置该卡牌原始位置(卡牌会持续向该点移动)
	/// </summary>
	/// <param name="vector2"></param>
	public void SetAnchor(Vector2 vector2)
	{
		_targetAnchor = vector2;
	}
	public override void _Process(double delta)
	{
		Vector2 diff = _targetAnchor - Position;
		Position += diff * SpringK * (float)delta;
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
