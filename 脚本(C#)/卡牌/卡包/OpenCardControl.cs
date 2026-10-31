using Card.Pack;
using Controller;
using Extension;
using FileExtension;
using Godot;
using Play;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 开包界面的控制器：<br/>
/// 1) 生成若干"3D 卡包"，从屏幕下方依次飞入并竖直叠放；<br/>
/// 2) 叠好后生成一个可点击撕开的卡包（<see cref="CardOpening"/>）；<br/>
/// 3) 开包完成后滑入「卡包开出列表」，按 cardPack.json 的分组显示：
///    每组一条组名横幅 + 该组的卡牌，一行最多 4 张（排版和卡包详情一致）。
/// </summary>
public partial class OpenCardControl : Node2D
{
	/// <summary>生成几个卡包</summary>
	[Export] public int CardCount { get; set; } = 10;

	/// <summary>叠放时每张往上错开的像素</summary>
	[Export] public float StackOffset { get; set; } = 25f;

	/// <summary>每张卡的飞入间隔（秒），做出依次飞入的效果</summary>
	[Export] public float StaggerDelay { get; set; } = 0.3f;

	/// <summary>时间间隔的动态比值</summary>
	[Export(PropertyHint.Range, "0,1,0.1")] public float SineRatio = 0.6f;

	/// <summary>抽哪个卡包（cardPack.json 里的对象名）</summary>
	public string PackName { get; set; } = "Pack1";

	/// <summary>抽几张卡</summary>
	public int CardCounts { get; set; } = 100;

	private int PileCardCount { get; set; }

	private const string PackScenePath = "res://场景(C#)/卡包/3d卡包.tscn";
	private const string OpeningScenePath = "res://场景(C#)/卡包/CardOpening.tscn";
	private const string ResultListScenePath = "res://场景(C#)/卡包/卡包开出列表.tscn";
	private const string BannerScenePath = "res://场景(C#)/卡包/横幅.tscn";
	private const string PackDataPath = "res://数据(C#)/cardPack.json";

	/// <summary>结果列表的格子宽度（和卡包详情一致）</summary>
	private const float CardCellWidth = 150f;
	/// <summary>卡牌.tscn 根节点的设计尺寸，用来算缩放比</summary>
	private const float CardNaturalWidth = 255f;
	private const float CardNaturalHeight = 200f;

	/// <summary>组名横幅的尺寸（和 横幅.tscn 的 custom_minimum_size 一致）</summary>
	private const float BannerWidth = 720f;
	private const float BannerHeight = 50f;

	/// <summary>开卡特效：稀有度 → spine skeleton data 资源</summary>
	private static readonly Dictionary<string, string> EffectPaths = new()
	{
		["罕见"] = "res://素材(C#)/卡包/开卡特效/罕见/罕见.tres",
		["稀有"] = "res://素材(C#)/卡包/开卡特效/稀有/稀有.tres",
		["超稀有"] = "res://素材(C#)/卡包/开卡特效/超稀有/超稀有.tres",
		["传说"] = "res://素材(C#)/卡包/开卡特效/传说/传说.tres",
	};

	public override async void _Ready()
	{
		PreloadEffects();          // 先把 4 种开卡特效资源读进缓存，减少第一次播放时的卡顿
		var cardList = CalculateCards();
		PileCardCount = Math.Max(CardCount - 1, 0);

		var packScene = GD.Load<PackedScene>(PackScenePath);
		if (packScene == null)
		{
			GD.PushError($"[OpenCardControl] 找不到 {PackScenePath}");
			return;
		}

		Vector2 center = GetViewportRect().Size / 2;
		float delay = 0f;
		Random rand = new();
		float baseRatio = 1f - SineRatio;
		List<Card3D> card3DList = new();
		for (int i = 0; i < PileCardCount; i++)
		{
			Vector2 randomPos = new Vector2(rand.Next(-5, 5), 0);
			Card3D card = packScene.Instantiate<Card3D>();
			card3DList.Add(card);
			card.UltimatePos = center + randomPos + new Vector2(0, 300f - StackOffset * i);
			float progress = PileCardCount > 1 ? (float)i / (PileCardCount - 1) : 0f;
			float sinValue = Mathf.Sin(progress * Mathf.Pi);
			float currentInterval = StaggerDelay * (baseRatio + SineRatio * (1f - sinValue));
			delay += currentInterval;
			card.StartDelay = delay;
			AddChild(card);
		}

		await this.Wait(delay + 1f);

		var packOpening = GD.Load<PackedScene>(OpeningScenePath).Instantiate<CardOpening>();
		AddChild(packOpening);
		var tcs = new TaskCompletionSource<bool>();
		Action onFinished = null;
		onFinished = () =>
		{
			packOpening.Finished -= onFinished;
			tcs.TrySetResult(true);
		};
		packOpening.Finished += onFinished;
		await tcs.Task;

		foreach (var card in card3DList)
			card.QueueFree();

		var listScene = GD.Load<PackedScene>(ResultListScenePath).Instantiate<OpeningCardList>();
		AddChild(listScene);
		listScene.Position = new Vector2(0, GetViewportRect().Size.Y);
		var tween = CreateTween();
		tween.TweenProperty(listScene, "position", Vector2.Zero, 1f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.InOut);
		await ToSignal(tween, Tween.SignalName.Finished);

		// 动态添加卡牌（分组横幅 + 卡牌）
		await FillResultCards(listScene, cardList);
	}

	/// <summary>
	/// 把抽到的卡按 cardPack.json 里的分组顺序排列：
	/// 每组一条组名横幅，接着是该组抽到的卡；一行最多 4 张、靠左（和卡包详情一样）。
	/// </summary>
	private async Task FillResultCards(OpeningCardList listScene, List<(string Card, int Count)> drawn)
	{
		if (listScene == null || !GodotObject.IsInstanceValid(listScene))
			return;

		FlowContainer list = listScene.NodeList;
		if (list == null)
		{
			GD.PushError("[OpenCardControl] 卡包开出列表里找不到 HFlowContainer");
			return;
		}

		float fit = CardCellWidth / CardNaturalWidth;
		var cellSize = new Vector2(CardCellWidth, CardNaturalHeight * fit);

		var groups = ReadGroups(PackName);
		var usedCards = new HashSet<string>();

		foreach (var (groupName, groupCards) in groups)
		{
			// 保持抽卡结果里的顺序
			var inGroup = drawn.Where(d => groupCards.Contains(d.Card)).ToList();
			if (inGroup.Count == 0) continue;

            await AddGroupBar(list, groupName);

			foreach (var (cardName, count) in inGroup)
			{
				usedCards.Add(cardName);
                await AddCardCell(list, cardName, groupName, cellSize, fit, Math.Max(count, 1));
			}
		}

		// 保险：抽到但没在分组里配到的卡也显示出来，避免"抽到了却看不到"
		var rest = drawn.Where(d => !usedCards.Contains(d.Card)).ToList();
		if (rest.Count > 0)
		{
			GD.PushWarning("[OpenCardControl] 有卡牌不在任何 group 里，已放进「其它」分组");
			await AddGroupBar(list, "其它");
			foreach (var (cardName, count) in rest)
				for (int i = 0; i < Math.Max(count, 1); i++)
					await AddCardCell(list, cardName, "其他", cellSize, fit);
		}
	}

	/// <summary>
	/// 加一条组名横幅（复用卡包详情的横幅.tscn）。<br/>
	/// 注意：横幅不能直接塞进 FlowContainer —— 容器每次排版都会把**直接子节点**的
	/// position/scale 重置成排版值，横幅自己的缩放动画（从左边展开）会被立刻覆盖，
	/// 看起来就是"没有动画"。所以外面套一层普通 Control 当格子，动画加在内层横幅上。
	/// </summary>
	private static async Task AddGroupBar(FlowContainer list, string text)
	{
		var cell = new Control { CustomMinimumSize = new Vector2(BannerWidth, BannerHeight) };
		list.AddChild(cell);

		CardBar bar = GD.Load<PackedScene>(BannerScenePath).Instantiate<CardBar>();
		cell.AddChild(bar);
		bar.IntroduceLabel.Text = text ?? "";
		await bar.IntroIn(0.5f);
	}

	/// <summary>加一张卡牌（外面套固定尺寸的格子，卡牌在格子里按比例缩放）</summary>
	private async Task AddCardCell(FlowContainer list, string cardName,string groupName, Vector2 cellSize, float fit,int count = 1)
	{
		CardModel model = CardModel.GetTemplateByClassName(cardName);
		if (model == null)
		{
			GD.PushWarning($"[OpenCardControl] 找不到卡牌：{cardName}");
			return;
		}
		model.Status = Status.FaceUp;

		var cell = new Control { CustomMinimumSize = cellSize };
		list.AddChild(cell);

		// 动画节点（cell1）：普通 Control，动画加在它身上不会被 FlowContainer 重置。
		// 注意要给它尺寸（CustomMinimumSize）并把 pivot 设到中心，缩放才是"从中心弹出"。
		var cell1 = new Control { CustomMinimumSize = cellSize };
		cell1.PivotOffset = Vector2.Zero;
		cell1.PivotOffsetRatio = new Vector2(0.5f, 0.5f);
		cell.AddChild(cell1);
		//抽卡特效
		await FillEffect(cell1, groupName);

        NodeCard card = await NodeCard.DisplayCardAt(model, Vector2.Zero, cell1, CardView.Collection, fit);
		card.PivotOffset = Vector2.Zero;
		card.PivotOffsetRatio = new Vector2(0.5f, 0.5f);

		// 位置补偿：以中心缩放会把视觉左上角往右下推，反向偏移让静止时仍贴格子左上角。
		Vector2 cardSize = card.GetCombinedMinimumSize();
		card.Position = -cardSize * 0.5f * (1f - fit);

		// 和卡包详情一样：点卡只弹说明面板，不展开信息背景（避免列表里层级/点击问题）
		card.AllowExpand = false;
		card.PreventGray = true;
		card.SetToBright();

		// 设置数量：直接改 ForceShowAmount/ForcedAmount 不会刷新界面，
		// 必须调 SetAmount（内部会 ForceShowAmount=true + ForcedAmount=count + 立即刷新）
		card.SetAmount(count);

		// 弹出动画：必须先把初始 scale 设成 0，再 tween 到 1。
		// （原来没设初始值，从 (1,1) tween 到 (1,1) = 什么都不动，所以"没有动画"）
		cell1.Scale = Vector2.Zero;
		var tween = CreateTween();
		tween.TweenProperty(cell1, "scale", Vector2.One, 0.5f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		await ToSignal(tween, Tween.SignalName.Finished);
    }

	/// <summary>读 cardPack.json 里这个卡包的分组（组名 + 组内卡牌类名）</summary>
	private static List<(string Name, HashSet<string> Cards)> ReadGroups(string packName)
	{
		var groups = new List<(string, HashSet<string>)>();
		try
		{
			FileJson json = new FileJson().Open(PackDataPath).OpenObject(packName);
			foreach (JsonObject group in json.GetArray<JsonObject>("group"))
			{
				string name = group["Name"]?.GetValue<string>() ?? "";
				var set = new HashSet<string>();
				if (group["Cards"] is JsonArray cards)
				{
					foreach (JsonNode node in cards)
					{
						string cardName = node?.GetValue<string>();
						if (!string.IsNullOrEmpty(cardName)) set.Add(cardName);
					}
				}
				groups.Add((name, set));
			}
		}
		catch (Exception e)
		{
			GD.PushWarning($"[OpenCardControl] 读取卡包分组失败：{e.Message}");
		}
		return groups;
	}
	/// <summary>
	/// 创建抽卡特效
	/// </summary>
	/// <summary>预读 4 种稀有度的开卡特效资源（skeleton data），避免第一次播放时同步读盘卡顿</summary>
	private static void PreloadEffects()
	{
		foreach (string path in EffectPaths.Values)
			SpineHandler.GetSkeletonData(path);
	}

	/// <summary>
	/// 在卡片格子上播开卡特效（按稀有度选 spine 动画），播完自动释放。<br/>
	/// </summary>
	public async Task FillEffect(Control fatherNode, string groupName)
	{
		var effect = SpineHandler.Get();
		effect.Modulate = new Color(1, 1, 1, 0);
		fatherNode.AddChild(effect);

		if (!EffectPaths.TryGetValue(groupName, out string path))
			path = EffectPaths["罕见"];

		effect.Scale = Vector2.One * 1.25f;
		effect.LoadSkeletonData(SpineHandler.GetSkeletonData(path));
		effect.Position = new Vector2(75, 50);

		effect.ClearTracks();
		effect.SetAnimation(10, "intro", false);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		effect.Modulate = Colors.White;

		await effect.WaitAnimationCompleteTask();
		if (GodotObject.IsInstanceValid(effect))
			effect.QueueFree();
	}

	/// <summary>
	/// 计算开出的卡牌，开包的一瞬间决定
	/// </summary>
	public List<(string Card, int Count)> CalculateCards()
	{
		CardPackExtensions c1 = new();
		return c1.DrawCard(PackName, CardCounts, PlayerManager.HostPlayer.Asset);
	}
}
