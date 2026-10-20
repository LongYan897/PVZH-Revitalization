using FileExtension;
using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Linq;
namespace Card;
/// <summary>
/// 生成一个卡包磁贴，需要用到LoadResource方法加载资源。
/// 所有数值来自 数据(C#)/cardPack.json 中该卡包对应的对象。
/// </summary>
public partial class CardTile : CenterContainer
{
	private const string PackDataPath = "res://数据(C#)/cardPack.json";
	private string PackName { get; set; } = "Pack1";
	private string PackTitle { get; set; } = "测试卡包1";
	private string IntroduceText { get; set; } = "这是卡包测试文本，限购4包！\n* 包含1张超稀有卡牌,\n* 3张稀有卡牌。";
	private string CardTexture { get; set; } = "res://素材(C#)/卡包/卡包封面/pack_dawnofthelawn.png";
	private string MainColor { get; set; } = "477fff";
	private string TopColor { get; set; } = "007190";
	private List<CardTileOption> Options = new();
	// 场景节点
	private Label PackNameLabel;
	private TextureRect PackImage;
	private Label IntroduceLabel;
	private Panel Board;
	private Panel TopBanner;
	private Control ButtonGroup;
	public void LoadResource(string packName)
	{
		PackName = packName;

		// ---- 1. 把 Pack1 / Pack2 这一层里的数值全部读出来 ----
		FileJson json = new FileJson().Open(PackDataPath).OpenObject(PackName);
		PackTitle = json.GetVariable("PackName", PackTitle);
		IntroduceText = json.GetVariable("IntroduceText", IntroduceText);
		CardTexture = json.GetVariable("CardTexture", CardTexture);
		MainColor = json.GetVariable("MainColor", MainColor);
		TopColor = json.GetVariable("TopColor", TopColor);
		Options = json.GetArray<JsonObject>("Options")
			.Select(option => new CardTileOption
			{
				ButtonText = option["ButtonText"]?.GetValue<string>() ?? "1包",
				PriceType = option["PriceType"]?.GetValue<string>() ?? "Gem",
				Price = option["Price"]?.GetValue<int>() ?? 0,
				PullCount = option["PullCount"]?.GetValue<int>() ?? 1
			}).ToList();

		// ---- 2. 取节点（底版/购买按钮组 是 Control，挂 AutoFitHBoxContainer.gd，不是 BoxContainer）----
		Board = GetNodeOrNull<Panel>("底版");
		TopBanner = GetNodeOrNull<Panel>("底版/顶部横幅");
		PackNameLabel = GetNodeOrNull<Label>("底版/顶部横幅/卡包名字");
		PackImage = GetNodeOrNull<TextureRect>("底版/卡包图片");
		IntroduceLabel = GetNodeOrNull<Label>("底版/介绍文本");
		ButtonGroup = GetNodeOrNull<Control>("底版/购买按钮组");
		if (ButtonGroup == null)
		{
			GD.PushError($"[CardTile] 场景中缺少 底版/购买按钮组，请检查 卡包.tscn。当前场景：{SceneFilePath}");
			return;
		}

		// ---- 3. 把数值写进界面 ----
		if (PackNameLabel != null) PackNameLabel.Text = PackTitle;
		if (IntroduceLabel != null) IntroduceLabel.Text = IntroduceText;
		if (PackImage != null && !string.IsNullOrWhiteSpace(CardTexture))
		{
			if (ResourceLoader.Exists(CardTexture))
				PackImage.Texture = GD.Load<Texture2D>(CardTexture);
			else
				GD.PushWarning($"[CardTile] {PackName} 的 CardTexture 路径不存在：{CardTexture}");
		}
		SetPanelColor(Board, MainColor);
		SetPanelColor(TopBanner, TopColor);

		// ---- 4. 按 Options 生成购买按钮 ----
		foreach(var Child in ButtonGroup.GetChildren())
		{
			// 先摘出树再释放，避免本帧新旧按钮同时参与 AutoFit 布局
			ButtonGroup.RemoveChild(Child);
			Child.QueueFree();
		}
		var OptionSlot = GD.Load<PackedScene>("res://场景(C#)/卡包/购买按钮槽.tscn");
		if (OptionSlot == null)
		{
			GD.PushError("[CardTile] 无法加载 res://场景(C#)/卡包/购买按钮槽.tscn");
			return;
		}
		foreach (var Option in Options)
		{
			var button = OptionSlot.Instantiate<OptionSlot>();
			// 先塞数据再 AddChild：这里是从 _Load() 里调过来的，整棵子树还在 _EnterTree 阶段，
			// OptionSlot 的 _Ready 还没跑（Godot 会把 ready 推迟到子树全部进入之后），
			// 此时它的 IntroduceLabel / PriceLabel 都是 null，直接 Setup() 会 NRE。
			button.IntroduceText = Option.ButtonText;
			button.PriceCount = Option.Price;
			ButtonGroup.AddChild(button);
			if (button.IsNodeReady())
				button.Setup();                 // 已经 ready 的情况直接填
			else
				button.Ready += button.Setup;   // 等它的 _Ready 跑完（Label 取到了）再填文本
		}
	}

	/// <summary>
	/// 把面板样式里的颜色换成 MainColor / TopColor。
	/// StyleBoxFancy 是 GDScript 资源（addons/StyleboxFancy/StyleBoxFancy.gd），C# 没有强类型，
	/// 只能通过 Variant 接口读写它的 color 字段。
	/// </summary>
	private static void SetPanelColor(Panel panel, string htmlColor)
	{
		if (panel == null || string.IsNullOrWhiteSpace(htmlColor)) return;
		StyleBox style = panel.GetThemeStylebox("panel");
		if (style == null)
		{
			GD.PushWarning($"[CardTile] {panel.Name} 没有 panel 样式，跳过染色");
			return;
		}
		// 场景里的样式是共享子资源，先复制一份再改，避免连带改掉别的节点
		StyleBox copy = (StyleBox)style.Duplicate();
		// StyleBoxFlat 的字段叫 bg_color，只有 StyleBoxFancy 才叫 color
		if (copy.Get("color").VariantType != Variant.Type.Color)
		{
			GD.PushWarning($"[CardTile] {panel.Name} 的样式不是 StyleBoxFancy，跳过染色");
			return;
		}
		copy.Set("color", new Color(htmlColor));
		panel.AddThemeStyleboxOverride("panel", copy);
	}
}
