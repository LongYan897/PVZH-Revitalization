using Battle;
using Battle.Entity;
using Card;
using Card.Cmd;
using Card.Imp;
using Card.Pea;
using Godot;
using Pack;

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
    public static Node DesLayer { get; private set; }
    public static Node AudioContainer { get; private set; }
    public override async void _Ready()
    {
        //初始化各各组件
        HpType.Init();
        AtkType.Init();
        Icon.Init();
        CardModel.Init();
        SceneAnimaActor.Init(this);
        //初始化容器
        Animator = GetNode<CanvasLayer>("Animators");
        CardContainer = GetNode<CanvasLayer>("Cards");
        FighterContainer = GetNode<CanvasLayer>("Fighters");
        DesLayer = GetNode<CanvasLayer>("Des");
        AudioContainer = GetNode<Node2D>("Audios");
        //
    }
}
