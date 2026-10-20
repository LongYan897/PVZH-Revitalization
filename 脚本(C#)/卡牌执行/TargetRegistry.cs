using Card;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Target;

public static class TargetRegistry
{
    private static readonly List<ITarget> _targets = new();
    public static void Clear() => _targets.Clear();
    public static void Register(ITarget target)
    {
        if (target == null) return;
        if (!_targets.Contains(target))
            _targets.Add(target);
    }

    public static void Unregister(ITarget target)
    {
        if (target == null) return;
        _targets.Remove(target);
    }

    public static IEnumerable<ITarget> All => _targets;

    public static bool AnyCanBeTarget(CardModel card, TargetType type, bool fromBuff, Func<ITarget, bool> filter = null)
    {
        if (card == null) return false;
        filter ??= (tg) => true;
        foreach (var t in _targets)
        {
            if ((t.TargetType & type) == 0) continue;
            if (t.CanBeTarget(card, filter, fromBuff)) return true;
        }
        return false;
    }
    
    public static IEnumerable<ITarget> GetAllCanBeTarget(CardModel card, TargetType type, bool fromBuff, Func<ITarget, bool> filter = null)
    {
        var targets = new List<ITarget>();
        filter ??= (tg) => true;
        foreach (var t in _targets)
        {
            if ((t.TargetType & type) == 0) continue;
            if (t.CanBeTarget(card, filter, fromBuff)) targets.Add(t);
        }
        return targets;
    }

    public static void CallTargeted(CardModel card, TargetType type, bool fromBuff)
    {
        CallTargeted(card, type, fromBuff, card.TargetFilter);
    }

    public static void CallTargeted(CardModel card, TargetType type, bool fromBuff, Func<ITarget, bool> filter)
    {
        if (card == null) return;
        filter ??= (tg) => true;
        foreach (var t in _targets.ToList())
        {
            if ((t.TargetType & type) == 0) continue;
            if (!t.CanBeTarget(card, filter, fromBuff)) continue;
            t.OnCallTargeted(type,card);
        }
    }

    public static void DeleteTargeted(TargetType type)
    {
        foreach (var t in _targets.ToList())
        {
            if ((t.TargetType & type) == 0) continue;
            t.OnDeleteTargeted(type);
        }
    }
}