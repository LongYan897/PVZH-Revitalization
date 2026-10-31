using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Variable;

namespace Card;

public interface IHealthCard
{
    CardVariable<HpType> HpType { get; }
}
public static class HealthCardExtensions
{
    public static async Task MergeHp(this IHealthCard card, params HpType[] hpTypes)
    {
        var cur = card.HpType.Current;
        hpTypes = [.. hpTypes.Where(atk => atk is not MergedHpType && ((cur is MergedHpType merged && !merged.HpTypes.Any(tp => tp.GetType() == atk.GetType())) || cur is not MergedHpType))];
        if (hpTypes.Length == 0) return;
        if (cur is NormalHpType && hpTypes.Length == 1)
            await card.HpType.Set(hpTypes[0], VariableReason.Self);
        else if (cur is NormalHpType)
            await card.HpType.Set(HpType.Merge(hpTypes), VariableReason.Self);
        else
            await card.HpType.Set(cur.Merge(hpTypes), VariableReason.Self);
    }
}