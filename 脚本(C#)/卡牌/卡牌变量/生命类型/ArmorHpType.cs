using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Battle;
using Godot;
using Pack;

namespace Card;

public class ArmorHpType(int amount) : HpType
{
    public static (bool, HpType) Parse(string str)
    {
        var name = str.Split(":")[0];
        var Amount = str.Split(":").Length > 1 ? str.Split(":")[1].ToInt() : 0;
        return (name is "装甲" or "Armor", new ArmorHpType(Amount));
    }
    private class ArmorBuff(int amount) : Buff
    {
        public override bool IsTransferable => false;
        public override int Priority => -1000;
        public override Timing[] Timings => [Timing.ModifyCardDamage];
        public override Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            var stk = parameters[0] as AtkStack;
            stk.FinalDamage-=amount;
            return Task.CompletedTask;
        }
    }
    public int Amount = amount;
    [IconText("ffffff", "91001b", 3)]
    public const string Armor = "res://素材(C#)/文本图片/Icon_HeartArmor.png";
    [URL]
    public static string 装甲 => "对这{#/&:Campifier}{#/&:Camp}造成的=Health=减少{#/&:x}。";
    public override string IconSkelPath => "res://数据资源/属性动画/血量动画.tres";
    public override string IconPath => "res://素材(C#)/文本图片/Icon_HeartArmor.png";
    public override string ExLabel => $"=Armor=_装甲#{Amount}#_";
    public override Buff Buff => new ArmorBuff(Amount);
}
