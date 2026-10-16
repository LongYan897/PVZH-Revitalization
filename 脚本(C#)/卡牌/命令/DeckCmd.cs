using Phrases;
using Play;
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
    public static async Task HintFresh()
    {
        var player = PlayerManager.HostPlayer;
        if (PhraseManager.IsInBattle)
        {
            if (player == null) return;
            var initCards = player.Status.DrawCard(4);
            var prepareCards = player.Status.DrawCard(4);
            foreach (var c in initCards)
            {

            }
        }
    }
}
