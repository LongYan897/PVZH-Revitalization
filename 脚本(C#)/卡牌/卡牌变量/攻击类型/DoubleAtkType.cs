using Battle;
using Card.Cmd;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

public class DoubleAtkType : AtkType
{
    public static (bool, AtkType) Parse(string str)
    {
        var bol = str.Split(":")[0] == "双重打击" || str.Split(":")[0] == "双重" || str.Split(":")[0] == "Double";
        var atk = new DoubleAtkType();
        return (bol, atk);
    }

    public override string IconSkelPath => "res://数据资源/属性动画/攻击动画.tres";
    public override string IconPath => "res://素材(C#)/文本图片/Icon_RapidAttack.png";
    public override string ExLabel => "_双重打击_";

    [URL]
    public static string 双重打击 => "在自己线上的战斗结束后：额外再攻击一次。";
    private bool isTurnFirst;
    private Task Target(Timing timing, CardModel card, params object[] objects)
    {
        if (isTurnFirst && timing == Timing.AfterAttack)
        {
            isTurnFirst = false;
            if (card is FighterCardModel fighter)
            {
                var atkStack = objects[0];
                if (atkStack is AtkStack stack)
                {
                    if (stack.Attacker == fighter)
                    {
                        stack.ExtraAttacks++;
                    }
                }
            }
        }

        if (timing == Timing.BeforeLaneStart)
        {
            isTurnFirst = true;
        }

        return Task.CompletedTask;
    }
    public override Buff Buff => new CustomBuff([Timing.AfterAttack,Timing.AfterLaneStart], Target);
}
