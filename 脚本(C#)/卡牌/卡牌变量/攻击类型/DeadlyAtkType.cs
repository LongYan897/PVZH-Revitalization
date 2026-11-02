using Battle;
using Battle.Entity;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Variable;

namespace Card;

public class DeadlyAtkType : AtkType
{
    public static (bool, AtkType) Parse(string str)
    {
        var bol = str.Split(":")[0] == "致命" || str.Split(":")[0] == "Dead" || str.Split(":")[0] == "Deadly" || str.Split(":")[0] == "Lethal";
        var atk = new DeadlyAtkType();
        return (bol, atk);
    }

    public override string IconSkelPath => "res://数据资源/属性动画/攻击动画.tres";
    public override string IconPath => "res://素材(C#)/文本图片/Icon_Deadly.png";
    public override string ExLabel => "=Dead=_致命_";

    [URL]
    public static string 致命 => "_消灭_被这只僵尸造成=Health=的植物。";
    [IconText("ffffff")]
    public static string Dead => "res://素材(C#)/文本图片/Icon_Deadly.png";
    private async Task Target(Timing timing, CardModel card, params object[] objects)
    {
        if (card is not FighterCardModel attacker) return;
        if (objects.Length < 1 || objects[0] is not AtkStack stack) return;
        if (stack.Attacker != attacker) return;

        foreach (var t in stack.Targets)
        {
            if (t.InferAsCard() != null)
                await t.InferAsCard().Die(VariableReason.Fighter | VariableReason.Damaged, stack);
        }
    }
    public override Buff Buff => new CustomBuff([Timing.OnDamaged], Target);
}
