using Card.Pack;
using Controller;
using Controller.WavPlay;
using Extension;
using FileExtension;
using Godot;
using Play;
using Scene;
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
///    每组一条组名横幅 + 该组的卡牌，一行最多 4 张（排版和卡包详情一致）。<br/>
/// 本类是 <see cref="SubMenuScene"/>，由 SceneManager 作为一层子场景打开（见 OptionSlot）。
/// </summary>
public partial class OpenCardControl : SubMenuScene
{
    /// <summary>生成几个卡包</summary>
    [Export] public int CardCount { get; set; } = 10;

    /// <summary>叠放时每张往上错开的像素</summary>
    [Export] public float StackOffset { get; set; } = 25f;

    /// <summary>每张卡的飞入间隔（秒），做出依次飞入的效果</summary>
    [Export] public float StaggerDelay { get; set; } = 0.3f;

    /// <summary>时间间隔的动态比值</summary>
    [Export(PropertyHint.Range, "0,1,0.1")] public float SineRatio = 0.6f;

    /// <summary>收集后要返回的场景（预留；目前由「收集」按钮回主界面）</summary>
    [Export] public string ReturnPath { get; set; }

    /// <summary>3D 卡包用的封面图（由购买按钮根据卡包数据传入）</summary>
    [Export] public string CardTexturePath { get; set; }

    /// <summary>抽哪个卡包（cardPack.json 里的对象名）</summary>
    public string PackName { get; set; } = "Pack1";

    /// <summary>抽几张卡</summary>
    public int CardCounts { get; set; } = 100;

    private int PileCardCount { get; set; }

    private const string PackScenePath = "res://场景(C#)/卡包/3d卡包.tscn";
    private const string OpeningScenePath = "res://场景(C#)/卡包/CardOpening.tscn";
    private const string ResultListScenePath = "res://场景(C#)/卡包/卡包开出列表.tscn";
    private const string BannerScenePath = "res://场景(C#)/卡包/横幅.tscn";
    private const string EndButtonScenePath = "res://场景(C#)/卡包/结束按钮.tscn";
    private const string PackDataPath = "res://数据(C#)/cardPack.json";

    /// <summary>结果列表的格子宽度（和卡包详情一致）</summary>
    private const float CardCellWidth = 150f;
    /// <summary>卡牌.tscn 根节点的设计尺寸，用来算缩放比</summary>
    private const float CardNaturalWidth = 255f;
    private const float CardNaturalHeight = 200f;

    /// <summary>组名横幅的高度</summary>
    private const float BannerHeight = 50f;

    /// <summary>
    /// 内容栏宽度：横幅按这个宽度居中显示。<br/>
    /// 不能让它跟着列表铺满整屏（1280 宽会横贯整个屏幕，看着像一条黑带）。
    /// 一行 4 张卡是 4*150 + 3*12 = 636，720 的栏宽两边各留 42。
    /// </summary>
    private const float ContentWidth = 720f;

    /// <summary>开卡特效：稀有度 → spine skeleton data 资源</summary>
    private static readonly Dictionary<string, string> EffectPaths = new()
    {
        ["罕见"] = "res://素材(C#)/卡包/开卡特效/罕见/罕见.tres",
        ["稀有"] = "res://素材(C#)/卡包/开卡特效/稀有/稀有.tres",
        ["超稀有"] = "res://素材(C#)/卡包/开卡特效/超稀有/超稀有.tres",
        ["传说"] = "res://素材(C#)/卡包/开卡特效/传说/传说.tres",
    };

    /// <summary>开包界面没有全局返回按钮，结束由「收集」按钮负责</summary>
    public override bool HideReturnButton => true;

    public override Task _Load()
    {
        if (string.IsNullOrEmpty(ReturnPath)) ReturnPath = "res://场景(C#)/主节点.tscn";
        if (string.IsNullOrEmpty(CardTexturePath)) CardTexturePath = "res://素材(C#)/卡包/卡包主体/set2_premium_pack.png";

        // 购买按钮通过 SubMenu.Payload 传来的开包参数（哪个卡包、抽几张、显示哪张卡包图）
        if (Payload is OpenPackPayload payload)
        {
            if (!string.IsNullOrEmpty(payload.PackName)) PackName = payload.PackName;
            if (payload.PullCount > 0) CardCounts = payload.PullCount;
            if (!string.IsNullOrEmpty(payload.CardTexturePath)) CardTexturePath = payload.CardTexturePath;
        }
        // 顶部钻石由 SceneManager 管：OptionSlot 建 SubMenu 时把 IsPersistNess 设成 true，
        // SceneManager 进场景后会调 ShowNess(false) 把它藏起来。
        // 这里不要自己去藏 —— _Load 是在 AddChild 里跑的，紧接着 SceneManager 就会 ShowNess(true)
        // 把刚藏的又显示回来（实测过）。
        RunOpeningSafe();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 显示/隐藏顶部的钻石 HUD。<br/>
    /// 它不在本场景里，而是常驻在 <see cref="Main.SceneContainer"/> 的 "Scene/Button/钻石" 节点上。
    /// </summary>
    private static void SetDiamondVisible(bool visible)
    {
        if (Main.SceneContainer == null || !GodotObject.IsInstanceValid(Main.SceneContainer)) return;

        Control diamond = Main.SceneContainer.GetNodeOrNull<Control>("%钻石");
        if (diamond != null && GodotObject.IsInstanceValid(diamond))
            diamond.Visible = visible;
    }

    public override void _ExitTree()
    {
        // 离开这个界面时把钻石还回来。
        // 不能只靠 SceneManager 的 ShowNess：点「收集」走的是 ReturnToSub(null,...)，
        // 如果它是最后一层（Menus 清空），那条分支不会调 ShowNess，钻石会一直藏着回到主界面。
        SetDiamondVisible(true);
    }

    /// <summary>后台跑开包流程，出错时打日志（fire-and-forget 的异常否则会被静默吞掉）</summary>
    private async void RunOpeningSafe()
    {
        try
        {
            await RunOpening();
        }
        catch (Exception e)
        {
            GD.PushError($"[OpenCardControl] 开包流程出错：{e}");
        }
    }

    /// <summary>真正的开包流程（在 _Load 里后台启动，不阻塞 SceneManager 的加载流程）</summary>
    private async Task RunOpening()
    {
        PreloadEffects();          // 先把 4 种开卡特效资源读进缓存，减少第一次播放时的卡顿
        var drawResult = CalculateCards();
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
        Texture2D packTexture = !string.IsNullOrEmpty(CardTexturePath) && ResourceLoader.Exists(CardTexturePath)
            ? GD.Load<Texture2D>(CardTexturePath)
            : null;
        List<Card3D> card3DList = new();
        for (int i = 0; i < PileCardCount; i++)
        {
            Vector2 randomPos = new Vector2(rand.Next(-5, 5), 0);
            Card3D card = packScene.Instantiate<Card3D>();
            card3DList.Add(card);
            if (packTexture != null) card.Texture = packTexture;
            card.UltimatePos = center + randomPos + new Vector2(0, 230f - StackOffset * i);
            float progress = PileCardCount > 1 ? (float)i / (PileCardCount - 1) : 0f;
            float sinValue = Mathf.Sin(progress * Mathf.Pi);
            float currentInterval = StaggerDelay * (baseRatio + SineRatio * (1f - sinValue));
            delay += currentInterval;
            card.StartDelay = delay;
            AddChild(card);
        }

        await this.Wait(delay + 0.5f);

        var packOpening = GD.Load<PackedScene>(OpeningScenePath).Instantiate<CardOpening>();
        AddChild(packOpening);
        packOpening.CardNode.Texture = GD.Load<Texture2D>(CardTexturePath);
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
        await FillResultCards(listScene, drawResult.ByGroup);
        await this.Wait(1f);

        //添加结束按钮
        if (listScene.NodeList == null)
            return;

        var endButton = GD.Load<PackedScene>(EndButtonScenePath).Instantiate<EndButton>();
        // 注意：这里不要设 endButton.Scale —— NodeList 是 HFlowContainer，
        // 容器排版会把直接子节点的 scale 重置成 1，设了也没用。
        // 弹出动画在 EndButton 内部的可视节点上做（见 EndButton.cs）。
        var endPanel = new Panel()
        {
            CustomMinimumSize = new(0, 200)
        };
        listScene.NodeList.AddChild(endPanel);

        // 「收集」按钮要落在屏幕正中：EndButton 自己是 CenterContainer，会把内部按钮摆在
        // 自己矩形的中心，所以把它的宽度撑成整行（而不是它场景里的 720），内部按钮就居中了。
        float rowWidth = listScene.VisibleWidth;
        if (rowWidth > 0)
            endButton.CustomMinimumSize = new Vector2(rowWidth, endButton.CustomMinimumSize.Y);
        listScene.NodeList.AddChild(endButton);
    }

    /// <summary>
    /// 把抽到的卡按 cardPack.json 里的分组顺序排列：
    /// 每组一条组名横幅（内容栏 720 宽、居中），接着是该组抽到的卡；<b>一行最多 4 张、靠左</b>。<br/>
    /// 实现方式：HFlowContainer 只会把子节点从左往右排（Alignment 对它没用），
    /// 所以每一行都套一个和列表等宽的 HBoxContainer（居中），里面再套 720 宽的 HBox（靠左）：
    /// 整行宽的子节点必然独占一行，内容栏本身居中，卡片则从内容栏左边开始排。<br/>
    /// 注意这里用的是 <see cref="CardPackExtensions.PackDrawResult.ByGroup"/>（按组明细），
    /// 不能用按卡名合并的结果：同一张卡被配在多个组里时，合并后数量会累加，显示就对不上了。
    /// </summary>
    private async Task FillResultCards(
        OpeningCardList listScene,
        List<(string Group, string Card, int Count)> drawnByGroup)
    {
        if (listScene == null || !GodotObject.IsInstanceValid(listScene))
            return;

        FlowContainer list = listScene.NodeList;
        if (list == null)
        {
            GD.PushError("[OpenCardControl] 卡包开出列表里找不到 HFlowContainer");
            return;
        }

        // 一行的宽度 = 滚动可视区的宽度（不是 FlowContainer 自己的宽度）。
        // FlowContainer 在 ScrollContainer 里会被撑到比可视区宽得多（730 的可视区里它是 1280），
        // 按它来居中，内容会被整体推到面板外面。
        float rowWidth = listScene.VisibleWidth;
        if (rowWidth <= 0) rowWidth = GetViewportRect().Size.X;

        float fit = CardCellWidth / CardNaturalWidth;
        var cellSize = new Vector2(CardCellWidth, CardNaturalHeight * fit);

        string currentGroup = null;
        var shownCards = new HashSet<string>();
        HBoxContainer row = null;
        int inRow = 0;

        foreach (var (groupName, cardName, count) in drawnByGroup)
        {
            if (count <= 0) continue;

            if (groupName != currentGroup)
            {
                currentGroup = groupName;
                row = null;      // 新的一组从新的一行开始
                inRow = 0;
                await AddGroupBar(list, string.IsNullOrEmpty(groupName) ? "其它" : groupName, rowWidth);
            }

            // 数据检查：同一张卡被配在多个组里时，会在多个组下各显示一次，
            // 这不是显示逻辑的问题，是 cardPack.json 的数据配置问题，打条警告方便发现。
            if (!shownCards.Add(cardName))
            {
                GD.PushWarning(
                    $"[OpenCardControl] 卡牌 {cardName} 同时出现在多个分组里，开包结果会在多个组下重复显示，" +
                    "建议把 cardPack.json 里同一张卡只配在一个 group 中。");
            }

            if (row == null || inRow >= OpeningCardList.CardsPerRow)
            {
                row = NewLine(list, rowWidth);
                inRow = 0;
            }

            await AddCardCell(row, cardName, groupName, cellSize, fit, count);
            inRow++;
        }
    }

    /// <summary>
    /// 造一行：外层是整行宽、居中的 HBox（这样它必然独占 HFlowContainer 的一行），
    /// 里面再套一个 <see cref="ContentWidth"/> 宽、<b>靠左对齐</b>的 HBox ——
    /// 卡牌就从这个内容栏的左边缘开始排，不满一行（哪怕只有一张）也靠左。<br/>
    /// 返回的是内层那个 HBox，往里塞卡片格子即可。
    /// </summary>
    private static HBoxContainer NewLine(FlowContainer list, float rowWidth)
    {
        var line = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            CustomMinimumSize = new Vector2(rowWidth, 0),
        };
        list.AddChild(line);

        var items = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Begin,
            CustomMinimumSize = new Vector2(Mathf.Min(ContentWidth, rowWidth), 0),
        };
        items.AddThemeConstantOverride("separation", OpeningCardList.Separation);
        line.AddChild(items);
        return items;
    }

    /// <summary>
    /// 加一条组名横幅（复用卡包详情的横幅.tscn）。<br/>
    /// 格子占满整行（这样它必然独占一行、好居中），但横幅本体只有 <see cref="ContentWidth"/> 宽，
    /// 左右居中摆在格子里 —— 不要让横幅跟着铺满整屏。<br/>
    /// 注意：横幅不能直接塞进 FlowContainer —— 容器每次排版都会把**直接子节点**的
    /// position/scale 重置成排版值，横幅自己的缩放动画（从左边展开）会被立刻覆盖，
    /// 看起来就是"没有动画"。所以外面套一层普通 Control 当格子，动画加在内层横幅上。
    /// </summary>
    private static async Task AddGroupBar(Container list, string text, float rowWidth)
    {
        float width = Mathf.Min(ContentWidth, rowWidth);

        var cell = new Control { CustomMinimumSize = new Vector2(rowWidth, BannerHeight) };
        list.AddChild(cell);

        CardBar bar = GD.Load<PackedScene>(BannerScenePath).Instantiate<CardBar>();
        bar.CustomMinimumSize = new Vector2(width, BannerHeight);
        bar.Size = new Vector2(width, BannerHeight);
        bar.Position = new Vector2((rowWidth - width) * 0.5f, 0f);
        cell.AddChild(bar);
        bar.IntroduceLabel.Text = text ?? "";
        await bar.IntroIn(0.5f);
    }

    /// <summary>加一张卡牌（外面套固定尺寸的格子，卡牌在格子里按比例缩放）</summary>
    private async Task AddCardCell(Container list, string cardName, string groupName, Vector2 cellSize, float fit, int count = 1)
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
        FillEffectSFX(groupName);
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
        card.SetAmount(count);
        await CardIntroAn(card, groupName, fit);
    }
    private async Task CardIntroAn(NodeCard card,string groupName,float fit)
    {
        Tween tween = CreateTween();
        Tween tween1 = null;
        if (groupName == "罕见")
        {
            card.Scale = new(0, 0.4f);
            tween.TweenProperty(card, "scale", Vector2.One * fit * 1f, 0.35f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
        }
        else if (groupName == "稀有")
        {
            card.Scale = new(0, 0);
            card.Rotation = Mathf.Pi * -0.2f;
            tween.TweenProperty(card, "scale", Vector2.One * fit * 1.3f, 0.15f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(card, "scale", Vector2.One * fit, 0.45f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.InOut);

            tween1 = CreateTween();
            tween1.TweenProperty(card, "rotation",0, 0.7f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
        }
        else if (groupName == "超稀有")
        {
            card.Scale = new(2f, 2f);
            card.Rotation = Mathf.Pi * -1f;
            tween.TweenProperty(card, "scale", Vector2.One * fit, 0.2f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(card, "scale", Vector2.One * fit * 1.4f, 0.35f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.InOut)
                .SetDelay(0.1f);
            tween.TweenProperty(card, "scale", Vector2.One * fit, 0.35f);

            tween1 = CreateTween();
            tween1.TweenProperty(card, "rotation", 0, 0.4f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
        }
        else if (groupName == "传说")
        {
            card.Scale = new(0,0);
            tween.TweenProperty(card, "scale", Vector2.One * fit * 2f, 0.4f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(card, "scale", Vector2.One * fit * 1.2f, 0.4f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.InOut);
            tween.TweenProperty(card, "scale", Vector2.One * fit * 1f, 0.4f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.InOut);

            tween1 = CreateTween();
            tween1.TweenProperty(card, "rotation", Mathf.Pi * -0.1f, 0.4f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
            tween1.TweenProperty(card, "rotation", Mathf.Pi * 0.075f, 0.4f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
            tween1.TweenProperty(card, "rotation", -0.05f, 0.4f);
            tween1.TweenProperty(card, "rotation", 0, 0.4f);
        }
        else
        {
            card.Scale = new(0, 0.4f);
            tween.TweenProperty(card, "scale", Vector2.One * fit * 1.2f, 0.25f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(card, "scale", Vector2.One * fit, 0.25f);
        }
        // 注意：两个补间是同时开始的，必须先"同时等"。
        // 如果先 await tween 再 await tween1，而 tween1 更短、已经播完，
        // 它的 Finished 信号不会再触发 → 永远卡在这里。
        var waits = new List<Task> { ToSignal(tween, Tween.SignalName.Finished).ToTask() };
        if (tween1 != null)
            waits.Add(ToSignal(tween1, Tween.SignalName.Finished).ToTask());
        await Task.WhenAll(waits);
    }
    /// <summary>预读 4 种稀有度的开卡特效资源（skeleton data），避免第一次播放时同步读盘卡顿</summary>
    private static void PreloadEffects()
    {
        foreach (string path in EffectPaths.Values)
            SpineHandler.GetSkeletonData(path);
    }

    /// <summary>
    /// 在卡片格子上播开卡特效（按稀有度选 spine 动画），播完自动释放。<br/>
    /// "第一帧是默认姿势"的原因：spine 在动画状态被 _process 更新之前，会先按 skeleton 的
    /// setup 姿势渲染一帧。这里的做法是：先设成完全透明（但保持 visible，spine 才会正常更新），
    /// 设好 skeleton data + 动画后等两帧让姿势走到 intro 第一帧，再显形，就不会闪默认姿势了。
    /// </summary>
    public async Task FillEffect(Control fatherNode, string groupName)
    {
        Vector2 offset = new(75, 50);

        if (groupName == "超稀有")
        {
            Action backgroundAn = async () =>
            {
                var background = SpineHandler.Get();
                background.Modulate = new Color(1, 1, 1, 0);
                fatherNode.AddChild(background);

                EffectPaths.TryGetValue("超稀有", out string path1);
                background.LoadSkeletonData(SpineHandler.GetSkeletonData(path1));
                background.Position = offset;
                background.ZIndex = 0;
                background.ClearTracks();
                background.SetAnimationAndFreeOnEnd(10, "backgroud", false);

                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                background.Modulate = Colors.White;
            };
            backgroundAn();
        }

        var effect = SpineHandler.Get();
        effect.Modulate = new Color(1, 1, 1, 0);
        fatherNode.AddChild(effect);

        if (!EffectPaths.TryGetValue(groupName, out string path))
            path = EffectPaths["罕见"];

        effect.Scale = Vector2.One * 1.25f;
        effect.LoadSkeletonData(SpineHandler.GetSkeletonData(path));
        effect.Position = offset;

        // 注意：Godot 的 z_index 上限是 4096，超过会被忽略（引擎会打 clamped 警告）；
        if (groupName == "超稀有")
            effect.ZIndex = 4080;

        effect.ClearTracks();
        var animationCompleteTask = effect.WaitAnimationCompleteTask();
        var finishedTask = effect.SetAnimationAndWaitEvent(10, "intro", "finished", false);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        effect.Modulate = Colors.White;

        await finishedTask;

        Func<Task> freeAfterAnimation = async () =>
        {
            await animationCompleteTask;
            if (GodotObject.IsInstanceValid(effect))
                effect.QueueFree();
        };
        _ = freeAfterAnimation();
    }
    /// <summary>按稀有度播一次性开包音效（CustomWav 播完会自动释放播放器）</summary>
    private static void FillEffectSFX(string groupName)
    {
        string path = groupName switch
        {
            "罕见" => "res://素材(C#)/卡包/开包音频/罕见.mp3",
            "稀有" => "res://素材(C#)/卡包/开包音频/稀有.mp3",
            "超稀有" => "res://素材(C#)/卡包/开包音频/超稀有.mp3",
            "传说" => "res://素材(C#)/卡包/开包音频/传说.mp3",
            _ => "res://素材(C#)/卡包/开包音频/罕见.mp3"
        };
        WavPlayer.Play<CustomWav>(path);
    }

    /// <summary>
    /// 计算开出的卡牌，开包的一瞬间决定。<br/>
    /// 用 DrawPack（带"按组"明细）：同一张卡如果被配在多个组里，
    /// 按卡名合并会把各组数量加在一起，展示时就会"数量对不上"。
    /// </summary>
    public CardPackExtensions.PackDrawResult CalculateCards()
    {
        CardPackExtensions c1 = new();
        return c1.DrawPack(PackName, CardCounts, PlayerManager.HostPlayer.Asset);
    }
}
