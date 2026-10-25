using Card.Dance;
using Card.Metier;
using Card.Scientist;
using Hero.Model;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pack;

public sealed class AllDancer : PresetPack
{
    public override string PackName => "舞动";
    public override HeroModel Hero => throw new NotImplementedException();
    protected override IReadOnlyList<CardModel> DeckCards => 
        new List<CardModel>().RepeatLoad<BackupDancer>(40);
}
