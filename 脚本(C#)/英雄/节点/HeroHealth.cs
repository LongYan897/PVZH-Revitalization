using Card;
using Controller;
using Godot;
using Hero.Model;
using Spine;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hero;

[GlobalClass]
/// <summary>
/// 英雄血量和护盾
/// </summary>
public partial class HeroHealth : Node2D
{
    public void ExchangeColor(Camp camp)
    {
        var mat = _shieldMat;
        foreach (var sp in GetChildren().OfType<Sprite2D>().Where(s => s.Name.ToString().Contains("护盾值")))
        {
            sp.Texture = camp switch
            {
                Camp.Zombie => GD.Load<Texture2D>("res://素材/ui/护盾/pip_zombie.png"),
                Camp.Plant => GD.Load<Texture2D>("res://素材/ui/护盾/pip_glow.png"),
                _ => GD.Load<Texture2D>("res://素材/ui/护盾/pip_glow.png")
            };
        }
    }
    public static float GetRot(int index)
    {
        return index * (1f / 8f);
    }
    public async Task LoseShield(HeroModel hero, int amount, SpineHandler shield)
    {
        var mat = _shieldMat;
        Tween tween = CreateTween();
        tween.TweenMethod(Callable.From((float v) =>
        {
            mat.SetShaderParameter("progress", v);
        }), GetRot(hero.Shield.Current), GetRot(hero.Shield.Current - amount), amount * 0.1f).SetTrans(Tween.TransitionType.Quad);
        await ToSignal(tween, Tween.SignalName.Finished);
    }
    public async Task Hurt(HeroModel hero, int amount, int shieldAmount, SpineHandler shield)
    {
        var mat = _shieldMat;
        var i = shieldAmount;
        var str = hero.Camp switch
        {
            Camp.Plant => $"p{i}",
            Camp.Zombie => $"z{i}",
            _ => null
        };

        var hpNode = GetNode<Sprite2D>("生命值");
        var originScale = hpNode.Scale;
        var originModulate = hpNode.Modulate;
        hpNode.Modulate = new Color(1, 1, 1, 1);

        Tween hurtTween = CreateTween();
        hurtTween.SetParallel(true);

        hurtTween.TweenProperty(hpNode, "scale", originScale * 1.5f, 0.15f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
        hurtTween.TweenProperty(hpNode, "scale", originScale, 0.35f)
            .SetDelay(0.15f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

        hurtTween.TweenProperty(hpNode, "modulate", new Color("ffb3b3"), 0.15f)
            .SetEase(Tween.EaseType.Out);
        hurtTween.TweenProperty(hpNode, "modulate", originModulate, 0.35f)
            .SetDelay(0.15f)
            .SetEase(Tween.EaseType.Out);

        if (hero.Shield.Current + i >= 8 && shieldAmount > 0)
        {
            GetNode<SpineHandler>("%护盾").Visible = true;
            GetNode<SpineHandler>("%护盾").SetAttachment("护盾槽位", str);

            var introTask = GetNode<SpineHandler>("%护盾").SetAnimationTask(0, "shieldIntro", false);

            Tween tween3 = CreateTween();
            tween3.TweenMethod(Callable.From((float v) =>
            {
                mat.SetShaderParameter("progress", v);
            }), GetRot(hero.Shield.Current), GetRot(hero.Shield.Current + i), i * 0.1f).SetTrans(Tween.TransitionType.Quad);
            var addShieldTask = ToSignal(tween3, Tween.SignalName.Finished).ToTask();

            await Task.WhenAll(introTask, addShieldTask);

            shield.Visible = true;
            shield.Modulate = hero.Camp switch
            {
                Camp.Plant => new("08ff00"),
                Camp.Zombie => new("f34dff"),
                _ => new("08ff00")
            };
            Tween tween2 = CreateTween();
            tween2.TweenMethod(Callable.From((float v) =>
            {
                mat.SetShaderParameter("progress", v);
            }), GetRot(hero.Shield.Current), GetRot(0), 4 * 0.1f).SetTrans(Tween.TransitionType.Quad);
            await Task.WhenAll(shield.SetAnimationTask(0, hero.Camp switch
            {
                Camp.Zombie => "zombieshield",
                Camp.Plant => "plantshield",
                _ => null
            }
            ), ToSignal(tween2, Tween.SignalName.Finished).ToTask());
            return;
        }

        var final = GetNode<Label>("%数值").Text.ToInt() - amount;
        GetNode<Label>("%数值").Text = $"{final}";
        if (shieldAmount <= 0) return;

        GetNode<SpineHandler>("%护盾").Visible = true;
        GetNode<SpineHandler>("%护盾").SetAttachment("护盾槽位", str);

        var introTask2 = GetNode<SpineHandler>("%护盾").SetAnimationTask(0, "shieldIntro", false);

        Tween tween = CreateTween();
        tween.TweenMethod(Callable.From((float v) =>
        {
            mat.SetShaderParameter("progress", v);
        }), GetRot(hero.Shield.Current), GetRot(hero.Shield.Current + i), i * 0.1f).SetTrans(Tween.TransitionType.Quad);
        var addShieldTask2 = ToSignal(tween, Tween.SignalName.Finished).ToTask();

        await Task.WhenAll(introTask2, addShieldTask2);
    }
    private ShaderMaterial _shieldMat;

    public void SetReady()
    {
        GetNode<SpineHandler>("%护盾").Visible = false;
        GetNode<SpineHandler>("%护盾").LoadSkeletonData("res://素材/ui/护盾/动画/护盾动画.tres");

        var sp = GetNode<Sprite2D>("%护盾高亮");
        _shieldMat = (ShaderMaterial)sp.Material.Duplicate();
        sp.Material = _shieldMat;
        _shieldMat.SetShaderParameter("progress", 0);
    }
}
