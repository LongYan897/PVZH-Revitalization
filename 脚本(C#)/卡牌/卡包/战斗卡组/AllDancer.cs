using Card.Dance;
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
    public override IReadOnlyList<CardModel> Cards => 
        new List<CardModel>().RepeatLoad<BackupDancer>(40);
}
