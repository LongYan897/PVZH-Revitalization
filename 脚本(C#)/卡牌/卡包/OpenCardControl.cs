using Card.Pack;
using Controller;
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

        // 关键：开包是"长流程"（飞包 + 等玩家点击撕包 + 展示结果），不能 await 在这里。
        // _Load() 被 SubMenuScene._EnterTree await，SceneManager 会等 LoadTask 完成才把这一层
        // 设为 Visible —— 如果这里 await 整个开包流程，界面会一直不可见（看着像卡死）。
        _ = RunOpening();
        return Task.CompletedTask;
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
        // 卡包封面图（按 Payload 里传的卡包换图，没有就用 3d卡包.tscn 自带的）
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
            card.UltimatePos = center + randomPos + new Vector2(0, 300f - StackOffset * i);
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
        var endButton = GD.Load<PackedScene>(EndButtonScenePath).Instantiate<EndButton>();
        // 注意：这里不要设 endButton.Scale —— NodeList 是 HFlowContainer，
        // 容器排版会把直接子节点的 scale 重置成 1，设了也没用。
        // 弹出动画在 EndButton 内部的可视节点上做（见 EndButton.cs）。
        var endPanel = new Panel()
        {
            CustomMinimumSize = new(0, 200)
        };
        listScene.NodeList.AddChild(endPanel);
        listScene.NodeList.AddChild(endButton);
    }

    /// <summary>
    /// 把抽到的卡按 cardPack.json 里的分组顺序排列：
    /// 每组一条组名横幅，接着是该组抽到的卡；一行最多 4 张、靠左（和卡包详情一样）。<br/>
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

        float fit = CardCellWidth / CardNaturalWidth;
        var cellSize = new Vector2(CardCellWidth, CardNaturalHeight * fit);

        string currentGroup = null;
        var shownCards = new HashSet<string>();

        foreach (var (groupName, cardName, count) in drawnByGroup)
        {
            if (count <= 0) continue;

            if (groupName != currentGroup)
            {
                currentGroup = groupName;
                await AddGroupBar(list, string.IsNullOrEmpty(groupName) ? "其它" : groupName);
            }

            // 数据检查：同一张卡被配在多个组里时，会在多个组下各显示一次，
            // 这不是显示逻辑的问题，是 cardPack.json 的数据配置问题，打条警告方便发现。
            if (!shownCards.Add(cardName))
            {
                GD.PushWarning(
                    $"[OpenCardControl] 卡牌 {cardName} 同时出现在多个分组里，开包结果会在多个组下重复显示，" +
                    "建议把 cardPack.json 里同一张卡只配在一个 group 中。");
            }

            await AddCardCell(list, cardName, groupName, cellSize, fit, count);
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
    private async Task AddCardCell(FlowContainer list, string cardName, string groupName, Vector2 cellSize, float fit, int count = 1)
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
        card.SetAmount(count);
        await CardIntroAn(card, groupName, fit);
    }
    private async Task CardIntroAn(NodeCard card,string groupName,float fit)
    {
        // 注意：Tween 必须至少有一个 Tweener 才能启动，
        // 空的 Tween 会报 "Tween ... started with no Tweeners"，而且 await Finished 永远不会返回（卡死）。
        // 所以 tween1 只在真正用到时创建，没用到的分组不要 await 它。
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
