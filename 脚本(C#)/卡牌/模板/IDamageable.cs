using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Variable;

namespace Card;

public interface IDamageCard
{
    CardVariable<AtkType> AtkType { get; }
}
public static class DamageCardExtensions
{
    public static async Task MergeAtk(this IDamageCard card, params AtkType[] atkTypes)
    {
        var cur = card.AtkType.Current;
        atkTypes = [.. atkTypes.Where(atk => atk is not MergedAtkType && ((cur is MergedAtkType merged && !merged.AtkTypes.Any(tp => tp.GetType() == atk.GetType())) || cur is not MergedAtkType))];
        if (atkTypes.Length == 0) return;
        if (cur is NormalAtkType && atkTypes.Length == 1)
            await card.AtkType.Set(atkTypes[0], VariableReason.Self);
        else if (cur is NormalAtkType)
            await card.AtkType.Set(AtkType.Merge(atkTypes), VariableReason.Self);
        else
            await card.AtkType.Set(cur.Merge(atkTypes), VariableReason.Self);
    }
}