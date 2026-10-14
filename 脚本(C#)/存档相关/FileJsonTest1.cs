using Card.Pack;
using Godot;
using System.Collections.Generic;

namespace FileExtension
{
    public partial class FileJsonTest1 : Node2D
    {
        public override void _Ready()
        {
            FileJson fj = new();
            fj.Open("res://数据(C#)/playerAsset.json")
                .OpenObject("CardCounts");
            Dictionary<string, int> cardCounts = fj.GetEntries<int>();
            cardCounts["Peashooter"] += 1;
            cardCounts["SunFlower"] += 1;

            fj.SetEntries(cardCounts);
            foreach (var entry in cardCounts)
            {
                GD.Print($"{entry.Key}: {entry.Value}");
            }
            fj.Save();
            CardPackExtensions cardPack = new(0);
            var cards = cardPack.DrawCard("pack1", 100);
            foreach (var card in cards)
            {
                GD.Print($"cardName:{card.Card.PadRight(15)}\tCount:{card.Count}");
            }
        }
    }
}
