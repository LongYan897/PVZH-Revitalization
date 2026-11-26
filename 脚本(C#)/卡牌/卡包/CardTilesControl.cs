using Card;
using Controller;
using Godot;
using Pack;
using Scene;
using System.Threading.Tasks;

public partial class CardTilesControl : SubMenuScene
{
    private BoxContainer CardTilesGroup;
    private Panel Container;
    private Tween Tween1;
    private TextureButton BackButton;
    private bool _closing;
    private VBoxContainer DetailCardList;

    public override bool HideReturnButton => true;

    public override Task _Load()
    {
        CardTilesGroup = GetNode<BoxContainer>("卡包遮罩/ScrollContainer/VBoxContainer");
        Container = GetNode<Panel>("卡包遮罩");
        DetailCardList = GetNode<VBoxContainer>("卡包遮罩/卡包详情/ScrollContainer2/VBoxContainer");

        BackButton = GetNodeOrNull<TextureButton>("返回按钮");
        if (BackButton != null)
            BackButton.Pressed += PlayOutro;
        else
            GD.PushWarning("[CardTilesControl] 场景里没找到 返回按钮(TextureButton)");

        IntroAn(0.6f);
        var Tile = "res://场景(C#)/卡包/卡包.tscn".Load<PackedScene>();
        for (int i = 1; i <= 5; i++)
        {
            var NewTile = Tile.Instantiate<CardTile>();
            CardTilesGroup.AddChild(NewTile);
            // 右侧详情栏是本场景的节点，卡包.tscn 里没有，必须从外面注入给磁贴
            NewTile.DetailedCardList = DetailCardList;
            NewTile.LoadResource(i % 2 == 0 ? "Pack1" : "Pack2");

        }
        return Task.CompletedTask;
    }
    private void IntroAn(float time)
    {
        if (Container == null) return;
        Vector2 restPos = Container.Position;
        Container.Position = new Vector2(restPos.X, GetViewportRect().Size.Y);
        if (BackButton != null)
            BackButton.Modulate = new Color(1, 1, 1, 1);
        QuickTween().TweenProperty(Container, "position", restPos, time)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
    }

    private async void PlayOutro()
    {
        if (!SceneManager.IsCurrent(this)) return;
        if (_closing || Container == null) return;
        _closing = true;

        Vector2 outPos = new Vector2(Container.Position.X, GetViewportRect().Size.Y);
        var tween = QuickTween();
        tween.TweenProperty(Container, "position", outPos, 0.3f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        if (BackButton != null)
            tween.Parallel().TweenProperty(BackButton, "modulate:a", 0.0f, 0.2f);

        await ToSignal(tween, Tween.SignalName.Finished);
        _closing = false;
        OnOutroFinished();
    }

    protected virtual void OnOutroFinished()
    {
        SceneManager.ReturnToSub(this);
    }
    private Tween QuickTween()
    {
        if (Tween1 != null && Tween1.IsValid())
            Tween1.Kill();
        Tween1 = CreateTween();
        return Tween1;
    }
}
