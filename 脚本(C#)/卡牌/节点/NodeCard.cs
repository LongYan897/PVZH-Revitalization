using Battle;
using Battle.Entity;
using Card.Cmd;
using Controller;
using Godot;
using Godot.Collections;
using Hero;
using Logger;
using Pack;
using Phrases;
using Play;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Target;

namespace Card;

/// <summary>
/// 卡牌的预览模式
/// </summary>
public enum CardView
{
    /// <summary>
    /// 战斗中
    /// </summary>
    Battle,
    /// <summary>
    /// 图鉴中
    /// </summary>
    Collection,
    /// <summary>
    /// 卡组中
    /// </summary>
    Deck
}
/// <summary>
/// 卡牌的可视化部分
/// </summary>
[GlobalClass]
public partial class NodeCard : Control
{
    /// <summary>
    /// 是否强制显示卡牌数量。
    /// </summary>
    public bool ForceShowAmount { get; set; } = false;

    /// <summary>
    /// 强制显示的卡牌数量。
    /// </summary>
    public int ForcedAmount { get; set; } = 0;
    /// <summary>
    /// 是否禁止卡牌被置灰。
    /// 置为 true 会立即清除已应用的灰度；置回 false 会按当前数量重新判断。
    /// </summary>
    public bool PreventGray
    {
        get => _preventGray;
        set
        {
            if (_preventGray == value) return;
            _preventGray = value;

            if (_preventGray)
                SetGray(false);
            else if (Model != null && CardViewMode != CardView.Battle)
                FreshAmount();
        }
    }
    private bool _preventGray;
    public static void Clear()
    {
        foreach (var inst in instances.ToList())
        {
            if (!GodotObject.IsInstanceValid(inst.Key))
            {
                instances.Remove(inst.Key);
                continue;
            }
            instances.Remove(inst.Key);
            inst.Key.QueueFree();
        }
    }
    private CardView CardViewMode;
    private static readonly PackedScene Scene = GD.Load<PackedScene>("res://场景(C#)/卡牌.tscn");
    public static NodeCard ChoiceCard { get; private set; }
    public CardModel Model { get; private set; }
    /// <summary>
    /// 是否允许点卡展开信息背景。<br/>
    /// 纯展示的列表（如卡包详情）把它设成 false：点卡只弹 <see cref="CardDes"/> 说明面板，
    /// 不展开、不改 ZIndex，避免网格布局里出现点不到/卡被遮住的问题。
    /// </summary>
    public bool AllowExpand { get; set; } = true;
    private static readonly System.Collections.Generic.Dictionary<NodeCard, CardModel> instances = new();
    private const int maxInstance = int.MaxValue;
    /// <summary>
    /// 当前展开信息背景的卡，全局唯一
    /// </summary>
    private static NodeCard _openedCard;
    /// <summary>
    /// 卡牌初始大小
    /// </summary>
    private const float BaseScaleFactor = 0.56f;
    private Vector2 _baseScale;

    /// <summary>
    /// 从一个卡牌中创建一个卡牌节点并自动挂载到父节点
    /// </summary>
    /// <param name="cardModel">卡牌</param>
    /// <param name="position">初始位置</param>
    /// <param name="parent">父节点，为 null 时使用 Main.CardContainer</param>
    /// <param name="drawAnimation">是否播放抽卡动画</param>
    /// <param name="scaleFactor">缩放系数，默认 BaseScaleFactor</param>
    public static async Task<NodeCard> CreateCard(
    CardModel cardModel,
    Vector2 position,
    CardView cardView = CardView.Battle,
    Node parent = null,
    bool drawAnimation = false,
    float scaleFactor = BaseScaleFactor)
    {
        parent ??= Main.CardContainer;

        if (instances.Count < maxInstance)
        {
            var node = Scene.Instantiate<NodeCard>();
            node.Model = cardModel;
            node.Position = position;
            node._baseScale = new Vector2(scaleFactor, scaleFactor);
            node.Scale = node._baseScale;
            node.CardViewMode = cardView;
            node.Visible = false;
            parent.AddChild(node);
            instances.Add(node, cardModel);
            node.BringToFront();
            if (drawAnimation)
                await node.DrawAnimation();
            else
                node.Fresh();
            return node;
        }
        else
        {
            var node = instances.First(p => p.Key.Model == null).Key;
            if (node.GetParent() != parent)
            {
                node.Reparent(parent, true);
            }

            node.Model = cardModel;
            node.Position = position;
            node._baseScale = new Vector2(scaleFactor, scaleFactor);
            node.Scale = node._baseScale;
            node.CardViewMode = cardView;
            node.Visible = false;
            instances[node] = cardModel;
            node.BringToFront();
            node.GetNode<Button>("碰撞").MouseFilter = Control.MouseFilterEnum.Stop;
            if (drawAnimation)
                await node.DrawAnimation();
            else
            {
                var drawFrame = node.GetNode<Sprite2D>("%特效");
                drawFrame.Visible = false;
                node.Fresh();
            }
            return node;
        }
    }

    /// <summary>
    /// 在指定 CanvasLayer 下创建一张卡。
    /// group 为 null 时退回默认父节点 Main.CardContainer。
    /// </summary>
    /// <param name="cardModel">卡牌</param>
    /// <param name="position">初始位置</param>
    /// <param name="group">目标 CanvasLayer</param>
    /// <param name="cardView">预览模式</param>
    /// <param name="drawAnimation">是否播放抽卡动画</param>
    /// <param name="scaleFactor">缩放系数</param>
    public static Task<NodeCard> CreateCardInGroup(
        CardModel cardModel,
        Vector2 position,
        CanvasLayer group,
        CardView cardView = CardView.Battle,
        bool drawAnimation = false,
        float scaleFactor = BaseScaleFactor)
    {
        Node parent = group;

        var container = group?.GetNodeOrNull<Node>("CardContainer");
        if (container != null)
            parent = container;

        return CreateCard(cardModel, position, cardView, parent, drawAnimation, scaleFactor);
    }

    /// <summary>
    /// 从一个卡牌中创建一个卡牌节点并自动挂载到父节点
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="cardModel">卡牌</param>
    public static Task<NodeCard> DisplayCardAt(CardModel cardModel, Vector2 position, Node parent, CardView cardView = CardView.Battle, float scaleFactor = BaseScaleFactor)
        => CreateCard(cardModel, position, cardView, parent, drawAnimation: false, scaleFactor: scaleFactor);

    /// <summary>
    /// 从一个卡牌中创建一个卡牌节点并自动挂载到父节点(播放抽卡动画)
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="cardModel">卡牌</param>
    public static Task<NodeCard> DrawACardAt(CardModel cardModel, Vector2 position, Node parent, CardView cardView = CardView.Battle, float scaleFactor = BaseScaleFactor)
        => CreateCard(cardModel, position, cardView, parent, drawAnimation: true, scaleFactor: scaleFactor);

    /// <summary>
    /// 从一个卡牌中创建一个卡牌节点并自动挂载到父节点
    /// </summary>
    /// <param name="cardModel">卡牌</param>
    public static Task<NodeCard> DisplayCard(CardModel cardModel, Vector2 position, CardView cardView = CardView.Battle, float scaleFactor = BaseScaleFactor)
        => CreateCard(cardModel, position, cardView, null, drawAnimation: false, scaleFactor: scaleFactor);

    /// <summary>
    /// 从一个卡牌中创建一个卡牌节点并自动挂载到父节点(播放抽卡动画)
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="cardModel">卡牌</param>
    public static Task<NodeCard> DrawACard(CardModel cardModel, Vector2 position, CardView cardView = CardView.Battle, float scaleFactor = BaseScaleFactor)
        => CreateCard(cardModel, position, cardView, null, drawAnimation: true, scaleFactor: scaleFactor);

    /// <summary>
    /// 暂时隐藏一个卡牌节点
    /// </summary>
    /// <param name="card"></param>
    public static async Task DestoryCard(CardModel card)
    {
        var node = instances.FirstOrDefault(n => n.Value == card);
        await node.Key?.Close();
    }
    /// <summary>
    /// 设置卡牌层级，每个 index 对应 40 的 ZIndex。
    /// 同一父级下同一 index 只允许一张卡，已在该层的其它卡顺移到下一层。
    /// </summary>
    public void SetIndex(int index)
    {
        int target = -4000 + index * 40;
        var myParent = GetParent();

        NodeCard occupant = null;
        foreach (var node in instances.Keys.ToList())
        {
            if (node == this) continue;
            if (!GodotObject.IsInstanceValid(node)) continue;
            if (node.GetParent() != myParent) continue;

            if (node.ZIndex == target)
            {
                occupant = node;
                break;
            }
        }

        if (occupant == null)
        {
            ZIndex = target;
            return;
        }

        occupant.SetIndex(index - 1);
        ZIndex = target;
    }

    /// <summary>
    /// 把这张卡放到同父级下的最高层。
    /// </summary>
    public void BringToFront()
    {
        var myParent = GetParent();
        int maxIndex = -1;

        foreach (var node in instances.Keys)
        {
            if (!GodotObject.IsInstanceValid(node)) continue;
            if (node.GetParent() != myParent) continue;

            int index = (8000) / 40;
            if (index > maxIndex) maxIndex = index;
        }

        SetIndex(maxIndex);
    }

    /// <summary>
    /// 把所有卡牌的 ZIndex 重置为最底层
    /// </summary>
    private static void ResetAllZIndex()
    {
        foreach (var node in instances.Keys.ToList())
        {
            if (!GodotObject.IsInstanceValid(node)) continue;
            node.ZIndex = -4000;
        }
    }
    /// <summary>
    /// 获得当前卡牌对应的节点
    /// </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    public static NodeCard GetNode(CardModel card)
    {
        return instances.FirstOrDefault(k => k.Value == card).Key;
    }

    /// <summary>
    /// 绑定的初始位置
    /// </summary>
    private Vector2? _boundOriginPos;

    /// <summary>
    /// 绑定初始位置，拖动结束后会回到这里
    /// </summary>
    public void BindOrigin(Vector2 position, float duration = 0.9f)
    {
        _boundOriginPos = position;
        _cardOriginPos = position;
        ReturnToOrigin(duration);
    }
    /// <summary>
    /// 绑定初始位置，拖动结束后会回到这里
    /// </summary>
    public async Task BindOriginTask(Vector2 position, float duration = 0.9f)
    {
        _boundOriginPos = position;
        _cardOriginPos = position;
        await ReturnToOriginTask(duration);
    }
    /// <summary>
    /// 绑定当前坐标作为初始位置
    /// </summary>
    public void BindOrigin()
    {
        BindOrigin(Position);
    }
    /// <summary>
    /// 点亮该卡牌但是不会改变卡牌是否可打出状态
    /// </summary>
    public void SetToBright()
    {
        DarkenLight();
    }
    /// <summary>
    /// 重抽一张卡牌
    /// </summary>
    /// <param name="cardModel">替换卡牌</param>
    /// <param name="position">新的坐标(不输入默认不换坐标)</param>
    public async Task RedrawCard(CardModel cardModel, Vector2 position = new())
    {
        CardCmd.PushStop();
        Model = cardModel;
        if (position != new Vector2())
            Position = position;
        await DrawAnimation();
        DarkenLight();
        instances[this] = cardModel;
        CardCmd.PopStop();
    }
    private async Task DrawAnimation()
    {
        var drawFrame = GetNode<Sprite2D>("%特效");
        drawFrame.Visible = true;
        Fresh(true);
        DarkenLight();
        GetNode<Node2D>("正面卡牌").Visible = false;
        GetNode<TextureRect>("%牌背").Visible = false;
        drawFrame.Modulate = new Color(0, 0, 0, 0);
        drawFrame.Rotation = 0;
        drawFrame.Scale = new Vector2(1.5f, 1.5f);

        Visible = true;

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

        await ToSignal(GetTree().CreateTimer(0.15f), Timer.SignalName.Timeout);
        drawFrame.Visible = false;
        GetNode<Node2D>("正面卡牌").Visible = true;
        GetNode<TextureRect>("牌背").Visible = true;
        Fresh();
        DarkenLight();
        var cannonical = Scale;
        Scale = new Vector2();
        Tween tween3 = CreateTween();
        tween3.TweenProperty(
        this, "scale", cannonical, 0.15f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        tween3.TweenProperty(
        this, "scale", cannonical * 1.2f, 0.05f);
        tween3.TweenProperty(
        this, "scale", cannonical * 1f, 0.05f);
        await ToSignal(tween3, Tween.SignalName.Finished);
    }

    private bool _isDragging;
    private Vector2 _cardOriginPos;
    private Vector2 _mouseDownGlobal;
    private Tween _returnTween;
    private Tween _colorTween;
    private Tween _clickTween;
    private bool _clickAnimationPlayed;
    private const float ClickScaleDown = 1.2f;
    private const float ClickScaleTime = 0.08f;
    private const float DragThreshold = 10f;

    public override void _Ready()
    {
        // 直接在场景里摆放的卡牌实例没有经过 CreateCard（不会写入 _baseScale），
        // 这里用场景里设置的 scale 兜底；否则点击动画会 tween 到 _baseScale(0,0)，卡片被缩没。
        if (_baseScale == Vector2.Zero)
            _baseScale = Scale;

        var hitBtn = GetNode<Button>("%碰撞");
        hitBtn.GuiInput += OnHitButtonGuiInput;
        GetNode<Area2D>("碰撞箱").AreaEntered += AreaEntered;
        GetNode<Area2D>("碰撞箱").AreaExited += AreaExited;

        var infoBtn = GetNode<InputNineRect>("%信息按钮");
        BindControlClickAnimation(infoBtn, OnInfoButtonPressed);

        var opBtn = GetNode<InputNineRect>("%操作按钮");
        BindControlClickAnimation(opBtn);
    }
    private void BindControlClickAnimation(InputNineRect control, System.Action onClick = null)
    {
        if (control == null) return;
        SetupPivot(control);
        var originScale = control.Scale;
        bool pressing = false;

        control.Input += (@event) =>
        {
            if (!control.Visible) return;
            if (control.MouseFilter == Control.MouseFilterEnum.Ignore) return;

            if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    pressing = true;
                    var tween = control.CreateTween();
                    tween.TweenProperty(control, "scale", originScale * 0.9f, 0.06f)
                        .SetTrans(Tween.TransitionType.Quad)
                        .SetEase(Tween.EaseType.Out);
                }
                else
                {
                    if (pressing)
                    {
                        pressing = false;
                        var tween = control.CreateTween();
                        tween.TweenProperty(control, "scale", originScale, 0.08f)
                            .SetTrans(Tween.TransitionType.Back)
                            .SetEase(Tween.EaseType.Out);

                        onClick?.Invoke();
                    }
                }
            }
        };
    }

    private bool currentPlayable = true;
    private ColorBox _colorBox = new ColorBox(new("f4f4d5"), new("20ff07"), new("94ff42"), new("fc1e1e"));
    private ColorBox _colorBox2 = new ColorBox(new("bf3b3b"), default, default, default);
    private bool _isOpen = true;
    private void OnInfoButtonPressed()
    {
        if (CardViewMode == CardView.Battle) return;
        if (Model == null) return;

        CardDes.DisplayDescription(Model);
    }
    public override void _Input(InputEvent @event)
    {
        if (_openedCard == this && IsInstanceValid(this))
        {
            if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
            {
                var infoBg = GetNodeOrNull<Control>("%信息背景");
                if (infoBg != null && infoBg.Visible)
                {
                    if (!new Rect2(Vector2.Zero, infoBg.Size).HasPoint(infoBg.GetLocalMousePosition()))
                    {
                        CloseNonBattle();
                    }
                }
                else
                {
                    CloseNonBattle();
                }
            }
        }
    }

    private async void OnHitButtonGuiInput(InputEvent @event)
    {
        if (!_isOpen) return;
        if (CardViewMode == CardView.Battle && !isCallDragging) return;
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    KillReturnTween();
                    KillClickTween();

                    _mouseDownGlobal = GetGlobalMousePosition();
                    _cardOriginPos = _boundOriginPos ?? Position;
                    _isDragging = false;
                    _clickAnimationPlayed = false;

                    _clickTween = CreateTween();
                    _clickTween.TweenProperty(this, "scale", _baseScale * ClickScaleDown, ClickScaleTime)
                        .SetTrans(Tween.TransitionType.Quad)
                        .SetEase(Tween.EaseType.Out);
                }
                else
                {
                    if (_isDragging)
                    {
                        await EndDrag();
                    }
                    else
                    {
                        float dist = (_mouseDownGlobal - GetGlobalMousePosition()).Length();

                        if (dist < DragThreshold)
                        {
                            KillClickTween();
                            _clickTween = CreateTween();
                            _clickTween.TweenProperty(this, "scale", _baseScale, ClickScaleTime)
                                .SetTrans(Tween.TransitionType.Quad)
                                .SetEase(Tween.EaseType.Out);

                            OnCardClick();
                        }
                        else
                        {
                            KillClickTween();
                            _clickTween = CreateTween();
                            _clickTween.TweenProperty(this, "scale", _baseScale, ClickScaleTime)
                                .SetTrans(Tween.TransitionType.Quad)
                                .SetEase(Tween.EaseType.Out);
                        }
                    }

                    _isDragging = false;
                }
            }
        }
        else if (@event is InputEventMouseMotion)
        {
            if (CardViewMode != CardView.Battle) return;

            if (!Input.IsMouseButtonPressed(MouseButton.Left))
                return;

            if (!Model.CanPlay()) return;
            if (!isCallDragging) return;

            if (!_isDragging)
            {
                float dist = (_mouseDownGlobal - GetGlobalMousePosition()).Length();
                if (dist > DragThreshold)
                {
                    _isDragging = true;
                    ChoiceCard = this;
                    BringToFront();

                    _overlappingAreas.Clear();
                    ClearTarget();

                    KillClickTween();
                    Scale = _baseScale;

                    Drag();
                }
            }

            if (_isDragging)
            {
                Vector2 delta = GetGlobalMousePosition() - _mouseDownGlobal;
                Position = _cardOriginPos + delta;

                RefreshNearestTarget();
            }
        }
    }

    private async Task Close()
    {
        instances[this] = null;
        GetNode<GpuParticles2D>("%粒子特效").Emitting = false;
        GetNode<Node2D>("正面卡牌").Visible = false;
        GetNode<TextureRect>("牌背").Visible = false;
        GetNode<Button>("%碰撞").MouseFilter = Control.MouseFilterEnum.Ignore;
        KillReturnTween();
        KillClickTween();
        _overlappingAreas.Clear();
        ClearTarget();
        _isOpen = false;
        if (ChoiceCard == this)
            await EndDrag();
    }

    private void Drag()
    {
        TargetRegistry.CallTargeted(Model, Model.TargetType, false,Model.Camp);
    }
    private void SetNode2DVisible(Node2D node, bool visible)
    {
        if (node == null) return;
        node.Visible = visible;
    }

    private void SetControlVisible(Control control, bool visible)
    {
        if (control == null) return;

        control.Visible = visible;
        control.MouseFilter = visible
            ? Control.MouseFilterEnum.Stop
            : Control.MouseFilterEnum.Ignore;
    }

    private void OnCardClick()
    {
        // 有些界面（比如卡包详情）只想要“点卡看说明”，不想要展开信息背景：
        // 展开会改动 ZIndex、盖住后面的节点，在网格布局里很容易出现点不到、卡消失的问题。
        // 关掉 AllowExpand 后直接弹说明面板，不动卡牌本身的布局和层级。
        if (!AllowExpand)
        {
            CardDes.DisplayDescription(Model);
            return;
        }

        ChoiceCard = this;
        ResetAllZIndex();
        BringToFront();

        if (CardViewMode == CardView.Battle)
        {
            CardDes.DisplayDescription(Model);
            return;
        }

        if (_openedCard != null && _openedCard != this && IsInstanceValid(_openedCard))
            _openedCard.CloseNonBattle();

        ToggleNonBattle();
    }
    /// <summary>
    /// 关闭信息背景
    /// </summary>
    public void CloseNonBattle()
    {
        var nonBattle = GetNode<Node2D>("%非战斗");
        if (!nonBattle.Visible) return;

        _ = ToggleNonBattleAsync(false);
    }
    private Tween _nonBattleTween;
    private Tween _nonBattleScaleTween;

    private void ToggleNonBattle()
    {
        var nonBattle = GetNode<Node2D>("%非战斗");
        _ = ToggleNonBattleAsync(!nonBattle.Visible);
    }

    private async Task ToggleNonBattleAsync(bool willOpen)
    {
        var nonBattle = GetNode<Node2D>("%非战斗");
        if (willOpen == nonBattle.Visible) return;

        var infoBg = GetNode<Control>("%信息背景");
        var infoBtn = GetNode<NinePatchRect>("%信息按钮");
        var opBtn = GetNode<NinePatchRect>("%操作按钮");

        if (_nonBattleTween != null && _nonBattleTween.IsValid())
            _nonBattleTween.Kill();
        if (_nonBattleScaleTween != null && _nonBattleScaleTween.IsValid())
            _nonBattleScaleTween.Kill();

        bool isCollection = CardViewMode == CardView.Collection;
        bool isDeck = CardViewMode == CardView.Deck;

        if (willOpen)
        {
            _openedCard = this;
            nonBattle.Visible = true;

            if (isCollection)
            {
                SetControlVisible(infoBtn, true);
            }
            else if (isDeck)
            {
                SetControlVisible(opBtn, true);
                SetControlVisible(infoBtn, true);
            }

            infoBg.PivotOffset = infoBg.Size / 2f;
            infoBg.Scale = Vector2.Zero;
            _nonBattleScaleTween = infoBg.CreateTween();
            _nonBattleScaleTween.TweenProperty(infoBg, "scale", Vector2.One * 1.05f, 0.2f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
            await ToSignal(_nonBattleScaleTween, Tween.SignalName.Finished);

            _nonBattleTween = infoBg.CreateTween();
            _nonBattleTween.TweenProperty(infoBg, "size:y", 368f, 0.25f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            await ToSignal(_nonBattleTween, Tween.SignalName.Finished);

            if (isDeck)
            {
                _nonBattleTween = infoBg.CreateTween();
                _nonBattleTween.TweenProperty(infoBg, "size:y", 528f, 0.25f)
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.Out);
                await ToSignal(_nonBattleTween, Tween.SignalName.Finished);
            }
        }
        else
        {
            if (_openedCard == this) _openedCard = null;

            if (isCollection)
            {

                _nonBattleTween = infoBg.CreateTween();
                _nonBattleTween.TweenProperty(infoBg, "size:y", 205f, 0.25f)
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.Out);
                await ToSignal(_nonBattleTween, Tween.SignalName.Finished);

                infoBg.PivotOffset = infoBg.Size / 2f;
                _nonBattleScaleTween = infoBg.CreateTween();
                _nonBattleScaleTween.TweenProperty(infoBg, "scale", Vector2.Zero, 0.2f)
                    .SetTrans(Tween.TransitionType.Back)
                    .SetEase(Tween.EaseType.In);
                await ToSignal(_nonBattleScaleTween, Tween.SignalName.Finished);

                SetControlVisible(infoBtn, false);
                nonBattle.Visible = false;
            }
            else if (isDeck)
            {

                _nonBattleTween = infoBg.CreateTween();
                _nonBattleTween.TweenProperty(infoBg, "size:y", 368f, 0.25f)
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.Out);
                await ToSignal(_nonBattleTween, Tween.SignalName.Finished);

                _nonBattleTween = infoBg.CreateTween();
                _nonBattleTween.TweenProperty(infoBg, "size:y", 205f, 0.25f)
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.Out);
                await ToSignal(_nonBattleTween, Tween.SignalName.Finished);

                infoBg.PivotOffset = infoBg.Size / 2f;
                _nonBattleScaleTween = infoBg.CreateTween();
                _nonBattleScaleTween.TweenProperty(infoBg, "scale", Vector2.Zero, 0.2f)
                    .SetTrans(Tween.TransitionType.Back)
                    .SetEase(Tween.EaseType.In);
                await ToSignal(_nonBattleScaleTween, Tween.SignalName.Finished);

                SetControlVisible(infoBtn, false);
                SetControlVisible(opBtn, false);
                nonBattle.Visible = false;
            }
            else
            {
                SetControlVisible(infoBtn, false);
                SetControlVisible(opBtn, false);
                nonBattle.Visible = false;
            }
        }
    }

    private async Task EndDrag()
    {
        _overlappingAreas.Clear();

        if (CardViewMode != CardView.Battle)
        {
            _isDragging = false;
            ClearTarget();
            ReturnToOrigin();
            return;
        }

        var savedTarget = target;
        if (ChoiceCard == this) ChoiceCard = null;
        TargetRegistry.DeleteTargeted(Model.TargetType,Model.Camp);
        ClearTarget(true);
        if (savedTarget == null)
        {
            ReturnToOrigin();
        }
        else
        {
            var playedModel = Model;
            if (playedModel != null)
                PlayerManager.GetPlayer(playedModel.Camp)?.Status?.HandRemoveCard(playedModel);
            Model = null;
            Visible = false;
            PlayerBox.StuffCard();
            await DiscallDragging();
            if (GodotObject.IsInstanceValid(this) && HasNode("碰撞"))
                GetNode<Button>("%碰撞").MouseFilter = Control.MouseFilterEnum.Ignore;
            try
            {
                if (playedModel != null)
                {
                    await playedModel.DestroyMe();
                    await CardCmd.PlayedCard(playedModel, savedTarget);
                }
            }
            finally
            {
                instances.Remove(this);
                _isOpen = false;
                if (GodotObject.IsInstanceValid(this))
                    QueueFree();
            }
        }
    }

    private void ReturnToOrigin(float duration = 0.9f)
    {
        KillReturnTween();
        _returnTween = CreateTween();
        _returnTween.SetEase(Tween.EaseType.Out);
        _returnTween.SetTrans(Tween.TransitionType.Quad);
        _returnTween.TweenProperty(this, "position", _cardOriginPos, duration);
    }
    private async Task ReturnToOriginTask(float duration = 0.9f)
    {
        KillReturnTween();
        _returnTween = CreateTween();
        _returnTween.SetEase(Tween.EaseType.Out);
        _returnTween.SetTrans(Tween.TransitionType.Quad);
        _returnTween.TweenProperty(this, "position", _cardOriginPos, duration);
        await ToSignal(_returnTween, Tween.SignalName.Finished);
    }
    private ITarget target;
    private Area2D _currentTargetArea;

    private readonly System.Collections.Generic.HashSet<Area2D> _overlappingAreas = new();

    private void AreaEntered(Area2D area)
    {
        if (CardViewMode != CardView.Battle) return;
        if (Model == null)
        {
            return;
        }
        if (!Model.CanPlay()) return;

        _overlappingAreas.Add(area);
        RefreshNearestTarget();
    }

    private void AreaExited(Area2D area)
    {
        if (CardViewMode != CardView.Battle) return;

        _overlappingAreas.Remove(area);

        if (target != null && area.HasMeta("Target") &&
            (ITarget)(GodotObject)area.GetMeta("Target") == target)
        {
            ClearTarget();
            RefreshNearestTarget();
        }
    }

    private void RefreshNearestTarget()
    {
        if (CardViewMode != CardView.Battle) return;
        if (!Model.CanPlay()) return;
        if (!_isDragging) return;

        var myPos = GetGlobalMousePosition();

        Area2D bestArea = null;
        ITarget bestTarget = null;
        float bestDist = float.MaxValue;

        foreach (var area in _overlappingAreas)
        {
            if (!IsInstanceValid(area)) continue;
            if (!area.HasMeta("Target")) continue;

            var col = (ITarget)(GodotObject)area.GetMeta("Target");
            if (col == null) continue;
            if (!col.Calling) continue;
            if (!col.CanBeTarget(Model, Model.TargetFilter, false)) continue;

            Vector2 areaPos = area is Node2D n2d
                ? n2d.GlobalPosition
                : (area.GetParent() as Node2D)?.GlobalPosition ?? Vector2.Zero;

            float dist = myPos.DistanceSquaredTo(areaPos);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestArea = area;
                bestTarget = col;
            }
        }

        if (bestTarget == target) return;

        ClearTarget();

        if (bestTarget != null && bestArea != null)
        {
            target = bestTarget;
            _currentTargetArea = bestArea;

            var kind = (string)bestArea.GetMeta("TargetType");
            FighterCardModel fighter = null;
            if (Model is FighterCardModel fighterCard)
                fighter = fighterCard;
            var ctx = kind switch
            {
                "Road" => new TargetContext { Line = true, FighterCard = fighter ,Camp = Model.Camp },
                "Grid" => new TargetContext { Grid = true, FighterCard = fighter, Camp = Model.Camp },
                "CoopGrid" => new TargetContext { CoopGrid = true, FighterCard = fighter, Camp = Model.Camp },
                _ => default
            };

            bestTarget.OnTargeted(ctx);
            Glowing();
        }
    }

    private void ClearTarget(bool isPlayed = false)
    {
        if (target == null) return;

        FighterCardModel fighter = null;
        if (Model is FighterCardModel fighterCard)
            fighter = fighterCard;
        if (_currentTargetArea != null && IsInstanceValid(_currentTargetArea))
        {
            var kind = (string)_currentTargetArea.GetMeta("TargetType");
            var ctx = kind switch
            {
                "Road" => new TargetContext { Line = true, FighterCard = fighter, Camp = Model.Camp },
                "Grid" => new TargetContext { Grid = true, FighterCard = fighter, Camp = Model.Camp },
                "CoopGrid" => new TargetContext { CoopGrid = true, FighterCard = fighter, Camp = Model.Camp },
                _ => default
            };
            if (!isPlayed)
                target.OnDistargeted(ctx);
        }

        target = null;
        _currentTargetArea = null;
        Darken();
    }

    private void Glowing()
    {
        var glowNode = GetNode<TextureRect>("%卡牌发光背景");
        if (_colorTween != null && _colorTween.IsValid())
            _colorTween.Kill();
        _colorTween = glowNode.CreateTween();
        _colorTween.TweenProperty(glowNode, "modulate", new Color(_colorBox.ColorA, 1f), 0.5f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        GetNode<GpuParticles2D>("%粒子特效").Emitting = true;

        _callTween = CreateTween();
        _callTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.1f);
    }

    private void Darken()
    {
        var glowNode = GetNode<TextureRect>("%卡牌发光背景");
        if (_colorTween != null && _colorTween.IsValid())
            _colorTween.Kill();
        _colorTween = glowNode.CreateTween();
        _colorTween.TweenProperty(glowNode, "modulate", new Color(_colorBox.Default, 1f), 0.5f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        GetNode<GpuParticles2D>("%粒子特效").Emitting = false;
        target = null;

        _callTween = CreateTween();
        _callTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.1f);
    }

    private void DarkenLight()
    {
        var glowNode = GetNode<TextureRect>("%卡牌发光背景");
        KillCallTween();
        if (_colorTween != null && _colorTween.IsValid())
            _colorTween.Kill();
        _colorTween = glowNode.CreateTween();
        _colorTween.TweenProperty(glowNode, "modulate", new Color(_colorBox.Default, 0f), 0f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        _callTween = CreateTween();
        _callTween.TweenProperty(this, "modulate", new Color(1f, 1f, 1f, 1), 0.1f);
        GetNode<GpuParticles2D>("%粒子特效").Emitting = false;
        target = null;
    }

    private void Darkest()
    {
        var glowNode = GetNode<TextureRect>("%卡牌发光背景");
        KillCallTween();
        if (_colorTween != null && _colorTween.IsValid())
            _colorTween.Kill();
        _colorTween = glowNode.CreateTween();
        _colorTween.TweenProperty(glowNode, "modulate", new Color(_colorBox.Default, 0f), 0f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        _callTween = CreateTween();
        _callTween.TweenProperty(this, "modulate", new Color(0.6f, 0.6f, 0.6f, 1), 0.1f);
        GetNode<GpuParticles2D>("%粒子特效").Emitting = false;
        target = null;
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

    private Tween _callTween;
    private void KillCallTween()
    {
        if (_callTween != null && _callTween.IsValid())
        {
            _callTween.Kill();
        }
    }

    private bool isCallDragging;
    private async void _callDragging() => await CallDragging();

    private async Task CallDragging()
    {
        KillCallTween();
        isCallDragging = true;
        _callTween = CreateTween();
        _callTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.1f);
        _callTween.TweenProperty(GetNode<TextureRect>("%卡牌发光背景"), "modulate", new Color(1, 1, 1, 1), 0.1f);
        await ToSignal(_callTween, Tween.SignalName.Finished);
    }

    private async Task DiscallDragging()
    {
        KillCallTween();
        isCallDragging = false;
        _callTween = CreateTween();
        _callTween.TweenProperty(this, "modulate", new Color(0.6f, 0.6f, 0.6f, 1), 0.1f);
        _callTween.TweenProperty(GetNode<TextureRect>("%卡牌发光背景"), "modulate", new Color(1, 1, 1, 0f), 0.1f);
        await ToSignal(_callTween, Tween.SignalName.Finished);
    }

    private void SetupPivot(Control control)
    {
        CallDeferred(nameof(ApplyPivot), control);
        control.Resized += () => ApplyPivot(control);
    }

    private void ApplyPivot(Control control)
    {
        if (control == null || !IsInstanceValid(control)) return;
        control.PivotOffset = control.Size / 2f;
    }

    /// <summary>
    /// 每次修改卡牌时调用 用于刷新卡牌属性
    /// </summary>
    public void Fresh(bool anim = false)
    {
        try
        {
            if (Model != null)
            {

                GetNode<TextureRect>("%卡框").Visible = true;
                GetNode<TextureRect>("%卡牌发光背景").Visible = true;
                GetNode<TextureRect>("%卡牌底板").Visible = true;
                GetNode<Node2D>("%费用容器").Visible = true;
                GetNode<Node2D>("%攻击力容器").Visible = true;
                GetNode<Node2D>("%生命值容器").Visible = true;
                GetNode<Node2D>("%等级").Visible = true;
                GetNode<Node2D>("正面卡牌").Visible = true;
                GetNode<TextureRect>("牌背").Visible = false;

                if (CardViewMode == CardView.Battle)
                {
                    GetNode<Node2D>("%非战斗").Visible = false;
                }
                else
                {
                    var opBtn = GetNode<NinePatchRect>("%操作按钮");
                    SetControlVisible(opBtn, false);

                    var infoBtn = GetNode<NinePatchRect>("%信息按钮");
                    SetControlVisible(infoBtn, false);

                    var nonBattle = GetNode<Node2D>("%非战斗");
                    nonBattle.Visible = false;

                    var infoBg = GetNode<Control>("%信息背景");
                    infoBg.Size = new Vector2(infoBg.Size.X, 205f);
                    infoBg.Scale = Vector2.Zero;
                    infoBg.PivotOffset = infoBg.Size / 2f;
                }
                _isOpen = true;
                if (Model.Status == Status.FaceUp)
                {
                    {
                        var frame = GetNode<TextureRect>("%卡框");
                        var frameGlowBg = GetNode<TextureRect>("%卡牌发光背景");
                        var frameBg = GetNode<TextureRect>("%卡牌底板");
                        _callDragging();
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
                            CardType.None | CardType.Environment => new(0.55f, 0.55f),
                            _ => new(1f, 1f)
                        };
                        frame.Position = Model.CardType switch
                        {
                            CardType.None | CardType.Environment => new(128.0f, 92f),
                            _ => new(0, -32.0f)
                        };
                        frameBg.Modulate = Model.Class.ToColor();
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
                    costs.GetNode<TextureRect>("费用").Position = Model.Camp switch
                    {
                        Camp.Plant => new(180.0f, -18.5f),
                        Camp.Zombie => new(180.0f, -9.5f),
                        _ => new(180.0f, -18.5f)
                    };
                    GetNode<Node2D>("正面卡牌").Visible = true;
                    GetNode<TextureRect>("牌背").Visible = false;
                    Control control = null;
                    if (GetNode<Node2D>("%卡面容器").GetChildren().Count == 0)
                    {
                        control = Model.Icon.Instantiate<Control>();
                        control.MouseFilter = MouseFilterEnum.Ignore;
                        GetNode<Node2D>("%卡面容器").AddChild(control);
                        foreach (var cot in control.GetChildren().OfType<Control>())
                        {
                            cot.MouseFilter = MouseFilterEnum.Ignore;
                        }
                    }
                    else
                    {
                        GetNode<Node2D>("%卡面容器").GetChildren().OfType<Control>().First().QueueFree();
                        control = Model.Icon.Instantiate<Control>();
                        control.MouseFilter = MouseFilterEnum.Ignore;
                        GetNode<Node2D>("%卡面容器").AddChild(control);
                        foreach (var cot in control.GetChildren().OfType<Control>())
                        {
                            cot.MouseFilter = MouseFilterEnum.Ignore;
                        }
                    }
                    if (Model.CardType.HasFlag(CardType.Fighter))
                    {
                        control.ZIndex = 1;
                        GetNode<TextureRect>("%卡框").ZIndex = 0;
                    }
                    else
                    {
                        control.ZIndex = 0;
                        GetNode<TextureRect>("%卡框").ZIndex = 1;
                    }
                    GetNode<GpuParticles2D>("%粒子特效").Emitting = false;
                    if (Model.Cost.HasChanged)
                    {
                        if (Model.Cost.PositiveChanged)
                            costs.GetNode<RichTextLabel>("%数值").Modulate = _colorBox.ColorB;
                        else
                            costs.GetNode<RichTextLabel>("%数值").Modulate = _colorBox2.Default;
                    }
                    if (Model is FighterCardModel fighter)
                    {
                        var atks = GetNode<Node2D>("%攻击力容器");
                        atks.Visible = fighter.Atk.Current > 0 && fighter.BornWithAtk;
                        var hps = GetNode<Node2D>("%生命值容器");
                        hps.Visible = true;
                        GetNode<Node2D>("%等级").Visible = true;
                        atks.GetNode<RichTextLabel>("%伤害数值").Text = $"[center]{fighter.Atk.Current}[/center]";
                        hps.GetNode<RichTextLabel>("%血量数值").Text = $"[center]{fighter.Hp.Current}[/center]";
                        GetNode<TextureRect>("%伤害动画").Texture = fighter.AtkType.Current.Icon;
                        GetNode<TextureRect>("%血量动画").Texture = fighter.HpType.Current.Icon;
                        if (fighter.StarType != null)
                        {
                            GetNode<SpineHandler>("%等级").LoadSkeletonData(fighter.StarType.IconSkelPath);
                            GetNode<SpineHandler>("%等级").Scale = new(0.28f, 0.28f);
                            GetNode<SpineHandler>("%等级").SetAnimation(0, $"intro", false);
                        }

                        if (fighter.MaxHp.HasChanged || fighter.Hp.Current != fighter.MaxHp.Current)
                        {
                            if (fighter.MaxHp.PositiveChanged && fighter.MaxHp.Current == fighter.Hp.Current)
                                GetNode<RichTextLabel>("%血量数值").SelfModulate = _colorBox.ColorB;
                            else
                            {
                                if (fighter.MaxHp.Current == fighter.Hp.Current)
                                    GetNode<RichTextLabel>("%血量数值").SelfModulate = _colorBox2.Default;
                                else
                                    GetNode<RichTextLabel>("%血量数值").SelfModulate = _colorBox.ColorC;
                            }
                        }
                        else
                            GetNode<RichTextLabel>("%血量数值").SelfModulate = _colorBox.Default;

                        if (fighter.Atk.HasChanged)
                        {
                            if (fighter.Atk.PositiveChanged)
                                GetNode<RichTextLabel>("%伤害数值").SelfModulate = _colorBox.ColorB;
                            else
                                GetNode<RichTextLabel>("%伤害数值").SelfModulate = _colorBox.ColorC;
                        }
                        else
                            GetNode<RichTextLabel>("%伤害数值").SelfModulate = _colorBox.Default;
                    }
                    else
                    {
                        var atks = GetNode<Node2D>("%攻击力容器");
                        atks.Visible = false;
                        var hps = GetNode<Node2D>("%生命值容器");
                        hps.Visible = false;
                        GetNode<Node2D>("%等级").Visible = false;
                    }
                    JustFresh();
                }
                else if (Model.Status == Status.FaceDown)
                {
                    GetNode<Node2D>("正面卡牌").Visible = false;
                    GetNode<TextureRect>("牌背").Visible = true;

                    GetNode<TextureRect>("%卡框").Visible = false;
                    GetNode<TextureRect>("%卡牌发光背景").Visible = false;
                    GetNode<TextureRect>("%卡牌底板").Visible = false;
                    GetNode<Node2D>("%费用容器").Visible = false;
                    GetNode<Node2D>("%攻击力容器").Visible = false;
                    GetNode<Node2D>("%生命值容器").Visible = false;
                    GetNode<Node2D>("%等级").Visible = false;
                    GetNode<GpuParticles2D>("%粒子特效").Emitting = false;

                    DarkenLight();

                    GetNode<TextureRect>("牌背").Texture = Model.CardType switch
                    {
                        _ => Model.Camp switch
                        {
                            Camp.Plant => GD.Load<Texture2D>("res://素材/牌背、卡面/cardback_plants.png"),
                            Camp.Zombie => GD.Load<Texture2D>("res://素材/牌背、卡面/cardback_zombies.png"),
                            _ => null
                        }
                    };
                }
                Visible = !anim;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message + ex.StackTrace);
        }
    }
    /// <summary>
    /// 切换明暗状态
    /// </summary>
    public void JustFresh()
    {
        if (Model == null) return;
        if (Model.Status == Status.FaceDown)
        {
            DarkenLight();
            return;
        }
        if (CardViewMode == CardView.Battle)
        {
            FreshAmount();
            if (Model.CanPlay())
                Darken();
            else
                Darkest();
        }
        else
        {
            int amount = Mathf.Min(
                PlayerManager.HostPlayer?.Asset?.GetAmount(Model.Title) ?? 0,
                4);
            if (amount == 0)
                Darkest();
            else
                DarkenLight();
            FreshAmount();
        }
    }
    public override void _ExitTree()
    {
        KillReturnTween();
        KillClickTween();
        KillCallTween();

        if (_nonBattleTween != null && _nonBattleTween.IsValid())
            _nonBattleTween.Kill();
        if (_nonBattleScaleTween != null && _nonBattleScaleTween.IsValid())
            _nonBattleScaleTween.Kill();
        if (_colorTween != null && _colorTween.IsValid())
            _colorTween.Kill();
    }
    /// <summary>
    /// 翻转卡牌
    /// </summary>
    public void Filp()
    {
        if (Model.Status == Status.FaceDown)
        {
            Model.Status = Status.FaceUp;
            Fresh();
        }
        else if (Model.Status == Status.FaceUp)
        {
            Model.Status = Status.FaceDown;
            Fresh();
        }
    }
    /// <summary>
    /// 刷新卡牌数量显示。
    /// </summary>
    private void FreshAmount()
    {
        var amountRoot = GetNodeOrNull<CanvasItem>("%卡牌数容器");
        var amountLabel = GetNodeOrNull<Label>("%数量");

        if (CardViewMode == CardView.Battle || Model == null)
        {
            if (amountRoot != null) amountRoot.Visible = false;
            SetGray(false);
            return;
        }

        int amount;
        if (ForceShowAmount)
        {
            amount = Mathf.Max(ForcedAmount, 1);
        }
        else
        {
            int raw = PlayerManager.HostPlayer?.Asset?.GetAmount(Model.Title) ?? 0;
            amount = Mathf.Min(raw, 4);
        }

        if (amountLabel != null)
            amountLabel.Text = $"x{amount}";
        if (amountRoot != null)
            amountRoot.Visible = amount > 0;

        SetGray(amount == 0 || ForceShowAmount);
    }
    private static readonly string[] GrayNodePaths =
    [
    "%卡牌底板",
    "%血量数值",
    "%伤害数值",
    "%血量动画",
    "%伤害动画",
    "%费用",
    "%数值",
    ];

    private const float FrameGraySaturation = 0.75f;
    private const float NormalGraySaturation = 0.05f;

    private static Shader _desaturateShader;
    private static Shader DesaturateShader
    {
        get
        {
            if (_desaturateShader != null) return _desaturateShader;
            _desaturateShader = GD.Load<Shader>("res://着色器/desaturate.gdshader");
            if (_desaturateShader == null)
                GD.PushError("desaturate.gdshader 加载失败");
            return _desaturateShader;
        }
    }

    private readonly Godot.Collections.Dictionary<CanvasItem, ShaderMaterial> _grayMaterials = new();

    private void EnsureGrayMaterials()
    {
        EnsureGrayMaterialFor("%卡框", FrameGraySaturation);

        foreach (var path in GrayNodePaths)
            EnsureGrayMaterialFor(path, NormalGraySaturation);
    }

    private void EnsureGrayMaterialFor(string path, float defaultSaturation)
    {
        var node = GetNodeOrNull<CanvasItem>(path);
        if (node == null) return;

        if (_grayMaterials.TryGetValue(node, out var exist) && IsInstanceValid(exist))
            return;

        var sm = new ShaderMaterial { Shader = DesaturateShader };
        sm.SetShaderParameter("saturation", defaultSaturation);
        node.Material = sm;
        _grayMaterials[node] = sm;
    }

    private void SetGray(bool on)
    {
        if (on)
            EnsureGrayMaterials();

        SetMaterial("%卡框", on, FrameGraySaturation);

        foreach (var path in GrayNodePaths)
            SetMaterial(path, on, NormalGraySaturation);

        SetCardFaceGray(on);
    }

    private void SetMaterial(string path, bool on, float graySaturation)
    {
        var node = GetNodeOrNull<CanvasItem>(path);
        if (node == null) return;

        if (!on)
        {
            node.Material = null;
            return;
        }

        if (_grayMaterials.TryGetValue(node, out var sm) && IsInstanceValid(sm))
        {
            sm.SetShaderParameter("saturation", graySaturation);
            return;
        }

        sm = new ShaderMaterial { Shader = DesaturateShader };
        sm.SetShaderParameter("saturation", graySaturation);
        node.Material = sm;
        _grayMaterials[node] = sm;
    }

    private void SetCardFaceGray(bool on)
    {
        var container = GetNodeOrNull<Node>("%卡面容器");
        if (container == null) return;

        var tex = FindDeepestTextureRect(container);
        if (tex == null) return;

        if (!on)
        {
            tex.Material = null;
            return;
        }

        if (_grayMaterials.TryGetValue(tex, out var sm) && IsInstanceValid(sm))
        {
            sm.SetShaderParameter("saturation", NormalGraySaturation);
            return;
        }

        sm = new ShaderMaterial { Shader = DesaturateShader };
        sm.SetShaderParameter("saturation", NormalGraySaturation);
        tex.Material = sm;
        _grayMaterials[tex] = sm;
    }

    private static TextureRect FindDeepestTextureRect(Node node)
    {
        TextureRect result = null;
        foreach (var child in node.GetChildren())
        {
            if (child is TextureRect tr)
                result = tr;

            var deeper = FindDeepestTextureRect(child);
            if (deeper != null)
                result = deeper;
        }
        return result;
    }
    //专门用于远程同步或者Ai的区域
    /// <summary>
    /// 对一个对象打出该卡牌
    /// </summary>
    /// <param name="target">打出的对象</param>
    /// <returns></returns>
    public async Task PlayMe(PlayContext ctx)
    {
        CardCmd.PushStop();
        try
        {
            if (ctx.Target == null) return;

            if (Model is FighterCardModel fighterCard && fighterCard.CardTag.HasFlag(CardTag.GraveStone))
            {
                Model.Status = Status.FaceDown;
            }
            else
                Model.Status = Status.FaceUp;

            Fresh();
            DarkenLight();

            Vector2 CardCenterOffset() => new Vector2(256f, 193f) * Scale + new Vector2(0, 60);

            Vector2 ToCardPos(Vector2 targetGlobal)
            {
                var parent2D = GetParent() as Node2D;
                Vector2 local = parent2D != null ? parent2D.ToLocal(targetGlobal) : targetGlobal;
                return local - CardCenterOffset();
            }
            var fighterm = Model as FighterCardModel;
            var ctx2 = ctx.RoadKind switch
            {
                RoadTargetKind.Line => new TargetContext { Line = true, FighterCard = fighterm },
                RoadTargetKind.Grid => new TargetContext { Grid = true, FighterCard = fighterm },
                RoadTargetKind.CoopGrid => new TargetContext { CoopGrid = true, FighterCard = fighterm },
                _ => new TargetContext { FighterCard = fighterm }
            };

            var cannoncial = Scale;
            Tween tween = CreateTween();
            tween.TweenProperty(this, "scale", Scale * 1f, 0.5f);
            tween.TweenProperty(this, "scale", Scale * 1.1f, 0.1f);
            tween.TweenProperty(this, "scale", Scale * 1f, 0.1f);
            tween.TweenProperty(this, "scale", Scale * 1f, 0.3f);
            await BindOriginTask(ToCardPos(new(360f, 240f)), 0.6f);
            ctx.Target.AutoTargeted(ctx2);
            await ToSignal(GetTree().CreateTimer(0.5f), Timer.SignalName.Timeout);
            if (ctx.Target is Fighter fighter)
                await BindOriginTask(ToCardPos(fighter.GlobalPosition));
            else if (ctx.Target is HeroInstance instance)
                await BindOriginTask(ToCardPos(instance.GlobalPosition));
            else if (ctx.Target is NodeRoad road)
            {
                road.CallTargetKind(ctx.RoadKind);

                switch (road.LastTargetKind)
                {
                    case RoadTargetKind.Line:
                        await BindOriginTask(ToCardPos(road.GlobalPosition));
                        break;

                    case RoadTargetKind.Grid:
                        if (Model is FighterCardModel && Model.Camp.Plant())
                            road.SwapForGrid();
                        await BindOriginTask(ToCardPos(road.GetFighterLocation(Model.Camp.Switch(Location.Plant, Location.Zombie))));
                        break;

                    case RoadTargetKind.CoopGrid:
                        if (Model.Camp.Plant())
                        {
                            if (Model is FighterCardModel)
                                road.SwapForCoopGrid();
                            await BindOriginTask(ToCardPos(road.GetFighterLocation(Location.Plant)));
                        }
                        break;

                    default:
                        if (ctx.TargetType.HasFlag(TargetType.CoopGrids) && Model.Camp.Plant())
                        {
                            if (Model is FighterCardModel)
                                road.SwapForCoopGrid();
                            await BindOriginTask(ToCardPos(road.GetFighterLocation(Location.Plant)));
                        }
                        else if (ctx.TargetType.HasFlag(TargetType.Grids))
                        {
                            if (Model is FighterCardModel && Model.Camp.Plant())
                                road.SwapForGrid();
                            await BindOriginTask(ToCardPos(road.GetFighterLocation(Model.Camp.Switch(Location.Plant, Location.Zombie))));
                        }
                        else
                        {
                            await BindOriginTask(ToCardPos(road.GlobalPosition));
                        }
                        break;
                }
            }

            PlayerManager.GetPlayer(Model.Camp).Status.HandRemoveCard(Model);
            PlayerBox.StuffCard();
            await DiscallDragging();
            Visible = false;
            GetNode<Button>("%碰撞").MouseFilter = Control.MouseFilterEnum.Ignore;
            ctx.Target.AutoDistargeted(ctx2);
            await CardCmd.PlayedCard(Model, ctx.Target);
            await Model.DestroyMe();
        }
        finally
        {
            CardCmd.PopStop();
        }
    }
}