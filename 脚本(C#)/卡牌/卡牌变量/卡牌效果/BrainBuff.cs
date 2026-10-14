using Card.Cmd;
using Godot;
using Phrases;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Card;

/// <summary>
/// 获得脑子的buff
/// </summary>
public class BrainBuff(int amount, Phrase gainPhrase, Func<Task> extenField = null) : Buff
{
    private int Amount = amount;
    private Phrase gainPhrase = gainPhrase;
    private Func<Task> extenField = extenField;
    public override Timing[] Timings => [Timing.AfterPhraseStart];
    public override bool IsTransferable => false;
    public override async Task OnTiming(Timing timing, CardModel card, params object[] parameters)
    {
        if ((Phrase)parameters[0] == gainPhrase)
        {
            await (extenField?.Invoke() ?? Task.CompletedTask);
            await CardCmd.ChangeCost(null, Amount, card);
        }
    }
}
