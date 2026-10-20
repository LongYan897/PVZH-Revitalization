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
    private static readonly Dictionary<int, (CardModel cannioncial,NodeCard node,CardModel replacement)> freshedCards = new();
    public static async Task HintFresh()
    {
        CardCmd.PushStop();
        freshedCards.Clear();
        var player = PlayerManager.HostPlayer;
        if (PhraseManager.IsInBattle)
        {
            if (player == null) return;
            var initCards = player.Status.DrawCard(4);
            var prepareCards = player.Status.DrawCard(4);
            var loc = BattleSubMenuScene.GetFreshLocation();

            var tasks = new List<Task<NodeCard>>();
            for (int i = 1; i < initCards.Count + 1; i++)
            {
                var task = NodeCard.DrawACard(initCards[i - 1], loc[i], CardView.Battle);
                tasks.Add(task);
            }

            var nodes = await Task.WhenAll(tasks);
            for (int i = 1; i < initCards.Count + 1; i++)
            {
                nodes[i - 1].SetToBright();
                freshedCards[i] = (initCards[i - 1], nodes[i - 1], prepareCards[i - 1]);
            }
        }
        await BattleSubMenuScene.GetFreshTask();
    }
    public static async Task FreshIndex(int index)
    {
        if (freshedCards.Count == 0) return;
        var p = freshedCards[index];
        await p.node.RedrawCard(p.replacement);
        var player = PlayerManager.HostPlayer;
        player.Status.HandAddCard(p.replacement);
        player.Status.InputCard(p.cannioncial);
        p.replacement = null;
        freshedCards[index] = p;
    }
    public static async Task HintFreshEnd()
    {
        var player = PlayerManager.HostPlayer;
        foreach (var p in freshedCards)
        {
            if (p.Value.replacement == null)
            {
                player.Status.InputCard(p.Value.cannioncial);
            }
            else
            {
                player.Status.InputCard(p.Value.replacement);
            }
            p.Value.node.BindOrigin();
        }
        var plantSide = player == PlayerManager.GetPlayer(Camp.Plant);
        var player2 = (plantSide ? PlayerManager.GetPlayer(Camp.Zombie) : PlayerManager.GetPlayer(Camp.Plant));
        if (PhraseManager.IsInBattle)
        {
            if (player2 == null) return;
            await player2.Status.PlayCardAndDraw(4);
        }
        PlayerBox.StuffCard();
        CardCmd.PopStop();
    }
}
