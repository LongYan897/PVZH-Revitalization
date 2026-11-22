using Card;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Battle;

public interface IBattleHook
{
    public List<Hooker> Hooks { get; }

    public static async void PushHooks(Timing timing, CardModel card, params object[] parameters)
    {
        await HookManager.SpreadHooks(timing, card, parameters).ConfigureAwait(false);
    }
}

public abstract class Hooker : Buff
{
    protected abstract void Hooked(
        Timing hookedTiming,
        CardModel card,
        out Timing timing,
        params object[] parameters);

    protected abstract void Hooking(
        Timing hookedTiming,
        CardModel card,
        params object[] parameters);

    private void PushHook(Timing timing, CardModel card, params object[] parameters)
    {
        Hooked(timing, card, out Timing hookedTiming, parameters);

        if (hookedTiming == timing)
        {
            Hooking(timing, card, parameters);
        }
    }

    public sealed override Timing[] Timings => Enum.GetValues<Timing>();

    public sealed override Task OnTiming(
        Timing timing,
        CardModel card,
        params object[] parameters)
    {
        PushHook(timing, card, parameters);
        return Task.CompletedTask;
    }
}

internal static class HookManager
{
    private static readonly object SyncRoot = new();
    private static readonly List<IBattleHook> Hooks = new();

    public static void Register(IBattleHook hook)
    {
        if (hook == null)
        {
            throw new ArgumentNullException(nameof(hook));
        }

        lock (SyncRoot)
        {
            if (!Hooks.Contains(hook))
            {
                Hooks.Add(hook);
            }
        }
    }

    public static void Unregister(IBattleHook hook)
    {
        if (hook == null)
        {
            return;
        }

        lock (SyncRoot)
        {
            Hooks.Remove(hook);
        }
    }

    public static async Task SpreadHooks(
        Timing timing,
        CardModel card,
        params object[] parameters)
    {
        IBattleHook[] hooks;
        lock (SyncRoot)
        {
            hooks = [.. Hooks];
        }

        foreach (var hook in hooks)
        {
            foreach (var hooker in hook.Hooks.ToArray())
            {
                await hooker.OnTiming(timing, card, parameters).ConfigureAwait(false);
            }
        }
    }
}