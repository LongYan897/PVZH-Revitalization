using Card;
using Card.Cmd;
using Card.Dance;
using Card.Flower;
using Card.Imp;
using Card.Pea;
using Card.Scientist;
using Controller;
using Godot;
using Hero;
using Phrases;
using Play;
using Spine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scene;

[GlobalClass]
public partial class BattleSubMenuScene : SubMenuScene
{
    public override bool HideReturnButton => true;
    private static BattleSubMenuScene instance;
    public override async Task _Load()
    {
        instance = this;
        PhraseManager.BuildUp(GetNode<Node2D>("道路层"), PlayerManager.HostPlayer.Status.Hero.Camp);
        GetNode<SpineHandler>("%动画").LoadSkeletonData("res://素材/ui/轮盘动画/轮盘动画.tres");
        PhraseManager.SetUp(GetNode<Button>("%Button"), GetNode<SpineHandler>("%动画"));
        await Prepare();
        Refresh(PlayerManager.HostPlayer);
    }
    private static async void Refresh(Player player)
    {

    }
    private static async Task Prepare()
    {
        instance.GetNode<SpineHandler>("%护盾").LoadSkeletonData("res://素材/ui/护盾/动画/护盾动画.tres");
        instance.GetNode<SpineHandler>("%护盾").Visible = false;
        instance.GetNode<SpineHandler>("%护盾2").LoadSkeletonData("res://素材/ui/护盾/动画/护盾动画.tres");
        instance.GetNode<SpineHandler>("%护盾").Visible = false;
        instance.GetNode<HeroHealth>("%英雄血量").SetReady();
        instance.GetNode<HeroHealth>("%英雄血量2").SetReady();
        instance.GetNode<Label>("%数值").Text = "0";
        instance.GetNode<Label>("%数值2").Text = "0";
        instance.GetNode<TextureRect>("%费用图片").Texture = GD.Load<Texture2D>(PlayerManager.HostPlayer.Status.Hero.Camp switch
        {
            Camp.Zombie => "res://素材(C#)/文本图片/inhnd_brains.png",
            Camp.Plant => "res://素材(C#)/文本图片/inhnd_sun_gb.png",
            _ => ""
        }); 
        instance.GetNode<TextureRect>("%费用特效").Texture = GD.Load<Texture2D>(PlayerManager.HostPlayer.Status.Hero.Camp switch
        {
            Camp.Zombie => "res://素材(C#)/文本图片/inhnd_brains.png",
            Camp.Plant => "res://素材(C#)/文本图片/inhnd_sun_gb.png",
            _ => ""
        });
        await HeroInstance.Create(PlayerManager.HostPlayer.Status.Hero,instance.GetNode<Marker2D>("%主玩家").GlobalPosition,false);
        instance.GetNode<HeroHealth>("%英雄血量").ExchangeColor(PlayerManager.HostPlayer.Status.Hero.Camp);
        var pE = PlayerManager.HostPlayer.Status.Hero.Camp switch
        {
            Camp.Zombie => PlayerManager.GetPlayer(Camp.Plant),
            Camp.Plant => PlayerManager.GetPlayer(Camp.Zombie),
            _ => null
        };
        instance.GetNode<TextureRect>("%费用图片2").Texture = GD.Load<Texture2D>(pE.Status.Hero.Camp
            switch
        {
            Camp.Zombie => "res://素材(C#)/文本图片/inhnd_brains.png",
            Camp.Plant => "res://素材(C#)/文本图片/inhnd_sun_gb.png",
            _ => ""
        });
        instance.GetNode<TextureRect>("%费用特效2").Texture = GD.Load<Texture2D>(pE.Status.Hero.Camp
            switch
        {
            Camp.Zombie => "res://素材(C#)/文本图片/inhnd_brains.png",
            Camp.Plant => "res://素材(C#)/文本图片/inhnd_sun_gb.png",
            _ => ""
        });
        await HeroInstance.Create(pE.Status.Hero, instance.GetNode<Marker2D>("%副玩家").GlobalPosition,true);
        instance.GetNode<HeroHealth>("%英雄血量2").ExchangeColor(pE.Status.Hero.Camp);
    }
    public static async Task PlayHurt(Player player, int amount, int shieldAmount,CardModel cardModel)
    {
        CardCmd.PushStop();
        if (player == PlayerManager.HostPlayer)
        {
            await instance.GetNode<HeroHealth>("%英雄血量").Hurt(player.Status.Hero,amount,shieldAmount,cardModel,instance.GetNode<SpineHandler>("%护盾"));
        }
        else
        {
            await instance.GetNode<HeroHealth>("%英雄血量2").Hurt(player.Status.Hero,amount,shieldAmount,cardModel, instance.GetNode<SpineHandler>("%护盾2"));
        }
        CardCmd.PopStop();
    }
    private static bool _firstPlay = true;
    public static async Task PlayCostGain(Player player,int amount)
    {
        if (_firstPlay)
        {
            _firstPlay = false;
            CardCmd.PushStop();
        }
        TextureRect twNode;
        TextureRect tgNode;
        TextureRect glNode;
        TextureRect glNode2;
        Label costLabel;
        Label costLabel2;
        Vector2 posIn;
        Vector2 posStart;
        Vector2 posDiff;
        if (player == PlayerManager.HostPlayer)
        {
            twNode = instance.GetNode<TextureRect>("%费用特效");
            tgNode = instance.GetNode<TextureRect>("%费用图片");
            glNode = instance.GetNode<TextureRect>("%阳光发光");
            glNode2 = instance.GetNode<TextureRect>("%阳光发光3");
            costLabel2 = instance.GetNode<Label>("%数值3");
            costLabel = instance.GetNode<Label>("%数值");
            posIn = new Vector2(-360,-380.0f);
            posStart = new Vector2(68.0f, -380.0f);
            posDiff = new Vector2(-30, 0);
        }
        else
        {
            twNode = instance.GetNode<TextureRect>("%费用特效2");
            tgNode = instance.GetNode<TextureRect>("%费用图片2");
            glNode = instance.GetNode<TextureRect>("%阳光发光2");
            glNode2 = instance.GetNode<TextureRect>("%阳光发光4");
            costLabel2 = instance.GetNode<Label>("%数值4");
            costLabel = instance.GetNode<Label>("%数值2");
            posIn = new Vector2(223f, 291.0f);
            posStart = new Vector2(-205.0f, 291.0f);
            posDiff = new Vector2(30, 0);
        }
        glNode2.Visible = player.Status.Hero.Camp == Camp.Plant;
        costLabel2.Visible = true;
        costLabel2.Text = $"{amount}";
        twNode.Visible = true;
        Tween tween = instance.CreateTween();
        twNode.Position = posIn;
        twNode.Scale = new(0.6f, 0.6f);
        tween.TweenProperty(twNode, "position", posStart, 0.4f / 2).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(twNode, "position", posStart + new Vector2(player == PlayerManager.HostPlayer ? -20 : 20, 0), 0.6f /2).SetEase(Tween.EaseType.Out);
        Tween tween2 = instance.CreateTween();
        tween2.TweenProperty(twNode, "scale", new Vector2(1,0.6f), 0.2f/2).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        tween2.TweenProperty(twNode, "scale", new Vector2(0.4f, 0.6f) * 1.05f, 0.6f/2).SetTrans(Tween.TransitionType.Quad);
        tween2.TweenProperty(twNode, "scale", new Vector2(0.6f, 0.6f), 0.1f/2).SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(twNode, "position",tgNode.Position + posDiff,0.4f);
        await Task.WhenAll(instance.ToSignal(tween, Tween.SignalName.Finished).ToTask(),instance.ToSignal(tween2,Tween.SignalName.Finished).ToTask());
        twNode.Visible = false;
        glNode.Visible = true;
        tween = instance.CreateTween();
        tween.TweenProperty(tgNode,"scale", new Vector2(0.8f,0.8f),0.1f).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(tgNode, "scale", new Vector2(0.6f, 0.6f), 0.1f).SetEase(Tween.EaseType.Out);
        glNode.Visible = player.Status.Hero.Camp == Camp.Plant;
        glNode.Modulate = new Color(1, 1, 1, 0);
        Tween tween1 = instance.CreateTween();
        tween1.TweenProperty(tgNode, "modulate", new Color(1,1,1,1),0.2f);
        await Task.WhenAll(instance.ToSignal(tween, Tween.SignalName.Finished).ToTask(), instance.ToSignal(tween1,Tween.SignalName.Finished).ToTask());
        var cost = costLabel.Text.ToInt() + amount;
        costLabel.Text = $"{cost}";
        CardCmd.PopStop();
        _firstPlay = true;
        costLabel2.Visible = false;
        glNode.Visible = false;
        glNode2.Visible = false;
    }
    public static async Task PlayCostLose(Player player,int amount)
    {
        if (_firstPlay)
        {
            _firstPlay = false;
            CardCmd.PushStop();
        }
        TextureRect twNode;
        TextureRect tgNode;
        Label costLabel;
        Vector2 posDiff;
        if (player == PlayerManager.HostPlayer)
        {
            twNode = instance.GetNode<TextureRect>("%费用特效");
            tgNode = instance.GetNode<TextureRect>("%费用图片");
            costLabel = instance.GetNode<Label>("%数值");
            posDiff = new Vector2(-30, 0);
        }
        else
        {
            twNode = instance.GetNode<TextureRect>("%费用特效2");
            tgNode = instance.GetNode<TextureRect>("%费用图片2");
            costLabel = instance.GetNode<Label>("%数值2");
            posDiff = new Vector2(30, 0);
        }
        var cost = costLabel.Text.ToInt() - 1;
        costLabel.Text = $"{cost}";
        Tween tween = instance.CreateTween();
        tween.TweenProperty(tgNode, "scale", new Vector2(0.75f, 0.75f), 0.1f).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(tgNode, "scale", new Vector2(0.6f, 0.6f), 0.2f).SetEase(Tween.EaseType.Out);
        twNode.Visible = true;
        twNode.Position = tgNode.Position + posDiff;
        twNode.Scale = new();
        Tween tween1 = instance.CreateTween();
        tween1.TweenProperty(tgNode, "modulate",new Color(1,1,1,0.1f),0.3f);
        tween1.TweenProperty(tgNode, "modulate", new Color(1, 1, 1, 1), 0.1f);
        Tween tween2 = instance.CreateTween();
        twNode.Modulate = new Color(1, 1, 1, 1);
        tween2.TweenProperty(twNode, "scale", new Vector2(0.5f, 0.5f), 0.1f);
        Tween tweenModu = instance.CreateTween();
        tweenModu.TweenProperty(twNode, "modulate", new Color(1, 1, 1, 0), 0.4f);
        tween2.TweenProperty(twNode, "position", tgNode.Position + posDiff + new Vector2(0,-120f), 0.3f).SetEase(Tween.EaseType.Out);
        await Task.WhenAll( instance.ToSignal(tween2, Tween.SignalName.Finished).ToTask() , instance.ToSignal(tweenModu,Tween.SignalName.Finished).ToTask(),instance.ToSignal(tween, Tween.SignalName.Finished).ToTask());
        twNode.Visible = false;
        twNode.Modulate = new Color(1, 1, 1, 1);
        if (amount > 1)
        {
            await PlayCostLose(player, amount - 1);
        }
        CardCmd.PopStop();
        _firstPlay = true;
    }
}
