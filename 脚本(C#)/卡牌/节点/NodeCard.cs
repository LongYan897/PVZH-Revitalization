using Battle;
using Battle.Entity;
using Card.Cmd;
using Controller;
using Godot;
using Pack;
using Spine;
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
    private CardView CardViewMode;
    private static readonly PackedScene Scene = GD.Load<PackedScene>("res://场景(C#)/卡牌.tscn");
    public static NodeCard ChoiceCard { get; private set; }
    public CardModel Model { get; private set; }
    private static readonly Dictionary<NodeCard, CardModel> instances = new();
    private const int maxInstance = int.MaxValue;

    /// <summary>
    /// 卡牌初始大小
    /// </summary>
    private const float BaseScaleFactor = 0.5f;
    private Vector2 _baseScale;

    /// <summary>
    /// 从一个卡牌中创建一个卡牌节点并自动挂载到父节点
    /// </summary>
    /// <param name="cardModel">卡牌</param>
    /// <param name="position">初始位置</param>
    /// <param name="parent">父节点，为 null 时使用 Main.CardContainer</param>
    /// <param name="drawAnimation">是否播放抽卡动画</param>
    /// <param name="scaleFactor">缩放系数，默认 BaseScaleFactor</param>
    public static NodeCard CreateCard(
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
            if (drawAnimation)
                node.DrawAnimation();
            else
                node.Fresh();
            instances.Add(node, cardModel);
            parent.AddChild(node);
            node.ZIndex = instances.Count * 30;
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
            if (drawAnimation)
                node.DrawAnimation();
            else
                node.Fresh();
            instances[node] = cardModel;
            return node;
        }
    }
    /// <summary>
    /// 从一个卡牌中创建一个卡牌节点并自动挂载到父节点
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="cardModel">卡牌</param>
    public static NodeCard DisplayCardAt(CardModel cardModel, Vector2 position, Node parent, CardView cardView = CardView.Battle, float scaleFactor = BaseScaleFactor)
        => CreateCard(cardModel, position, cardView, parent, drawAnimation: false, scaleFactor: scaleFactor);

    /// <summary>
    /// 从一个卡牌中创建一个卡牌节点并自动挂载到父节点(播放抽卡动画)
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="cardModel">卡牌</param>
    public static NodeCard DrawACardAt(CardModel cardModel, Vector2 position, Node parent, CardView cardView = CardView.Battle, float scaleFactor = BaseScaleFactor)
        => CreateCard(cardModel, position, cardView, parent, drawAnimation: true, scaleFactor: scaleFactor);

    /// <summary>
    /// 从一个卡牌中创建一个卡牌节点并自动挂载到父节点
    /// </summary>
    /// <param name="cardModel">卡牌</param>
    public static NodeCard DisplayCard(CardModel cardModel, Vector2 position, CardView cardView = CardView.Battle, float scaleFactor = BaseScaleFactor)
        => CreateCard(cardModel, position, cardView, null, drawAnimation: false, scaleFactor: scaleFactor);

    /// <summary>
    /// 从一个卡牌中创建一个卡牌节点并自动挂载到父节点(播放抽卡动画)
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="cardModel">卡牌</param>
    public static NodeCard DrawACard(CardModel cardModel, Vector2 position, CardView cardView = CardView.Battle, float scaleFactor = BaseScaleFactor)
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
    /// 获得当前卡牌对应的节点
    /// </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    public static NodeCard GetNode(CardModel card)
    {
        return instances.FirstOrDefault(k => k.Value == card).Key;
    }

    private async void DrawAnimation()
    {
        var drawFrame = GetNode<Sprite2D>("%特效");
        drawFrame.Visible = true;
        GetNode<Node2D>("正面卡牌").Visible = false;
        GetNode<TextureRect>("牌背").Visible = false;
        drawFrame.Modulate = new Color(0, 0, 0, 0);
        drawFrame.Rotation = 0;
        drawFrame.Scale = new Vector2(2.8f, 2.8f);

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
    private const float ClickScaleDown = 1.1f;
    private const float ClickScaleTime = 0.08f;
    private const float DragThreshold = 10f;

    public override void _Ready()
    {
        var hitBtn = GetNode<Button>("碰撞");
        hitBtn.GuiInput += OnHitButtonGuiInput;
        GetNode<Area2D>("碰撞箱").AreaEntered += AreaEntered;
        GetNode<Area2D>("碰撞箱").AreaExited += AreaExited;

        var infoBtn = GetNode<NinePatchRect>("%信息按钮");
        BindControlClickAnimation(infoBtn, OnInfoButtonPressed);

        var opBtn = GetNode<NinePatchRect>("%操作按钮");
        BindControlClickAnimation(opBtn);
    }
    private void BindControlClickAnimation(Control control, System.Action onClick = null)
    {
        if (control == null) return;
        SetupPivot(control);
        var originScale = control.Scale;
        bool pressing = false;

        control.GuiInput += (@event) =>
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
    private ColorBox _colorBox = new ColorBox(new("f4f4d5"), new("20ff07"), new Color(0, 243, 0), new Color(243, 0, 0));
    private bool _isOpen = true;
    private void OnInfoButtonPressed()
    {
        if (CardViewMode == CardView.Battle) return;
        if (Model == null) return;

        CardDes.DisplayDescription(Model);
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
                    _cardOriginPos = Position;
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
                    ZIndex += instances.Count * 60;

                    KillClickTween();
                    Scale = _baseScale;

                    Drag();
                }
            }

            if (_isDragging)
            {
                Vector2 delta = GetGlobalMousePosition() - _mouseDownGlobal;
                Position = _cardOriginPos + delta;
            }
        }
    }

    private async Task Close()
    {
        instances[this] = null;
        GetNode<GpuParticles2D>("%粒子特效").Emitting = false;
        GetNode<Node2D>("正面卡牌").Visible = false;
        GetNode<TextureRect>("牌背").Visible = false;
        KillReturnTween();
        KillClickTween();
        _isOpen = false;
        if (ChoiceCard == this)
            await EndDrag();
    }

    private void Drag()
    {
        Fighter.CallTargeted(Model, Model.TargetType);
        NodeRoad.CallTargeted(Model, Model.TargetType);
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
        ChoiceCard = this;

        if (CardViewMode == CardView.Battle)
        {
            CardDes.DisplayDescription(Model);
            return;
        }

        ToggleNonBattle();
    }
    private Tween _nonBattleTween;
    private Tween _nonBattleScaleTween;

    private async void ToggleNonBattle()
    {
        var nonBattle = GetNode<Node2D>("%非战斗");
        bool willOpen = !nonBattle.Visible;

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
            nonBattle.Visible = true;

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

            if (isCollection)
            {
                SetControlVisible(infoBtn, true);
            }
            else if (isDeck)
            {
                SetControlVisible(opBtn, true);

                _nonBattleTween = infoBg.CreateTween();
                _nonBattleTween.TweenProperty(infoBg, "size:y", 528f, 0.25f)
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.Out);
                await ToSignal(_nonBattleTween, Tween.SignalName.Finished);

                SetControlVisible(infoBtn, true);
            }
        }
        else
        {
            if (isCollection)
            {
                SetControlVisible(infoBtn, false);

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

                nonBattle.Visible = false;
            }
            else if (isDeck)
            {
                SetControlVisible(infoBtn, false);

                _nonBattleTween = infoBg.CreateTween();
                _nonBattleTween.TweenProperty(infoBg, "size:y", 368f, 0.25f)
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.Out);
                await ToSignal(_nonBattleTween, Tween.SignalName.Finished);

                SetControlVisible(opBtn, false);

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
        if (CardViewMode != CardView.Battle)
        {
            _isDragging = false;
            if (target != null)
            {
                Fighter.DeleteTargeted(Model.TargetType);
                NodeRoad.DeleteTargeted(Model.TargetType);
                target = null;
            }
            ReturnToOrigin();
            return;
        }

        var savedTarget = target;
        ZIndex -= instances.Count * 60;
        if (ChoiceCard == this) ChoiceCard = null;
        Fighter.DeleteTargeted(Model.TargetType);
        NodeRoad.DeleteTargeted(Model.TargetType);
        if (target == null)
            ReturnToOrigin();
        else
        {
            await DiscallDragging();
            Visible = false;
            await CardCmd.PlayedCard(Model, savedTarget);
            await Model.DestroyMe();
        }
    }

    private void ReturnToOrigin()
    {
        KillReturnTween();
        _returnTween = CreateTween();
        _returnTween.SetEase(Tween.EaseType.Out);
        _returnTween.SetTrans(Tween.TransitionType.Quad);
        _returnTween.TweenProperty(this, "position", _cardOriginPos, 0.2f);
    }

    private ITarget target;

    private void AreaEntered(Area2D area)
    {
        if (CardViewMode != CardView.Battle) return;
        if (!Model.CanPlay()) return;
        if (target != null) return;
        if (area.HasMeta("Target"))
        {
            var variant = area.GetMeta("Target");
            var type = (string)area.GetMeta("TargetType");
            if (type == "Fighter")
            {
                if (Model.TargetType.HasFlag(TargetType.Fighters))
                {
                    var col = (Fighter)(GodotObject)variant;
                    if (!col.Calling) return;
                    target = col;
                    col.Targeted();
                    Glowing();
                }
            }
            if (type == "Road")
            {
                if (Model.TargetType.HasFlag(TargetType.Lines))
                {
                    var col = (NodeRoad)(GodotObject)variant;
                    if (!col.Calling) return;
                    target = col;
                    col.Targeted(true, false, false, null);
                    Glowing();
                }
            }
            if (type == "Grid")
            {
                bool needGrid = Model.TargetType.HasFlag(TargetType.Grids)
                     || Model.TargetType.HasFlag(TargetType.CoopGrids);
                bool coopNeedGrid = (Model.TargetType.HasFlag(TargetType.CoopGrids));
                if (!needGrid) return;
                var col = (NodeRoad)(GodotObject)variant;
                if (!col.Calling) return;
                target = col;
                FighterCardModel fighter = null;
                if (Model is FighterCardModel fighterCard)
                    fighter = fighterCard;
                col.Targeted(false, needGrid, coopNeedGrid, fighter);
                Glowing();
            }
        }
    }

    private void AreaExited(Area2D area)
    {
        if (CardViewMode != CardView.Battle) return;
        if (target == null) return;
        if (area.HasMeta("Target"))
        {
            var variant = area.GetMeta("Target");
            if (target == (ITarget)(GodotObject)variant)
            {
                var type = (string)area.GetMeta("TargetType");
                if (type == "Fighter")
                {
                    if (Model.TargetType.HasFlag(TargetType.Fighters))
                    {
                        var col = (Fighter)(GodotObject)variant;
                        col.Distargeted();
                    }
                }
                if (type == "Road")
                {
                    if (Model.TargetType.HasFlag(TargetType.Lines))
                    {
                        var col = (NodeRoad)(GodotObject)variant;
                        col.Distargeted(true, false, false);
                    }
                }
                if (type == "Grid")
                {
                    bool needGrid = Model.TargetType.HasFlag(TargetType.Grids)
                         || Model.TargetType.HasFlag(TargetType.CoopGrids);
                    bool coopNeedGrid = (Model.TargetType.HasFlag(TargetType.CoopGrids));
                    if (needGrid)
                    {
                        var col = (NodeRoad)(GodotObject)variant;
                        col.Distargeted(false, needGrid, coopNeedGrid);
                    }
                }
                Darken();
            }
        }
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
    public void Fresh()
    {
        if (Model != null)
        {

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

                    KillCallTween();
                    isCallDragging = false;
                    _callTween = CreateTween();
                    _callTween.TweenProperty(this, "modulate", new Color(0.6f, 0.6f, 0.6f, 1), 0.4f);
                    _callTween.TweenProperty(GetNode<TextureRect>("%卡牌发光背景"), "modulate", new Color(1, 1, 1, 0f), 0.4f);
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
                        _ => new(128.0f, 96f)
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
                    control = GetNode<Node2D>("%卡面容器").GetChildren().OfType<Control>().First();
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
                        costs.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.ColorB);
                    else
                        costs.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.ColorC);
                }
                if (Model is FighterCardModel fighter)
                {
                    var atks = GetNode<Node2D>("%攻击力容器");
                    atks.Visible = true;
                    var hps = GetNode<Node2D>("%生命值容器");
                    hps.Visible = true;
                    GetNode<Node2D>("%等级").Visible = true;
                    atks.GetNode<RichTextLabel>("数值").Text = $"[center]{fighter.Atk.Current}[/center]";
                    hps.GetNode<RichTextLabel>("数值").Text = $"[center]{fighter.Hp.Current}[/center]";
                    GetNode<SpineHandler>("%伤害动画").LoadSkeletonData(fighter.AtkType.Current.IconSkelPath);
                    GetNode<SpineHandler>("%伤害动画").Scale = new(0.28f, 0.28f);
                    GetNode<SpineHandler>("%血量动画").LoadSkeletonData(fighter.HpType.Current.IconSkelPath);
                    GetNode<SpineHandler>("%血量动画").Scale = new(0.28f, 0.28f);
                    if (fighter.StarType != null)
                    {
                        GetNode<SpineHandler>("%等级").LoadSkeletonData(fighter.StarType.IconSkelPath);
                        GetNode<SpineHandler>("%等级").Scale = new(0.28f, 0.28f);
                        GetNode<SpineHandler>("%等级").SetAnimation(0, $"intro", false);
                    }
                    if (fighter.Hp.HasChanged)
                    {
                        if (fighter.Hp.PositiveChanged)
                            hps.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.ColorB);
                        else
                            hps.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.ColorC);
                    }
                    else
                        hps.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.Default);
                    if (fighter.Atk.HasChanged)
                    {
                        if (fighter.Atk.PositiveChanged)
                            atks.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.ColorB);
                        else
                            atks.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.ColorC);
                    }
                    else
                        atks.GetNode<RichTextLabel>("数值").AddThemeColorOverride("default_color", _colorBox.Default);
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
    /// <summary>
    /// 切换明暗状态
    /// </summary>
    public void JustFresh()
    {
        if (Model == null) return;
        if (CardViewMode == CardView.Battle)
        {
            if (Model.CanPlay())
                Darken();
            else
                Darkest();
        }
        else
        {
            DarkenLight();
        }
    }
}