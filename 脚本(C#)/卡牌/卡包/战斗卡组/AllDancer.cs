using Card.Dance;
using Card.Metier;
using Card.Scientist;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pack;

public class AllDancer : PresetPack
{
    public override string PackName => "舞动";
    protected override IReadOnlyList<CardModel> DeckCards => 
        new List<CardModel>().RepeatLoad<BackupDancer>(20);
}
