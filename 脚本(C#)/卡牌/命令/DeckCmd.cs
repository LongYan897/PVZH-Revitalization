using Logger;
using Phrases;
using Play;
using Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Cmd;

/// <summary>
/// 管理卡组的命令
/// </summary>

public static class DeckCmd
{
    private static readonly Dictionary<int, (CardModel cannioncial, NodeCard node, CardModel replacement, bool refreshed)> freshedCards = new();

    public static async Task HintFresh()
    {
        CardCmd.PushStop();
        freshedCards.Clear();
        var player = PlayerManager.HostPlayer;
        if (PhraseManager.IsInBattle)
        {
            if (player?.Status == null) return;
            var initCards = player.Status.DrawCard(4);
            var prepareCards = player.Status.DrawCard(4);
            var loc = BattleSubMenuScene.GetFreshLocation();

            var pending = new List<(int index, CardModel card, Task<NodeCard> task)>();
            var count = Math.Min(initCards.Count, prepareCards.Count);
            for (int i = 0; i < count; i++)
            {
                if (!loc.TryGetValue(i + 1, out var position))
                {
                    player.Status.InputCard(initCards[i]);
                    player.Status.InputCard(prepareCards[i]);
                    continue;
                }
                pending.Add((i + 1, initCards[i], NodeCard.DrawACard(initCards[i], position, CardView.Battle)));
            }

            for (int i = count; i < initCards.Count; i++)
                player.Status.InputCard(initCards[i]);
            for (int i = count; i < prepareCards.Count; i++)
                player.Status.InputCard(prepareCards[i]);

            var nodes = await Task.WhenAll(pending.Select(p => p.task));
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (node == null) continue;
                node.SetToBright();
                freshedCards[pending[i].index] = (pending[i].card, node, prepareCards[pending[i].index - 1], false);
            }
        }
        await BattleSubMenuScene.GetFreshTask();
    }

    public static async Task FreshIndex(int index)
    {
        if (!freshedCards.ContainsKey(index)) return;
        var p = freshedCards[index];
        if (p.refreshed) return;
        if (p.node == null) return;
        await p.node.RedrawCard(p.replacement);
        freshedCards[index] = (p.cannioncial, p.node, p.replacement, true);
    }

    public static async Task HintFreshEnd()
    {
        try
        {
            var player = PlayerManager.HostPlayer;
            if (player?.Status == null)
            {
                freshedCards.Clear();
                return;
            }

            foreach (var entry in freshedCards)
            {
                var (cannioncial, node, replacement, refreshed) = entry.Value;
                if (cannioncial == null || replacement == null) continue;
                if (refreshed)
                {
                    player.Status.HandAddCard(replacement);
                    player.Status.InputCard(cannioncial);
                }
                else
                {
                    player.Status.HandAddCard(cannioncial);
                    player.Status.InputCard(replacement);
                }
            }

            var plantSide = player == PlayerManager.GetPlayer(Camp.Plant);
            var player2 = plantSide ? PlayerManager.GetPlayer(Camp.Zombie) : PlayerManager.GetPlayer(Camp.Plant);
            if (PhraseManager.IsInBattle)
            {
                if (player2?.Status != null)
                    await player2.Status.PlayCardAndDraw(4);
            }
            PlayerBox.StuffCard();
        }
        finally
        {
            freshedCards.Clear();
            CardCmd.PopStop();
        }
    }
}

