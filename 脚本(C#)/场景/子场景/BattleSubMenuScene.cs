using Card;
using Card.Cmd;
using Controller;
using Godot;
using Hero;
using Logger;
using Pack;
using Phrases;
using Play;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
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
        Start();
    }
    private static async void Start()
    {
        await Refresh();
        await PhraseManager.StartBattle();
    }
    private static async Task Refresh()
    {
        instance.GetNode<Node2D>("%重新选卡").Visible = true;
        freshAwaiters = new(false);
        await DeckCmd.HintFresh();
        UnableFresh();
        await DeckCmd.HintFreshEnd();
        freshAwaiters = null;
    }
    public static async Task Win(Player player)
    {
        var node = instance.GetNode<Node2D>("%战斗胜利");
        var heroIcon = player.Status.Hero.Data.WinningIcon.Load<Texture2D>();
        node.GetNode<TextureRect>("%Hero").Texture = heroIcon;
        node.Visible = true;

        var icon = node.GetNode<TextureRect>("%Hero");
        var flag = node.GetNode<Sprite2D>("Ribbon");
        var label = node.GetNode<Label>("%Label");
        var glow = node.GetNode<Node2D>("Glow");
        var rect = node.GetNode<ColorRect>("ColorRect");
        var pathFollow = node.GetNode<PathFollow2D>("%PathFollow");
        var path = node.GetNode<Path2D>("%Path");

        label.Position = new(-120f, -710f);
        label.Text = player.Status.Hero.Camp.Plant() ? "植物胜利！" : "僵尸胜利！";
        label.Modulate = new Color(1, 1, 1, 0);
        glow.Position = new(515.0f, 899.0f);
        glow.Scale = new(0, 0);
        flag.Scale = new(1.4f, 1.4f);
        flag.Modulate = new(1, 1, 1, 1);
        flag.Texture = player.Status.Hero.Camp.Switch(
            GD.Load<Texture2D>("res://素材(C#)/战斗胜利/ribbon.png"),
            GD.Load<Texture2D>("res://素材(C#)/战斗胜利/ribbonz.png"));
        icon.Position = Vector2.Zero;
        icon.Scale = new(0, 0);
        rect.Modulate = new Color(1, 1, 1, 0);
        pathFollow.ProgressRatio = 0f;

        var t = instance.CreateTween();
        t.SetParallel(true);

        t.TweenProperty(rect, "modulate", new Color(1, 1, 1, 1), 0.74f)
         .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);

        t.TweenProperty(label, "position", new Vector2(-120f, -46f), 0.9f)
         .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
        t.TweenProperty(label, "modulate", new Color(1, 1, 1, 1), 0.6f)
         .SetEase(Tween.EaseType.Out);

        t.TweenProperty(glow, "position", new Vector2(360.0f, 604.0f), 0.74f)
         .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        t.TweenProperty(glow, "scale", new Vector2(1.5f, 1.5f), 0.74f)
         .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
        t.TweenProperty(glow, "scale", new Vector2(1.4f, 1.4f), 0.3f)
         .SetDelay(0.74f).SetEase(Tween.EaseType.InOut)
         .SetTrans(Tween.TransitionType.Sine);

        t.TweenProperty(icon, "scale", new Vector2(1.1f, 1.1f), 0.74f)
         .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
        t.TweenProperty(icon, "scale", new Vector2(1, 1), 0.3f)
         .SetDelay(0.74f).SetEase(Tween.EaseType.InOut)
         .SetTrans(Tween.TransitionType.Sine);

        t.TweenProperty(pathFollow, "progress_ratio", 0.7061f, 0.99f)
         .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

        await t.ToTask();
        await instance.ToSignal(instance.GetTree().CreateTimer(0.2f), Timer.SignalName.Timeout);

        await WaitForClick();

        var t2 = instance.CreateTween();
        t2.SetParallel(true);

        t2.TweenProperty(flag, "scale", new Vector2(1.6f, 1.6f), 0.15f)
          .SetEase(Tween.EaseType.Out);
        t2.TweenProperty(flag, "scale", new Vector2(3.2f, 3.2f), 0.9f)
          .SetDelay(0.15f).SetEase(Tween.EaseType.In)
          .SetTrans(Tween.TransitionType.Quad);
        t2.TweenProperty(flag, "modulate", new Color(1, 1, 1, 0), 0.8f)
          .SetDelay(0.2f).SetEase(Tween.EaseType.In)
          .SetTrans(Tween.TransitionType.Sine);

        t2.TweenProperty(icon, "scale", new Vector2(1.15f, 1.15f), 0.15f)
          .SetEase(Tween.EaseType.Out);
        t2.TweenProperty(icon, "scale", new Vector2(0, 0), 0.6f)
          .SetDelay(0.15f).SetEase(Tween.EaseType.In)
          .SetTrans(Tween.TransitionType.Back);

        t2.TweenProperty(glow, "scale", new Vector2(1.8f, 1.8f), 0.2f)
          .SetEase(Tween.EaseType.Out);
        t2.TweenProperty(glow, "scale", new Vector2(0, 0), 0.55f)
          .SetDelay(0.2f).SetEase(Tween.EaseType.In)
          .SetTrans(Tween.TransitionType.Back);

        await t2.ToTask();
        SceneManager.ReturnToSub();
    }
    public static async Task Hint(Phrase phrase)
    {

        var tex = instance.GetNode<TextureRect>("%投影");
        var shadTex = tex.Material as ShaderMaterial;
        var TurnMain = instance.GetNode<Label>("%回合提示");
        var TurnSec = instance.GetNode<Label>("%回合提示2");
        var shad = TurnMain.Material as ShaderMaterial;
        var shadSec = TurnSec.Material as ShaderMaterial;
        TurnMain.Text = phrase switch
        {
            Phrase.Plant => "植物",
            Phrase.Zombie => "僵尸",
            Phrase.Trick => "僵尸",
            Phrase.Fight => "战斗！",
            _ => ""
        };
        TurnSec.Text = phrase switch
        {
            Phrase.Plant => "出牌",
            Phrase.Zombie => "出牌",
            Phrase.Trick => "锦囊牌",
            Phrase.Fight => "",
            _ => ""
        };
        var color = phrase switch
        {
            Phrase.Plant => new Color("00ff1e"),
            Phrase.Zombie => new Color("d000ff"),
            Phrase.Trick => new Color("8000ff"),
            Phrase.Fight => new Color("ff6600"),
            _ => new Color()
        }; 
        var colorBtn = phrase switch
        {
            Phrase.Plant => new Color("007e85"),
            Phrase.Zombie => new Color("850037"),
            Phrase.Trick => new Color("370085"),
            Phrase.Fight => new Color("850000"),
            _ => new Color()
        };
        var colorOtl = phrase switch
        {
            Phrase.Plant => new Color("01802374"),
            Phrase.Zombie => new Color("4f018074"),
            Phrase.Trick => new Color("4f018074"),
            Phrase.Fight => new Color("6c0c1974"),
            _ => new Color()
        };
        var colorTex = phrase switch
        {
            Phrase.Plant => new Color("17ff2693"),
            Phrase.Zombie => new Color("7c17ff93"),
            Phrase.Trick => new Color("7c17ff93"),
            Phrase.Fight => new Color("ff6c1793"),
            _ => new Color()
        };
        shadTex.SetShaderParameter("color",colorTex);
        shad.SetShaderParameter("top_color",color);
        shad.SetShaderParameter("bottom_color", colorBtn);
        shadSec.SetShaderParameter("top_color", color);
        shadSec.SetShaderParameter("bottom_color", colorBtn);
        TurnMain.AddThemeColorOverride("font_outline_color",colorOtl);
        TurnSec.AddThemeColorOverride("font_outline_color", colorOtl);
        var tex2 = instance.GetNode<TextureRect>("%阶段投影");
        tex2.Texture = (phrase switch 
        {
            Phrase.Plant => "res://素材/对象高亮框/植物投影.png",
            Phrase.Zombie => "res://素材/对象高亮框/僵尸投影.png",
            Phrase.Trick => "res://素材/对象高亮框/锦囊牌投影.png",
            Phrase.Fight => "res://素材/对象高亮框/战斗投影.png",
            _ => ""
        }).Load<Texture2D>();
        tex.Visible = true;
        tex2.Visible = true;
        tex2.Modulate = new(1, 1, 1, 0);
        Tween tween = instance.CreateTween();
        tex.Scale = new();
        tween.TweenProperty(tex,"scale",new Vector2(1.73f,1.73f) * 1f,0.2f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        Tween tween1 = instance.CreateTween();
        tween1.TweenProperty(tex2, "modulate", new Color(1,1,1,1), 0.2f).SetEase(Tween.EaseType.Out);
        await tween.ToTask();
        await instance.ToSignal(instance.GetTree().CreateTimer(0.2f),Timer.SignalName.Timeout);
        tween = instance.CreateTween();
        tween.TweenProperty(tex, "scale", new Vector2(1.83f, 1.73f) * 1f, 0.1f).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(tex, "scale", new Vector2(1.73f, 1.73f) * 0f, 0.1f).SetEase(Tween.EaseType.In);
        tween1 = instance.CreateTween();
        tween1.TweenProperty(tex2, "modulate", new Color(1, 1, 1, 0), 0.2f).SetEase(Tween.EaseType.Out);
        await tween.ToTask();
        tex.Visible = false;
        tex2.Visible = false;
    }
    private static async Task WaitForClick()
    {
        await instance.ToSignal(instance.GetTree(), SceneTree.SignalName.ProcessFrame);

        while (!Input.IsMouseButtonPressed(MouseButton.Left))
        {
            await instance.ToSignal(instance.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        while (Input.IsMouseButtonPressed(MouseButton.Left))
        {
            await instance.ToSignal(instance.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
    private static async Task Prepare()
    {
        instance.GetNode<Node2D>("%战斗胜利").Visible = false;
        instance.GetNode<Node2D>("%重新选卡").Visible = false;
        instance.GetNode<SpineHandler>("%护盾").LoadSkeletonData("res://素材/ui/护盾/动画/护盾动画.tres");
        instance.GetNode<SpineHandler>("%护盾").Visible = false;
        instance.GetNode<SpineHandler>("%护盾2").LoadSkeletonData("res://素材/ui/护盾/动画/护盾动画.tres");
        instance.GetNode<SpineHandler>("%护盾2").Visible = false;
        instance.GetNode<SpineHandler>("%费用特效动画").Visible = false;
        instance.GetNode<SpineHandler>("%费用特效动画2").Visible = false;
        instance.GetNode<TextureRect>("%费用特效").Visible = true;
        instance.GetNode<TextureRect>("%费用特效2").Visible = true;
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
        instance.GetNode<SpineHandler>("%费用特效动画").SetAttachment("main",PlayerManager.HostPlayer.Status.Hero.Camp switch
        {
            Camp.Zombie => "brain",
            Camp.Plant => "sun",
            _ => ""
        });
        await HeroInstance.Create(PlayerManager.HostPlayer.Status.Hero, instance.GetNode<Marker2D>("%主玩家").GlobalPosition, false);
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

        instance.GetNode<SpineHandler>("%费用特效动画2").SetAttachment("main", pE.Status.Hero.Camp switch
        {
            Camp.Zombie => "brain",
            Camp.Plant => "sun",
            _ => ""
        });
        await HeroInstance.Create(pE.Status.Hero, instance.GetNode<Marker2D>("%副玩家").GlobalPosition, true);
        instance.GetNode<HeroHealth>("%英雄血量2").ExchangeColor(pE.Status.Hero.Camp);
        foreach (var refButton in instance.GetNode<Node2D>("%重新选卡").GetChildren().OfType<TextureRect>())
        {
            if (refButton.Name.ToString().Contains("刷新卡牌"))
            {
                refButton.GetNode<Button>("碰撞").Pressed += () => BattleSubFreshButton(refButton, refButton.Name.ToString().Replace("刷新卡牌","").ToInt());
            }
        }
        instance.GetNode<Node2D>("%重新选卡").GetNode<NinePatchRect>("开始战斗").GetNode<Button>("碰撞").Pressed += BattleSubStartButton;
    }
    private static void UnableFresh()
    {
        foreach (var refButton in instance.GetNode<Node2D>("%重新选卡").GetChildren().OfType<TextureRect>())
        {
            if (refButton.Name.ToString().Contains("刷新卡牌"))
            {
                refButton.GetNode<Button>("碰撞").MouseFilter = Control.MouseFilterEnum.Ignore;
            }
        }
        instance.GetNode<Node2D>("%重新选卡").GetNode<NinePatchRect>("开始战斗").GetNode<Button>("碰撞").MouseFilter = Control.MouseFilterEnum.Ignore;
        instance.GetNode<Node2D>("%重新选卡").Visible = false;
    }
    public static Dictionary<int,Vector2> GetFreshLocation()
    {
        var dict = new Dictionary<int,Vector2>();
        foreach (var refloc in instance.GetNode<Node2D>("%重新选卡").GetChildren().OfType<Marker2D>())
        {
            if (refloc.Name.ToString().Contains('C'))
            {
                dict.Add(refloc.Name.ToString().Replace("C","").ToInt(),refloc.GlobalPosition);
            }
        }
        return dict;
    }
    private static TaskCompletionSource<bool> freshAwaiters;
    public static async Task GetFreshTask()
    {
        if (freshAwaiters == null) return;
        await freshAwaiters.Task;
    }
    private static async void BattleSubFreshButton(TextureRect textureRect,int index)
    {
        if (textureRect == null ||textureRect.Visible == false) return;
        await DeckCmd.FreshIndex(index);
        textureRect.GetNode<Button>("碰撞").MouseFilter = Control.MouseFilterEnum.Ignore;
        textureRect.Visible = false;
    }
    private static void BattleSubStartButton()
    {
        freshAwaiters?.SetResult(true);
    }
    public static async Task PlayHurt(Player player, int amount, int shieldAmount)
    {
        CardCmd.PushStop();
        if (player == PlayerManager.HostPlayer)
        {
            await instance.GetNode<HeroHealth>("%英雄血量").Hurt(player.Status.Hero, amount, shieldAmount, instance.GetNode<SpineHandler>("%护盾"));
        }
        else
        {
            await instance.GetNode<HeroHealth>("%英雄血量2").Hurt(player.Status.Hero, amount, shieldAmount, instance.GetNode<SpineHandler>("%护盾2"));
        }
        CardCmd.PopStop();
    }
    public static async Task PlayLoseShield(Player player,int amount)
    {
        CardCmd.PushStop();
        if (player == PlayerManager.HostPlayer)
        {
            await instance.GetNode<HeroHealth>("%英雄血量").LoseShield(player.Status.Hero, amount, instance.GetNode<SpineHandler>("%护盾"));
        }
        else
        {
            await instance.GetNode<HeroHealth>("%英雄血量2").LoseShield(player.Status.Hero, amount, instance.GetNode<SpineHandler>("%护盾2"));
        }
        CardCmd.PopStop();
    }
    public static async Task PlayCostGain(Player player, int amount)
    {
        CardCmd.PushStop();
        SpineHandler twNode;
        TextureRect tgNode;
        TextureRect glNode;
        TextureRect glNode2;
        Label costLabel;
        Label costLabel2;
        Vector2 posIn;
        Vector2 posStart;
        if (player == PlayerManager.HostPlayer)
        {
            twNode = instance.GetNode<SpineHandler>("%费用特效动画");
            tgNode = instance.GetNode<TextureRect>("%费用图片");
            glNode = instance.GetNode<TextureRect>("%阳光发光");
            glNode2 = instance.GetNode<TextureRect>("%阳光发光3");
            costLabel2 = instance.GetNode<Label>("%数值3");
            costLabel = instance.GetNode<Label>("%数值");
            posIn = new Vector2(-360, -380.0f);
            posStart = new Vector2(166.0f, -266.0f);
        }
        else
        {
            twNode = instance.GetNode<SpineHandler>("%费用特效动画2");
            tgNode = instance.GetNode<TextureRect>("%费用图片2");
            glNode = instance.GetNode<TextureRect>("%阳光发光2");
            glNode2 = instance.GetNode<TextureRect>("%阳光发光4");
            costLabel2 = instance.GetNode<Label>("%数值4");
            costLabel = instance.GetNode<Label>("%数值2");
            posIn = new Vector2(223f, 291.0f);
            posStart = new Vector2(-178.0f, 295.0f);
        }
        costLabel2.Visible = true;
        costLabel2.Text = $"{amount}";
        twNode.Visible = true;
        Tween tween = instance.CreateTween();
        twNode.Position = posIn;
        twNode.Scale = new(0.6f, 0.6f);
        tween.TweenProperty(twNode, "position", posStart, 0.4f / 2).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(twNode, "position", posStart + new Vector2(player == PlayerManager.HostPlayer ? -20 : 20, 0), 0.6f / 3).SetEase(Tween.EaseType.Out);

        string animName = player == PlayerManager.HostPlayer ? "intro_L" : "intro_R";
        float animDur = twNode.GetAnimationDuration(animName);

        await Task.WhenAll(
            instance.ToSignal(tween, Tween.SignalName.Finished).ToTask(),
            twNode.SetAnimationTask(0, animName),
            FollowScale(costLabel2,animDur));

        Tween tween2 = instance.CreateTween();
        tween2.TweenProperty(twNode, "position", new Vector2(), 0.4f);
        await instance.ToSignal(tween2, Tween.SignalName.Finished).ToTask();
        twNode.Visible = false;
        tween = instance.CreateTween();
        tween.TweenProperty(tgNode, "scale", new Vector2(0.8f, 0.8f), 0.1f).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(tgNode, "scale", new Vector2(0.6f, 0.6f), 0.1f).SetEase(Tween.EaseType.Out);
        glNode2.Visible = player.Status.Hero.Camp == Camp.Plant;
        glNode.Visible = player.Status.Hero.Camp == Camp.Plant;
        glNode.Modulate = new Color(1, 1, 1, 0);
        glNode2.Modulate = new Color(1, 1, 1, 0);
        Tween tween1 = instance.CreateTween();
        tween1.TweenProperty(tgNode, "modulate", new Color(1, 1, 1, 1), 0.2f);
        Tween tween3 = instance.CreateTween();
        tween3.TweenProperty(glNode, "modulate", new Color(1,1,1,1), 0.2f);
        tween3.TweenProperty(glNode, "modulate", new Color(1, 1, 1, 0), 0.2f);
        Tween tween4 = instance.CreateTween();
        tween4.TweenProperty(glNode2, "modulate", new Color(1, 1, 1, 1), 0.2f);
        tween4.TweenProperty(glNode2, "modulate", new Color(1, 1, 1, 0), 0.2f);
        var cost = costLabel.Text.ToInt() + amount;
        costLabel.Text = $"{cost}";
        await Task.WhenAll(instance.ToSignal(tween, Tween.SignalName.Finished).ToTask(), instance.ToSignal(tween1, Tween.SignalName.Finished).ToTask(), instance.ToSignal(tween3, Tween.SignalName.Finished).ToTask(), instance.ToSignal(tween4, Tween.SignalName.Finished).ToTask());
        CardCmd.PopStop();
        twNode.Visible = false;
        costLabel2.Visible = false;
        glNode.Visible = false;
        glNode2.Visible = false;
    }
    public static async Task PlayCostLose(Player player, int amount)
    {
        if (amount <= 0) return;

        CardCmd.PushStop();
        try
        {
            SpineHandler twNode;
            TextureRect tgNode;
            Label costLabel;
            if (player == PlayerManager.HostPlayer)
            {
                twNode = instance.GetNode<SpineHandler>("%费用特效动画");
                tgNode = instance.GetNode<TextureRect>("%费用图片");
                costLabel = instance.GetNode<Label>("%数值");
            }
            else
            {
                twNode = instance.GetNode<SpineHandler>("%费用特效动画2");
                tgNode = instance.GetNode<TextureRect>("%费用图片2");
                costLabel = instance.GetNode<Label>("%数值2");
            }

            int loops = amount <= 10 ? 1 : Math.Min(10, amount / 2);
            int baseDeduct = amount / loops;
            int remainder = amount % loops;

            for (int i = 0; i < loops; i++)
            {
                int deduct = baseDeduct + (i < remainder ? 1 : 0);

                var cost = costLabel.Text.ToInt() - deduct;
                costLabel.Text = $"{cost}";

                float speedDiv = Math.Max(1, loops);

                Tween tween = instance.CreateTween();
                tween.TweenProperty(tgNode, "scale", new Vector2(0.75f, 0.75f), 0.1f / speedDiv).SetEase(Tween.EaseType.Out);
                tween.TweenProperty(tgNode, "scale", new Vector2(0.6f, 0.6f), 0.2f / speedDiv).SetEase(Tween.EaseType.Out);

                twNode.Visible = true;
                twNode.Position = Vector2.Zero;
                twNode.Scale = Vector2.Zero;

                Tween tween1 = instance.CreateTween();
                tween1.TweenProperty(tgNode, "modulate", new Color(1, 1, 1, 0.1f), 0.3f / speedDiv);
                tween1.TweenProperty(tgNode, "modulate", new Color(1, 1, 1, 1), 0.1f / speedDiv);

                Tween tween2 = instance.CreateTween();
                twNode.Modulate = new Color(1, 1, 1, 1);
                tween2.TweenProperty(twNode, "scale", new Vector2(0.5f, 0.5f), 0.1f / speedDiv);

                Tween tweenModu = instance.CreateTween();
                tweenModu.TweenProperty(twNode, "modulate", new Color(1, 1, 1, 0), 0.4f / speedDiv);
                tween2.TweenProperty(twNode, "position", new Vector2(0, -120f), 0.3f / speedDiv).SetEase(Tween.EaseType.Out);

                await Task.WhenAll(
                    instance.ToSignal(tween2, Tween.SignalName.Finished).ToTask(),
                    instance.ToSignal(tweenModu, Tween.SignalName.Finished).ToTask(),
                    instance.ToSignal(tween, Tween.SignalName.Finished).ToTask());

                twNode.Visible = false;
                twNode.Modulate = new Color(1, 1, 1, 1);
            }
        }
        finally
        {
            CardCmd.PopStop();
        }
    }
    private static async Task FollowScale(Label label,float dur)
    {
        var cannoncial = label.Position;
        Tween tween = label.CreateTween();
        Tween tween1 = label.CreateTween();
        tween.TweenProperty(label,"scale",new Vector2(0.7f,1f),dur - 0.5f);
        tween1.TweenProperty(label, "position", cannoncial, dur - 0.5f);
        tween.TweenProperty(label, "scale", new Vector2(1f, 1.3f), 0.1f);
        tween1.TweenProperty(label, "position", cannoncial + new Vector2(0, -10f), 0.15f);
        tween.TweenProperty(label, "scale", new Vector2(1f, 0.8f), 0.15f );
        tween1.TweenProperty(label, "position", cannoncial, 0.2f);
        tween.TweenProperty(label, "scale", new Vector2(1f, 1f), 0.2f);
        var pos = label.Position;
    }
}
