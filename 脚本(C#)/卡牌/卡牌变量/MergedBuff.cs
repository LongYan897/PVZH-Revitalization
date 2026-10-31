using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 合并的 Buff
/// </summary>
public sealed class MergedBuff : Buff
{
    private readonly Buff[] _buffs;

    public MergedBuff(IReadOnlyList<Buff> buffs)
    {
        _buffs = buffs.ToArray();

        Timings = _buffs.SelectMany(b => b.Timings).Distinct().ToArray();
        Priority = _buffs.Max(b => b.Priority);
        OnlyPlaced = false;
        IsTransferable = _buffs.All(b => b.IsTransferable);
    }

    public override bool IsTransferable { get; }
    public override int Priority { get; }
    public override Timing[] Timings { get; }

    public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
    {
        foreach (var b in _buffs)
        {
            if (!b.Timings.Contains(timing)) continue;
            if (card is FighterCardModel fighter && !fighter.IsPlaced && b.OnlyPlaced) continue;
            await b.OnTiming(timing, card, parameters);
        }
    }
}
