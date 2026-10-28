using Extension;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 开包界面的控制器：生成若干"3D 卡包"，从屏幕下方依次飞入并竖直叠放。<br/>
/// 注意顺序：<see cref="Card3D.UltimatePos"/> 必须在 AddChild 之前设置，
/// 因为 AddChild 会同步触发 Card3D._Ready（_Ready 里只在未设置时才默认到屏幕中心）。
/// </summary>
public partial class OpenCardControl : Node2D
{
	/// <summary>生成几个卡包</summary>
	[Export] public int CardCount { get; set; } = 10;

	/// <summary>叠放时每张往上错开的像素</summary>
	[Export] public float StackOffset { get; set; } = 25f;

	/// <summary>每张卡的飞入间隔（秒），做出依次飞入的效果</summary>
	[Export] public float StaggerDelay { get; set; } = 0.3f;
	/// <summary>
	/// 时间间隔的动态比值
	/// </summary>
	[Export(PropertyHint.Range,"0,1,0.1")] public float SineRatio = 0.6f;
	private int PileCardCount { get; set; }

    private const string PackScenePath = "res://场景(C#)/卡包/3d卡包.tscn";

	public override async void _Ready()
	{
		PileCardCount = Math.Max(CardCount - 1, 0);
        var packScene = GD.Load<PackedScene>(PackScenePath);
		if (packScene == null)
		{
			GD.PushError($"[OpenCardControl] 找不到 {PackScenePath}");
			return;
		}

		Vector2 center = GetViewportRect().Size / 2;
		float Delay = 0f;
		Random Rand = new();
		float BaseRatio = 1f - SineRatio;
		List<Card3D> Card3DList = new();
		for (int i = 0; i < PileCardCount; i++)
		{
			Vector2 RandomPos = new Vector2(Rand.Next(-5, 5), 0);
			Card3D card = packScene.Instantiate<Card3D>();
			Card3DList.Add(card);
            card.UltimatePos = center + RandomPos + new Vector2(0,300f -StackOffset * i);
            float progress = PileCardCount > 1 ? (float)i / (PileCardCount - 1) : 0f;
            float sinValue = Mathf.Sin(progress * Mathf.Pi);
            float currentInterval = StaggerDelay * (BaseRatio + SineRatio * (1f - sinValue));
			Delay += currentInterval;
            card.StartDelay = (Delay);
			AddChild(card);
		}
		await this.Wait(Delay + 1f);
		var packOpening = GD.Load<PackedScene>("res://场景(C#)/卡包/CardOpening.tscn").Instantiate<CardOpening>();
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
		foreach (var card in Card3DList) card.QueueFree();
		var CardListScene = GD.Load<PackedScene>("res://场景(C#)/卡包/卡包开出列表.tscn").Instantiate<OpeningCardList>();
		AddChild(CardListScene);
        CardListScene.Position = new Vector2(0, GetViewportRect().Size.Y);
		var tween = CreateTween();
		tween.TweenProperty(CardListScene, "position", Vector2.Zero, 1f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.InOut);
    }
}
