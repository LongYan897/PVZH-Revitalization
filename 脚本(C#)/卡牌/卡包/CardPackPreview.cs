using FileExtension;
using Godot;
using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 把一个卡包「可能包含的卡」铺进任意容器。<br/>
/// 两个地方共用：<br/>
/// 1) 卡包详情场景（CardPackDetail）——容器是 FlowContainer，一排 4 张，自动折行；<br/>
/// 2) 卡包列表右侧详情栏（CardPackButton1 点击时）——容器是 VBoxContainer，每行自己拼一个
///    HBoxContainer，因为 VBox 只会竖着排，不会自动折行。<br/>
/// 每个 group 先来一条横幅（横幅.tscn / CardBar），再跟它下面的卡牌。
/// </summary>
public static class CardPackPreview
{
    public const string PackDataPath = "res://数据(C#)/cardPack.json";
    private const string BarScenePath = "res://场景(C#)/卡包/横幅.tscn";

    /// <summary>卡牌.tscn 根节点的设计尺寸（custom_minimum_size），用来算缩放比</summary>
    public const float CardNaturalWidth = 255f;
    public const float CardNaturalHeight = 200f;

    /// <summary>
    /// 清空容器里上次铺进去的东西，但保留场景自带的占位节点（比如「顶部占位」）。<br/>
    /// 注意：先 RemoveChild 再 QueueFree。QueueFree 是延迟到帧末的，如果只调 QueueFree，
    /// 同一帧里新旧卡片会同时参与容器排版，画面会闪一下、滚动条也会跳。
    /// </summary>
    public static void Clear(Container target, params string[] keepNames)
    {
        if (target == null) return;

        foreach (Node child in target.GetChildren())
        {
            bool keep = false;
            if (keepNames != null)
            {
                string childName = child.Name.ToString();
                foreach (string keepName in keepNames)
                {
                    if (childName == keepName) { keep = true; break; }
                }
            }
            if (keep) continue;

            target.RemoveChild(child);
            child.QueueFree();
        }
    }

    /// <summary>
    /// 按 cardPack.json 里 <paramref name="packName"/> 的 group 数据铺横幅 + 卡牌。<br/>
    /// FlowContainer：直接按顺序塞，靠它自己折行（cardsPerRow 忽略）。<br/>
    /// 其它容器（VBoxContainer 等）：每 <paramref name="cardsPerRow"/> 张塞进一个 HBoxContainer。
    /// </summary>
    /// <param name="target">目标容器</param>
    /// <param name="packName">cardPack.json 里的对象名，如 Pack1</param>
    /// <param name="cardsPerRow">一行几张（只在非 FlowContainer 时生效）</param>
    /// <param name="cardCellWidth">每张卡占的格子宽度</param>
    /// <param name="separation">卡牌间距</param>
    /// <param name="animateIn">
    /// true = 铺的时候先摆成"还没出现"（卡牌在右边、透明；横幅压扁、透明），
    /// 由调用方再播 <see cref="CardPackTransition.InAsync"/> 飞进来。
    /// false = 直接摆好（详情场景用）。
    /// </param>
    public static async Task Fill(
        Container target,
        string packName,
        int cardsPerRow = 4,
        float cardCellWidth = 150f,
        int separation = 12,
        bool animateIn = false)
    {
        if (target == null) return;
        if (string.IsNullOrWhiteSpace(packName)) return;

        FileJson json;
        try
        {
            json = new FileJson().Open(PackDataPath).OpenObject(packName);
        }
        catch (Exception e)
        {
            GD.PushError($"[CardPackPreview] 读取 {PackDataPath} 里的 {packName} 失败：{e.Message}");
            return;
        }

        // 卡牌外面必须套一个固定尺寸的"格子"（普通 Control）：容器是按 custom_minimum_size 排版的，
        // 而且 Container 会把子节点的 scale 重置成 1，直接给卡牌设 scale 再塞进容器是排不对的。
        float fit = cardCellWidth / CardNaturalWidth;
        var cellSize = new Vector2(cardCellWidth, CardNaturalHeight * fit);

        // FlowContainer 自己会折行；其它容器要我们自己分行
        bool useRows = target is not FlowContainer;
        if (cardsPerRow < 1) cardsPerRow = 1;

        PackedScene barScene = GD.Load<PackedScene>(BarScenePath);
        if (barScene == null)
            GD.PushError($"[CardPackPreview] 无法加载 {BarScenePath}");

        // 飞入起点：要偏到屏幕右边外面，否则能看到卡牌"凭空出现"的位置
        float flyOffsetX = CardPackTransition.ComputeFlyOffset(target, cardCellWidth);

        foreach (JsonObject group in json.GetArray<JsonObject>("group"))
        {
            // ---- 1) 这个分组的横幅 ----
            if (barScene != null)
            {
                var bar = barScene.Instantiate<CardBar>();
                if (useRows)
                {
                    // 行模式：横幅必须套在普通的 Control 格子里。
                    // 直接当 VBox 的子节点的话，容器每次排版都会把它的 scale 重置成 1，
                    // CardPackTransition 里的「压扁 / 长大」动画就看不见了。
                    // 另外 横幅.tscn 的 custom_minimum_size 是 720 宽，塞进 480 宽的栏里会横向溢出，
                    // 所以宽度归零、高度用格子固定，靠 anchors 铺满格子。
                    var barCell = new Control
                    {
                        CustomMinimumSize = new Vector2(0, CardPackTransition.BarHeight),
                        MouseFilter = Control.MouseFilterEnum.Ignore,
                        Name = "横幅格",
                    };
                    target.AddChild(barCell);

                    bar.CustomMinimumSize = Vector2.Zero;
                    barCell.AddChild(bar);
                    bar.SetAnchorsPreset(Control.LayoutPreset.FullRect);

                    if (animateIn) CardPackTransition.HideForIn(barCell, bar);
                }
                else
                {
                    target.AddChild(bar);
                }

                if (bar.IntroduceLabel != null)
                    bar.IntroduceLabel.Text = group["Name"]?.GetValue<string>() ?? "";
            }

            var cards = group["Cards"]?.AsArray();
            if (cards == null) continue;

            // ---- 2) 这个分组的卡牌 ----
            HBoxContainer row = null;
            int inRow = 0;
            foreach (var item in cards)
            {
                string cardName = item?.GetValue<string>();
                CardModel model = CardModel.GetTemplateByClassName(cardName);
                if (model == null)
                {
                    GD.PushWarning($"[CardPackPreview] 找不到卡牌：{cardName}");
                    continue;
                }
                model.Status = Status.FaceUp;

                Container slotParent;
                if (useRows)
                {
                    if (row == null || inRow >= cardsPerRow)
                    {
                        row = new HBoxContainer();
                        row.AddThemeConstantOverride("separation", separation);
                        target.AddChild(row);
                        inRow = 0;
                    }
                    slotParent = row;
                    inRow++;
                }
                else
                {
                    slotParent = target;
                }

                var cell = new Control { CustomMinimumSize = cellSize };
                slotParent.AddChild(cell);

                NodeCard card = await NodeCard.DisplayCardAt(model, Vector2.Zero, cell, CardView.Collection, fit);

                // 等待期间可能又被点了一次（清空重铺）：格子已被摘出树，这次就别再动了
                if (!GodotObject.IsInstanceValid(cell) || !cell.IsInsideTree()) return;
                if (card == null || !GodotObject.IsInstanceValid(card)) continue;

                // 缩放以卡牌"中心"为基准。
                // Godot 4.7 里实际 pivot = PivotOffset + Size × PivotOffsetRatio（两者相加），
                // 卡牌.tscn 根节点本来就有 pivot_offset=(128,96.5)，所以必须先把 PivotOffset 清零。
                card.PivotOffset = Vector2.Zero;
                card.PivotOffsetRatio = new Vector2(0.5f, 0.5f);

                // 位置补偿：以中心缩放会把卡牌视觉左上角往右下推 pivot×(1-scale)，
                // 这里把 Position 反向偏移同样的距离，保证静止时卡牌左上角仍贴在格子左上角。
                Vector2 cardSize = card.GetCombinedMinimumSize();
                card.Position = -cardSize * 0.5f * (1f - fit);
                card.AllowExpand = false;
                card.PreventGray = true;
                card.SetToBright();

                // 先把牌摆到屏幕右边外面，等 CardPackTransition.InAsync 统一飞进来
                if (animateIn) CardPackTransition.HideForIn(card, flyOffsetX);
            }
        }
    }
}
