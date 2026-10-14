using System.Collections.Generic;

namespace Card.Pack;

public partial class GachaCardPack : CardPack
{
    private readonly List<CardPackEntry> _cards = new();
    public override IReadOnlyList<CardPackEntry> Cards => _cards;
}
