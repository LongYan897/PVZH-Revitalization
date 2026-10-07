using Battle;
using Battle.Entity;
using Battle.Texture;
using Card;
using Card.Cmd;
using Card.Dance;
using Card.Flower;
using Card.Imp;
using Card.Pea;
using Controller.WavPlay;
using Godot;
using Pack;
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
    public static Node CardContainer { get; private set; }
    public static Node FighterContainer { get; private set; }
    public static Node FighterUp {  get; private set; }
    public static Node FighterDown { get; private set; }
    public static Node FighterFrontUp { get; private set; }
    public static Node FighterFrontDown { get; private set; }
    public static Node DesLayer { get; private set; }
    public static Node AudioContainer { get; private set; }
    public static Node SceneContainer { get; private set; }
    public static Loading Load { get; private set; }
    public override async void _Ready()
    {
        //初始化各各组件
        HpType.Init();
        AtkType.Init();
        Icon.Init();
        CardModel.Init();
        SceneAnimaActor.Init(this);
        SpineHandler.Init("res://数据资源");
        //初始化容器
        Animator = GetNode<CanvasLayer>("Animators");
        CardContainer = GetNode<CanvasLayer>("Cards");
        FighterContainer = GetNode<CanvasLayer>("Fighters");
        SceneContainer = GetNode<CanvasLayer>("Scene");
        FighterUp = FighterContainer.GetNode<Node>("Up");
        FighterFrontUp = FighterContainer.GetNode<Node>("FrontUp");
        FighterDown = FighterContainer.GetNode<Node>("Down");
        FighterFrontDown = FighterContainer.GetNode<Node>("FrontDown");
        DesLayer = GetNode<CanvasLayer>("Des");
        AudioContainer = GetNode<Node2D>("Audios");
        Load = GetNode<Loading>("%Load");
        //
        WavPlayer.SetBgMusic<MainMenuWav>();
        TextureBag.SetCurrent<DiscoBag>();
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
}
