using Card;
using Godot;
namespace Pack;

public partial class CardPackButton1 : Panel
{
    private CardTile _Tile;
    private int _switchId;   // 切换动画的版本号：连点时让上一次的切换作废
    private bool _isPressed = false;
    private Tween _scaleTween; // 存储 Tween，防止多次点击/悬停导致动画冲突

    public override void _Ready()
    {
        _Tile = GetParent()?.GetParent() as CardTile;
        MouseEntered += OnMouseEnter;
        MouseExited += OnMouseExit;
        PivotOffset = Size / 2;
        // 初始弹出动画
        Scale = new Vector2(0.7f, 0.7f);
        PlayScaleTween(new Vector2(1.0f, 1.0f), 0.5f);
    }

    private void OnMouseEnter()
    {
        // 悬停时放大
        if (!_isPressed)
        {
            SelfModulate = new Color(1.2f, 1.2f, 1.2f);
            PlayScaleTween(new Vector2(1.05f, 1.05f), 0.1f);
        }
    }

    private void OnMouseExit()
    {
        // 移出时恢复颜色和大小
        SelfModulate = new Color(1f, 1f, 1f);
        if (!_isPressed)
        {
            PlayScaleTween(new Vector2(1.0f, 1.0f), 0.1f);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
            {
                _isPressed = true;
                // 点哪个卡包，就把右侧详情栏铺成哪个卡包（先清空上一次的横幅/卡牌）
                ShowPackDetail();
                // 按下时缩小
                PlayScaleTween(new Vector2(0.95f, 0.95f), 0.1f);
            }
            else
            {
                if (_isPressed)
                {
                    _isPressed = false;

                    if (IsMouseInside())
                    {
                        OnButtonClicked();
                    }
                    else
                    {
                        // 移出且松开，恢复默认大小
                        PlayScaleTween(new Vector2(1.0f, 1.0f), 0.1f);
                    }
                }
            }
        }
    }

    private bool IsMouseInside()
    {
        Vector2 mousePos = GetLocalMousePosition();
        return new Rect2(Vector2.Zero, Size).HasPoint(mousePos);
    }

    private void OnButtonClicked()
    {
        // 点击后的回弹动画（假设点击后应该恢复到悬停大小 1.05）
        // 如果你想让点击后变回普通大小 1.0，这里就改成 1.0
        PlayScaleTween(new Vector2(1.05f, 1.05f), 0.3f, Tween.TransitionType.Back);
    }

    /// <summary>
    /// 点击卡包 → 把右侧详情栏（<see cref="CardTile.DetailedCardList"/>）铺成这个卡包的内容：
    /// 先清空上一次的横幅/卡牌，再按 cardPack.json 的 group 重新铺。<br/>
    /// 内容和「卡包详情」场景共用 <see cref="CardPackPreview"/>，区别只是这里的容器是 VBoxContainer，
    /// 它只会竖着排不会折行，所以由 CardPackPreview 自己拼 HBoxContainer 分行。
    /// </summary>
    private async void ShowPackDetail()
    {
        if (_Tile == null)
        {
            GD.PushWarning("[CardPackButton1] 没找到 CardTile（底版的祖父节点），右侧详情栏铺不了东西");
            return;
        }

        VBoxContainer list = _Tile.DetailedCardList;
        if (list == null)
        {
            GD.PushWarning("[CardPackButton1] CardTile.DetailedCardList 还没注入（CardTilesControl 里没赋值？）");
            return;
        }

        // 连点别的卡包时，让前一次还在跑的切换作废
        int switchId = ++_switchId;

        // 1) 旧内容退场：卡牌从上到下依次往右飞走，横幅压扁淡出
        try
        {
            await CardPackTransition.OutAsync(list);
            if (switchId != _switchId || !GodotObject.IsInstanceValid(list)) return;

            // 2) 清空（只留场景自带的「顶部占位」）
            CardPackPreview.Clear(list, "顶部占位");

            // 3) 新内容先以"隐藏"状态铺好：卡牌在屏幕外、横幅压扁透明
            //    480 宽的详情栏里一排 3 张（150*3 + 12*2 = 474）
            await CardPackPreview.Fill(list, _Tile.PackName, cardsPerRow: 3, cardCellWidth: 150f, separation: 12, animateIn: true);
            if (switchId != _switchId || !GodotObject.IsInstanceValid(list)) return;

            // 4) 统一入场：卡牌从右飞回、横幅由细变宽
            await CardPackTransition.InAsync(list);
        }
        catch (System.Exception e)
        {
            GD.PushError($"[CardPackButton1] 铺「{_Tile.PackName}」的详情失败：{e}");
        }
    }

    // 封装一个统一的 Tween 播放函数，防止动画重叠导致的 Bug
    private void PlayScaleTween(Vector2 targetScale, float duration, Tween.TransitionType transType = Tween.TransitionType.Sine)
    {
        // 如果上一次的 Tween 还在跑，先把它杀掉
        if (_scaleTween != null && _scaleTween.IsValid())
        {
            _scaleTween.Kill();
        }

        _scaleTween = CreateTween();
        _scaleTween.TweenProperty(this, "scale", targetScale, duration)
            .SetTrans(transType)
            .SetEase(Tween.EaseType.Out);
    }
}
