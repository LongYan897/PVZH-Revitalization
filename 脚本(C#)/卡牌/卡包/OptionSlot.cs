using Card;
using Controller;
using Godot;
using Play;
using Scene;

/// <summary>
/// 卡包磁贴上的一个购买按钮（对应 cardPack.json 里 Options 的一项）。<br/>
/// 点击后扣钻石并打开「开包界面」。<br/>
/// 注意：SceneManager 只接受 <see cref="SubMenu"/>（它靠 SubMenu.SubScene 路径实例化场景），
/// 不能直接把场景节点传给 JumpToSub，所以这里自己生成一个隐藏的 SubMenu 按钮。
/// </summary>
public partial class OptionSlot : Control
{
    /// <summary>按钮文字（如“1包/买10包送1包”）</summary>
    public string IntroduceText { get; set; }
    /// <summary>价格（钻石）</summary>
    public int PriceCount { get; set; }
    /// <summary>这个按钮属于哪个卡包（cardPack.json 的对象名）</summary>
    public string PackName { get; set; } = "Pack1";
    /// <summary>这个价格档位抽几张</summary>
    public int PullCount { get; set; } = 4;
    /// <summary>卡包封面图（3D 卡包用的贴图）</summary>
    public string PackTexturePath { get; set; }

    private Label IntroduceLabel;
    private Label PriceLabel;
    private Button HitButton;

    /// <summary>用来调 SceneManager 的隐藏菜单（复用，避免重复生成）</summary>
    private SubMenu _openMenu;

    public override void _Ready()
    {
        IntroduceLabel = GetNode<Label>("购买按钮/按钮文本");
        PriceLabel = GetNode<Label>("购买按钮/CenterContainer/HBoxContainer/钻石数量");
        HitButton = GetNode<Button>("碰撞箱");

        //绑定信号
        HitButton.Pressed += HandlePressed;
    }

    public void Setup()
    {
        IntroduceLabel.Text = IntroduceText;
        PriceLabel.Text = PriceCount.ToString();
    }

    public void HandlePressed()
    {
        // 已经在开包界面里（菜单对应的场景还在）就不重复开
        if (_openMenu != null && GodotObject.IsInstanceValid(_openMenu) && _openMenu.GetScene() != null)
            return;

        var gem = PlayerManager.HostPlayer.Asset.Diamond;
        if (gem < PriceCount) return;

        EnsureOpenMenu();

        // 把开包参数传给开包界面：SubMenu.Payload 会在 AddChild 前塞进场景的 Payload
        _openMenu.Payload = new OpenPackPayload(PackName, PullCount, PackTexturePath);
        SceneManager.JumpToSub(_openMenu);
    }

    /// <summary>创建一个隐藏的 SubMenu（SubMenu._Ready 需要名为 Button 的子节点）</summary>
    private void EnsureOpenMenu()
    {
        if (_openMenu != null && GodotObject.IsInstanceValid(_openMenu) && _openMenu.IsInsideTree())
            return;

        _openMenu = new SubMenu
        {
            Name = "开包菜单",
            SubScene = "res://场景(C#)/卡包/开包界面.tscn",
            ShowLoading = true,
        };
        _openMenu.AddChild(new Button { Name = "Button" });
        AddChild(_openMenu);   // 进树时 SubMenu._EnterTree 会向 SceneManager 注册
    }
}