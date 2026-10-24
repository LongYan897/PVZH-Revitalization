using Card;
using Godot;
using Hero.Model;
using Hero.String;
using Pack;
using Scene;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class SelectDeck : SubMenuScene
{
    private Camp camp = Camp.Plant;
    private PackedScene BallScene;
    private Dictionary<string, NButton> Balls;
    private MinimumHFlowContainer BallContainer;
    public override async Task _Load()
    {
        BallScene = GD.Load<PackedScene>("res://场景(C#)/英雄跳转球.tscn");
        BallContainer = GetNode<MinimumHFlowContainer>("主容器/英雄图标背景板/滚动框/英雄图标框");
        await CreateBalls();
    }
    private async Task CreateBalls()
    {
        List<Type> heroes = HeroModel.SeekHeroModel(camp.ToName());
        int x = 1;
        foreach (Type hero in heroes)
        {
            await CreateBall(hero.Name);
            x++;
            if (x > 6) x = 1;
        }
        if (x > 1 && x <= 6)
        {
            for (int i = x;i <= 6; i++)
            {
                await CreateBall(null, true);
            }
        }
    }
    private async Task CreateBall(string heroName,bool disabled = false)
    {
        string path;
        if (heroName != null) path = HeroString.GetData(heroName, "icon").AsString();
        else
        {
            if (camp == Camp.Plant) path = "";
            else path = "";
        }
        NButton ball = BallScene.Instantiate<NButton>();
        await ball.ToSignal(ball,NButton.SignalName.Ready);
        ball.TextureNormal = GD.Load<Texture2D>(path);
        ball.Disabled = disabled;
        Balls.Add(heroName,ball);
        BallContainer.AddChild(ball);
    }
}
