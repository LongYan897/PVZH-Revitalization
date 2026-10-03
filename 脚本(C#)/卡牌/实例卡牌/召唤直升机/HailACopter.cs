using Battle;
using Battle.Entity;
using Card.Cmd;
using Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Target;

namespace Card.Imp;

public class HailACopter : CardModel
{
    public override TargetType TargetType => TargetType.Lines;
    public override bool TargetFilter(ITarget target)
    {
        return target.CanBeRoad(out Road road) && Fighter.CanGenerate(GetTemplate<CopterCommando>() as FighterCardModel, road);
    }
    public override async Task Play(ITarget target)
    {
        WavPlayer.Play("res://素材(C#)/卡牌/召唤直升机/intro_1.wav");
        await PlayInstantAnimation("intro", target);
        if (target.CanBeRoad(out Road road))
        {
             await CardCmd.FighterGenerate(Load<CopterCommando>() as FighterCardModel, road, Location.Zombie);
        }
    }
}
