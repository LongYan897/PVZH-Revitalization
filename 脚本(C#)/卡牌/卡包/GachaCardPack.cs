using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pack;
public partial class GachaCardPack : CardPack
{
	private readonly List<CardPackEntry> _cards = new();
	public override IReadOnlyList<CardPackEntry> Cards => _cards;
}
