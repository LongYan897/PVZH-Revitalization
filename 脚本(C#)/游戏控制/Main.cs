using Battle.Texture;
using Card;
using Card.Cmd;
using Card.Pack;
using Controller.WavPlay;
using Godot;
using Hero;
using Pack;
using Play;
using Scene;
using Spine;
using Target;

namespace Controller;

/// <summary>
/// 游戏的主控
/// </summary>
[GlobalClass]
public partial class Main : Node
{
    public static Node Animator { get; private set; }
    public static Node AmmoAnimator { get; private set; }
    public static Node CardContainer { get; private set; }
    public static Node FighterContainer { get; private set; }
    public static Node HeroContainer { get; private set; }
    public static Node GuestHeroContainer { get; private set; }
    public static Node FighterUp { get; private set; }
    public static Node FighterDown { get; private set; }
    public static Node FighterFrontUp { get; private set; }
    public static Node FighterFrontDown { get; private set; }
    public static Node DesLayer { get; private set; }
    public static Node AudioContainer { get; private set; }
    public static Node SceneContainer { get; private set; }
    public static Node SubSceneContainer { get; private set; }
    public static Loading Load { get; private set; }
    public override async void _Ready()
    {
        //初始化各各组件
        HpType.Init();
        AtkType.Init();
        Icon.Init();
        CardModel.Init();
        PresetPack.Init();
        SceneAnimaActor.Init(this);
        SpineHandler.Init("res://数据资源");
        //初始化容器
        Animator = GetNode<CanvasLayer>("Animators");
        CardContainer = GetNode<CanvasLayer>("Cards");
        FighterContainer = GetNode<CanvasLayer>("Fighters");
        HeroContainer = GetNode<CanvasLayer>("Hero");
        GuestHeroContainer = GetNode<CanvasLayer>("Hero2");
        SceneContainer = GetNode<CanvasLayer>("Scene");
        SubSceneContainer = GetNode<CanvasLayer>("%SubScene");
        FighterUp = FighterContainer.GetNode<Node>("Up");
        FighterFrontUp = FighterContainer.GetNode<Node>("FrontUp");
        FighterDown = FighterContainer.GetNode<Node>("Down");
        FighterFrontDown = FighterContainer.GetNode<Node>("FrontDown");
        AmmoAnimator = FighterContainer.GetNode<Node>("Ammo");
        DesLayer = GetNode<CanvasLayer>("Des");
        AudioContainer = GetNode<Node2D>("Audios");
        Load = GetNode<Loading>("%Load");
        //
        WavPlayer.SetBgMusic<MainMenuWav>();
        TextureBag.SetCurrent<DiscoBag>();
        //
        var p1 = new Player("MME")
        {
            SelectedPreset = PresetPack.Load<AllDancer>()
        };
        var p2 = new AiPlayer("Ai")
        {
            SelectedPreset = PresetPack.Load<AllPea>()
        };
        PlayerManager.Hosting(p1);
        PlayerManager.FillPlayer(PlayerManager.HostPlayer, p2);
        p1.Asset.GainAllCards();
    }

    public void Clear()
    {
        foreach (var item in Animator.GetChildren())
        {
            item.QueueFree();
        }
        foreach (var item in CardContainer.GetChildren())
        {
            item.QueueFree();
        }
        foreach (var item in FighterContainer.GetChildren())
        {
            item.QueueFree();
        }
        foreach (var item in DesLayer.GetChildren())
        {
            item.QueueFree();
        }
        foreach (var item in AudioContainer.GetChildren())
        {
            item.QueueFree();
        }
        TargetRegistry.Clear();
        CardCmd.Clear();
    }

    private readonly System.Text.StringBuilder _cheatBuffer = new();
    private const string CheatCode = "tx2ex3sx4tx5";
    private bool _cheatTriggered;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_cheatTriggered) return;
        if (@event is not InputEventKey key) return;
        if (!key.Pressed || key.Echo) return;
        if (key.Unicode == 0) return;

        char ch = char.ToLowerInvariant((char)key.Unicode);
        if (ch < 0x20) return;

        _cheatBuffer.Append(ch);
        if (_cheatBuffer.Length > CheatCode.Length)
            _cheatBuffer.Remove(0, _cheatBuffer.Length - CheatCode.Length);

        if (_cheatBuffer.Length != CheatCode.Length) return;
        if (_cheatBuffer.ToString() != CheatCode) return;

        _cheatBuffer.Clear();
        _cheatTriggered = true;
        Callable.From(TryEnterTestBattle).CallDeferred();
    }

    private void TryEnterTestBattle()
    {
        try
        {
            var menu = GetNodeOrNull<SubMenu>("%TestBattle");
            if (menu == null)
            {
                GD.PushError("[Cheat] Main 下未找到 %TestBattle");
                return;
            }
            SceneManager.JumpToSub(menu);
        }
        finally
        {
            _cheatTriggered = false;
        }
    }
}