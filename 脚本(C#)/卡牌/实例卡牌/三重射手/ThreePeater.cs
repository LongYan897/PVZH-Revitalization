using Battle;
using Battle.Entity;
using Card.Cmd;
using Hero;
using Play;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card.Pea;

public class ThreePeater : FighterCardModel
{
    public override string AmmoPath => "res://素材(C#)/卡牌/豌豆射手/PeaAmmo.png";

    private class ThreeBuff : Buff
    {
        public override Timing[] Timings => [Timing.ModifyTarget];
        public override bool IsTransferable => false;
        public override int Priority => 200;

        public override Task OnTiming(Timing timing, CardModel card, params object[] parameters)
        {
            if (card is not FighterCardModel fighter) return Task.CompletedTask;
            if (parameters is not [AtkStack stack, ..]) return Task.CompletedTask;
            if (stack.Attacker != fighter) return Task.CompletedTask;
            if (timing != Timing.ModifyTarget) return Task.CompletedTask;
            if (stack.Road == null) return Task.CompletedTask;

            var roads = new List<Road> { stack.Road };
            var behind = CardCmd.GetBehindRoad(stack.Road);
            if (behind != null)
                roads.AddRange(behind.Where(r => r != null));

            for (int i = 0; i < roads.Count; i++)
            {
                var road = roads[i];

                string bone = $"FirePoint";

                var targets = road == stack.Road
                    ? stack.Targets.ToList()
                    : [.. fighter.GetTarget(road)];

                if (road != stack.Road)
                    foreach (var t in targets)
                        stack.AddTarget(t);

                foreach (var t in targets)
                {
                    var sc = stack.AddCurve(t, bone);
                    if (sc == null) continue;

                    if (t is Fighter f)
                        sc.Curve.AddPoint(f.GlobalPosition);
                    else if (t is HeroInstance h)
                    {
                        var nodeRoad = NodeRoad.GetNode(road);
                        if (nodeRoad != null)
                            sc.Curve.AddPoint(nodeRoad.GetHeroLocation(PlayerManager.GetFromHero(h.Hero)));
                    }
                }
            }

            return Task.CompletedTask;
        }
    }

    protected override List<Buff> InitBuffs => [new ThreeBuff()];
}