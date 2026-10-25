using Card;
using Card.Pack;
using Godot;
using Hero.Model;
using Hero.String;
using Pack;
using Play;
using Scene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public partial class SelectDeck : SubMenuScene
{
    public static Camp CurrentCamp = Camp.Plant;
    private Camp DeckCamp = CurrentCamp;
    private PackedScene BallScene;
    private Dictionary<string, NButton> Balls = new Dictionary<string, NButton>();
    private MinimumHFlowContainer BallContainer;
    private MinimumHFlowContainer DeckContainer;
    private Panel BallPanel;
    public override async Task _Load()
    {
        BallScene = GD.Load<PackedScene>("res://场景(C#)/英雄跳转球.tscn");
        BallContainer = GetNode<MinimumHFlowContainer>("主容器/英雄图标背景板/滚动框/英雄图标框");
        DeckContainer = GetNode<MinimumHFlowContainer>("%卡组框");
        BallPanel = GetNode<Panel>("主容器/英雄图标背景板");
        await Task.WhenAll(CreateBalls(),CreateDecks());
        if (DeckCamp == Camp.Plant) BallPanel.SelfModulate = new Color(0.411f, 0.69f, 0.38f, 1.0f);
        else BallPanel.SelfModulate = new Color(0.365f, 0.192f, 0.439f, 1.0f);
    }
    private async Task CreateDecks()
    {
        List<Type> heroes = HeroModel.SeekHeroModel(DeckCamp.ToName());
        foreach (Type Hero in heroes)
        {
            var hero = HeroModel.Identity(Hero.Name);
            var flow = HeroFlow.Flow(hero);
            DeckContainer.AddChild(flow);
            foreach (var deck in PlayerManager.HostPlayer.Presets.Where(d=>d.Hero != null && d.Hero.GetType() == Hero))
            {
                flow.AddChild(CardDeck.Instance(deck));
            }
        }
    }
    private async Task CreateBalls()
    {
        List<Type> heroes = HeroModel.SeekHeroModel(DeckCamp.ToName());
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
        Texture2D texture;
        if (heroName != null) texture = GD.Load<Texture2D>(HeroString.GetData(heroName, "Icon").AsString());
        else
        {
            if (DeckCamp == Camp.Plant) texture = GD.Load<Texture2D>("res://素材(C#)/英雄图标/cornerimage_plants_create.png");
            else texture = GD.Load<Texture2D>("res://素材(C#)/英雄图标/cornerimage_zombies_create.png");
            Image image = texture.GetImage();
            image.Resize(227,227);
            texture = ImageTexture.CreateFromImage(image);
        }
        NButton ball = BallScene.Instantiate<NButton>();
        ball.TextureNormal = texture;
        ball.Disabled = disabled;
        if (heroName != null) Balls.Add(heroName,ball);
        BallContainer.AddChild(ball);
    }
}
