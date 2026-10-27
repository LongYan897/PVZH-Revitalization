using Battle;
using Card.Cmd;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

public class TrackAtkType : AtkType
{
    public static (bool, AtkType) Parse(string str)
    {
        var bol = str.Split(":")[0] == "追踪" || str.Split(":")[0] == "Track";
        var atk = new TrackAtkType();
        return (bol, atk);
    }

    public override string IconSkelPath => "res://数据资源/属性动画/攻击动画.tres";
    public override string IconPath => "res://素材(C#)/文本图片/Track.png";
    public override string ExLabel => "=Track=_追踪_";

    [IconText("ffffff")]
    public const string Track = "res://素材(C#)/文本图片/Track.png";
    [URL]
    public static string 追踪 => "攻击最左侧的僵尸。"; private static Task Target(Timing timing, CardModel card, params object[] objects)
    {
        if (card is not FighterCardModel fighter) return Task.CompletedTask;
        if (objects is not [AtkStack stack, ..]) return Task.CompletedTask;
        if (stack.Attacker != fighter) return Task.CompletedTask;

        if (timing == Timing.ModifyTarget)
        {
            stack.Pierce = false;
            stack.TargetsRedirected = true;
            var target = CardCmd.GetFirstZombie();
            if (target == null || target.IsDie) return Task.CompletedTask;
            if (target.Attacking() is not { } attacking) return Task.CompletedTask;
            stack.ReplaceTarget(attacking);
        }

        return Task.CompletedTask;
    }

    public override Buff Buff => new CustomBuff([Timing.ModifyTarget], Target, -100) ;
}
