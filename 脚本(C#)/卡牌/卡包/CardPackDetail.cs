using FileExtension;
using Godot;
using Scene;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 卡包详情：从卡包列表的「详情」按钮打开，根节点必须是 SubMenuScene 的子类，
/// 否则 SubMenu.Instantiate() 里的 Instantiate&lt;SubMenuScene&gt;() 会抛 InvalidCastException。<br/>
/// 进入场景树时 SubMenuScene._EnterTree() 会先调 BackButton.JoinScene(HideReturnButton)，
/// 再 await _Load()，所以节点初始化写在这里，不要放 _Ready。<br/>
/// 注意：_Load() 执行期间场景还是 Visible = false，要等 LoadTask 完成后 SceneManager 才显示它，
/// 所以入场动画只能“点火即走”（不 await），否则动画会在屏幕外播完。<br/>
/// 显示哪个卡包由 <see cref="SubMenuScene.Payload"/> 决定（卡包磁贴点详情时把卡包名传进来）。
/// </summary>
public partial class CardPackDetail : SubMenuScene
{
	private const string PackDataPath = "res://数据(C#)/cardPack.json";
	/// <summary>一排几张卡</summary>
	private const int CardsPerRow = 4;
	/// <summary>每张卡占的格子宽度：720 的可用宽度里放 4 个（150*4 + 12*3 = 636，左右留白）</summary>
	private const float CardCellWidth = 150f;
	/// <summary>卡牌之间的间距（横向、纵向都用这个）</summary>
	private const int RowSeparation = 12;
	/// <summary>卡牌.tscn 根节点的设计尺寸（custom_minimum_size），用来算缩放比</summary>
	private const float CardNaturalWidth = 255f;
	private const float CardNaturalHeight = 200f;
	private FlowContainer ItemListNode;
	private readonly System.Collections.Generic.List<NodeCard> _cards = new();
	private Tween _introTween;
	private bool _introPlayed;
	/// <summary>点进来的是哪个卡包</summary>
	private string _packName = "Pack1";

	public override async Task _Load()
	{
		ItemListNode = GetNode<FlowContainer>("遮罩/滚动条/FlowContainer");
		// 卡牌之间的间距由容器的 theme 常量决定（HFlowContainer 默认只有 4）
		ItemListNode.AddThemeConstantOverride("h_separation", RowSeparation);
		ItemListNode.AddThemeConstantOverride("v_separation", RowSeparation);
		// 满的一排居中显示；最后一行不满时仍靠左
		ItemListNode.Alignment = FlowContainer.AlignmentMode.Center;
		ItemListNode.LastWrapAlignment = FlowContainer.LastWrapAlignmentMode.Begin;

		// 由点开它的人决定内容：卡包磁贴的「详情」按钮会把卡包名放进 Payload
		if (Payload is string packName && !string.IsNullOrWhiteSpace(packName))
			_packName = packName;
		FillPackInfo(_packName);
		await FillCards(_packName);

		IntroAn();
	}

	/// <summary>按卡包名把数据填进详情界面（数据来自 数据(C#)/cardPack.json 里对应的对象）。</summary>
	private void FillPackInfo(string packName)
	{
		FileJson json = new FileJson().Open(PackDataPath).OpenObject(packName);
		string title = json.GetVariable("PackName", packName);
		string intro = json.GetVariable("IntroduceText", "");

		Label topLabel = GetNodeOrNull<Label>("遮罩/顶部/顶部文本");
		if (topLabel != null) topLabel.Text = $"{title} 可能包含:";

		Label introLabel = GetNodeOrNull<Label>("遮罩/滚动条/FlowContainer/横幅/介绍文本");
		if (introLabel != null) introLabel.Text = intro;

		// 卡池分组也在这里读：json.GetArray<JsonObject>("group") 里每个 group 有
		// Name / Cards / MaxCount / BaseRate / UltimateRate，可以据此在 ItemListNode 里生成下方卡牌列表。
	}

	/// <summary>
	/// 按卡包数据生成卡牌：一排 4 张，不够 4 张靠左、后面空着。<br/>
	/// 注意：卡牌外面必须套一个固定尺寸的"格子"（普通 Control）。Godot 的容器是按子节点的
	/// custom_minimum_size 排版的，而且 Container 会把子节点的 scale 重置成 1；
	/// 直接给卡牌设 scale 再塞进 HFlowContainer 是排不下一排 4 张的。
	/// 格子决定排版尺寸，卡牌在格子里按比例缩放。
	/// </summary>
	private async Task FillCards(string packName)
	{
		float fit = CardCellWidth / CardNaturalWidth;
		var cellSize = new Vector2(CardCellWidth, CardNaturalHeight * fit);

		FileJson json = new FileJson().Open(PackDataPath).OpenObject(packName);
		foreach (JsonObject group in json.GetArray<JsonObject>("group"))
		{
			var groupName = group["Name"]?.GetValue<string>();
            CardBar newPanel = GD.Load<PackedScene>("res://场景(C#)/卡包/横幅.tscn").Instantiate<CardBar>();
			newPanel.IntroduceLabel.Text = groupName;
			ItemListNode.AddChild(newPanel);


            var cards = group["Cards"]?.AsArray();
			if (cards == null) continue;
			foreach (var item in cards)
			{
				string cardName = item?.GetValue<string>();
				CardModel model = CardModel.GetTemplateByClassName(cardName);
				if (model == null)
				{
					GD.PushWarning($"[CardPackDetail] 找不到卡牌：{cardName}");
					continue;
				}
				model.Status = Status.FaceUp;

				var cell = new Control { CustomMinimumSize = cellSize };
				ItemListNode.AddChild(cell);

				NodeCard card = await NodeCard.DisplayCardAt(model, Vector2.Zero, cell, CardView.Collection, fit);
				card.PivotOffset = Vector2.Zero;   // 以左上角为缩放基准，避免缩放后偏移
				_cards.Add(card);

				// 点卡时手动把这张卡顶到最上层（原因见 RaiseCardOnClick 的注释）
				Button hit = card.GetNodeOrNull<Button>("碰撞");
				if (hit != null) hit.GuiInput += (ev) => RaiseCardOnClick(card, ev);
			}
		}
	}

	/// <summary>
	/// 点卡时把这它顶到最上面。<br/>
	/// 两个原因：<br/>
	/// 1. 卡牌现在各自套在一个格子 Control 里，NodeCard 自带的 BringToFront() 是按"同一父节点下的卡"
	///    算 ZIndex 的，它们的父级都是各自的格子、互相看不到，算出来的层级不可靠；<br/>
	/// 2. OnCardClick 里还会 ResetAllZIndex()，把所有卡压到 -4000 —— 详情界面的卡在遮罩面板下面，
	///    负的 ZIndex 会直接被遮罩盖住，表现就是"点一下卡全不见了"。<br/>
	/// 所以这里在点击后把其它卡恢复成 0、被点的卡设 4000（比遮罩高，展开的信息背景才看得见）。
	/// 用 CallDeferred 保证在 NodeCard 自己的 OnCardClick / ResetAllZIndex 之后再执行。
	/// </summary>
	private void RaiseCardOnClick(NodeCard card, InputEvent ev)
	{
		if (ev is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Left) return;
		FixCardZ(card);
		Callable.From(() => FixCardZ(card)).CallDeferred();
	}

	private void FixCardZ(NodeCard top)
	{
		foreach (NodeCard other in _cards)
			if (GodotObject.IsInstanceValid(other) && other != top)
				other.ZIndex = 0;
		if (GodotObject.IsInstanceValid(top))
			top.ZIndex = 4000;
	}

	public override void _ExitTree()
	{
		if (_introTween != null && _introTween.IsValid())
			_introTween.Kill();
	}

	/// <summary>
	/// Godot 4 的 Tween 不能直接 new，必须用 CreateTween()（否则 is_valid() 恒为 false，
	/// TweenProperty 会报 "Tween invalid" 且不推进）。已结束/被杀掉的 Tween 要先用 IsValid() 判断，
	/// 同时字段初始为 null，不能直接对 null 调 IsValid()。
	/// </summary>
	private Tween QuickTween()
	{
		if (_introTween != null && _introTween.IsValid())
			_introTween.Kill();
		_introTween = CreateTween();
		return _introTween;
	}

	/// <summary>从屏幕下方滑入：先把根节点挪到屏幕外一屏，再补间回原位。</summary>
	private void IntroAn()
	{
		if (_introPlayed) return;
		_introPlayed = true;
		Vector2 restPos = Position;
		Position = new Vector2(restPos.X, GetViewportRect().Size.Y);
		QuickTween()
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out)
			.TweenProperty(this, "position", restPos, 0.5f);
	}

	/// <summary>
	/// 离场动画：滑回屏幕下方。SceneManager.ReturnToSub() 会 await 它，
	/// 播完才释放本层场景，所以返回动画写在这里。
	/// </summary>
	public override async Task Outro()
	{
		Vector2 outPos = new Vector2(Position.X, GetViewportRect().Size.Y);
		Tween tween = QuickTween();
		tween.TweenProperty(this, "position", outPos, 0.3f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);
		await ToSignal(tween, Tween.SignalName.Finished);
	}
}
