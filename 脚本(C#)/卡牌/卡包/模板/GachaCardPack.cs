using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pack;
public partial class GachaCardPack : CardPack
{
    private readonly List<CardModel> _cards = new List<CardModel>();
    public override IReadOnlyList<CardModel> Cards => _cards;
    

}
