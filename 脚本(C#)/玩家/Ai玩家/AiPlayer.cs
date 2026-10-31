using Battle;
using Battle.Entity;
using Card;
using Card.Cmd;
using Godot;
using Hero;
using Logger;
using Phrases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Target;
using Variable;

namespace Play;

public class AiPlayer : Player
{
	public AiPlayer(string id) : base(id) { }

	private const int HandFeat = 10;
	private const int MaxHand = 10;
	private const int FighterFeat = 12;
	private const int MaxFighters = 12;
	private const int GlobalFeat = 27;
	private const int TgtFeat = 8;
	private const int InDim = MaxHand * HandFeat + MaxFighters * FighterFeat + GlobalFeat;
	private const int HidDim = 128;
	private const int HandOut = MaxHand + 1;

	private static readonly float[] W1 = new float[HidDim * InDim];
	private static readonly float[] B1 = new float[HidDim];
	private static readonly float[] W2 = new float[HidDim * HidDim];
	private static readonly float[] B2 = new float[HidDim];
	private static readonly float[] WH = new float[HandOut * (HidDim + HandFeat)];
	private static readonly float[] BH = new float[HandOut];
	private static readonly float[] WtHand = new float[HidDim * (HidDim + HandFeat)];
	private static readonly float[] BtHand = new float[HidDim];
	private static readonly float[] WtTgt = new float[HidDim * (HidDim + TgtFeat)];
	private static readonly float[] BtTgt = new float[HidDim];
	private static readonly float[] WV = new float[HidDim];
	private static readonly float[] BV = new float[1];

	private static readonly float[] gW1 = new float[W1.Length];
	private static readonly float[] gB1 = new float[B1.Length];
	private static readonly float[] gW2 = new float[W2.Length];
	private static readonly float[] gB2 = new float[B2.Length];
	private static readonly float[] gWH = new float[WH.Length];
	private static readonly float[] gBH = new float[BH.Length];
	private static readonly float[] gWtHand = new float[WtHand.Length];
	private static readonly float[] gBtHand = new float[BtHand.Length];
	private static readonly float[] gWtTgt = new float[WtTgt.Length];
	private static readonly float[] gBtTgt = new float[BtTgt.Length];
	private static readonly float[] gWV = new float[WV.Length];
	private static readonly float[] gBV = new float[BV.Length];

	private static readonly float[] mW1 = new float[W1.Length], vW1 = new float[W1.Length];
	private static readonly float[] mB1 = new float[B1.Length], vB1 = new float[B1.Length];
	private static readonly float[] mW2 = new float[W2.Length], vW2 = new float[W2.Length];
	private static readonly float[] mB2 = new float[B2.Length], vB2 = new float[B2.Length];
	private static readonly float[] mWH = new float[WH.Length], vWH = new float[WH.Length];
	private static readonly float[] mBH = new float[BH.Length], vBH = new float[BH.Length];
	private static readonly float[] mWtHand = new float[WtHand.Length], vWtHand = new float[WtHand.Length];
	private static readonly float[] mBtHand = new float[BtHand.Length], vBtHand = new float[BtHand.Length];
	private static readonly float[] mWtTgt = new float[WtTgt.Length], vWtTgt = new float[WtTgt.Length];
	private static readonly float[] mBtTgt = new float[BtTgt.Length], vBtTgt = new float[BtTgt.Length];
	private static readonly float[] mWV = new float[WV.Length], vWV = new float[WV.Length];
	private static readonly float[] mBV = new float[BV.Length], vBV = new float[BV.Length];

	private static readonly float[] xBuf = new float[InDim];
	private static readonly float[] h1 = new float[HidDim];
	private static readonly float[] h2 = new float[HidDim];
	private static readonly float[] logH = new float[HandOut];
	private static readonly float[] handEmbBuf = new float[HidDim];
	private static readonly float[] tmpFeat = new float[HidDim + 16];

	private static readonly Random Rng = new(20240601);
	private static bool InitDone = false;
	private static bool RulesRegistered = false;
	private static bool _weightsLoaded = false;
	private static int adamT = 0;
	private static bool _tickRunning = false;
	private static int _stepsSinceSave = 0;

	private const float Gamma = 0.99f;
	private const float Lr = 3e-4f;
	private const float Beta1 = 0.9f;
	private const float Beta2 = 0.999f;
	private const float Eps = 1e-8f;
	private const float ValueCoef = 0.5f;
	private const int SaveEverySteps = 10;

	private const string WeightPath = "res://数据资源/ai_weights.json";

	private static readonly List<Func<PlayerBox, CardModel, ITarget, float>> ActionRules = new();

	public static bool TrainingMode = true;

	private bool _hasPending = false;
	private readonly float[] _pendingState = new float[InDim];
	private readonly float[] _pendingHandMask = new float[HandOut];
	private int _pendingHandIdx;
	private int _pendingTgtCount;
	private float _pendingValue;

	public static void RegisterAction(Func<PlayerBox, CardModel, ITarget, float> rule)
	{
		if (rule == null) return;
		ActionRules.Add(rule);
	}

	private float GetActionBonus(CardModel card, ITarget target)
	{
		float sum = 0f;
		foreach (var rule in ActionRules) sum += rule(Status, card, target);
		return sum;
	}

	public static void Init()
	{
		if (!_weightsLoaded)
		{
			if (!TryLoadWeights()) InitWeights();
			_weightsLoaded = true;
		}
		if (!RulesRegistered) { RegisterDefaultRules(); RulesRegistered = true; }
		InitDone = true;
	}

	private static void InitWeights()
	{
		float s1 = MathF.Sqrt(2f / InDim);
		for (int i = 0; i < W1.Length; i++) W1[i] = (float)(Rng.NextDouble() * 2 - 1) * s1;

		float s2 = MathF.Sqrt(2f / HidDim);
		for (int i = 0; i < W2.Length; i++) W2[i] = (float)(Rng.NextDouble() * 2 - 1) * s2;

		float sh = MathF.Sqrt(2f / (HidDim + HandFeat));
		for (int i = 0; i < WH.Length; i++) WH[i] = (float)(Rng.NextDouble() * 2 - 1) * sh;
		for (int i = 0; i < WtHand.Length; i++) WtHand[i] = (float)(Rng.NextDouble() * 2 - 1) * sh;

		float stt = MathF.Sqrt(2f / (HidDim + TgtFeat));
		for (int i = 0; i < WtTgt.Length; i++) WtTgt[i] = (float)(Rng.NextDouble() * 2 - 1) * stt;

		for (int i = 0; i < WV.Length; i++) WV[i] = (float)(Rng.NextDouble() * 2 - 1) * 0.1f;
	}

	private static bool TryLoadWeights()
	{
		try
		{
			if (!Godot.FileAccess.FileExists(WeightPath)) return false;
			using var f = Godot.FileAccess.Open(WeightPath, Godot.FileAccess.ModeFlags.Read);
			if (f == null) return false;
			string json = f.GetAsText();
			using var doc = JsonDocument.Parse(json);
			var r = doc.RootElement;
			ReadArr(r, "W1", W1); ReadArr(r, "B1", B1);
			ReadArr(r, "W2", W2); ReadArr(r, "B2", B2);
			ReadArr(r, "WH", WH); ReadArr(r, "BH", BH);
			ReadArr(r, "WtHand", WtHand); ReadArr(r, "BtHand", BtHand);
			ReadArr(r, "WtTgt", WtTgt); ReadArr(r, "BtTgt", BtTgt);
			ReadArr(r, "WV", WV); ReadArr(r, "BV", BV);
			if (r.TryGetProperty("adamT", out var t)) adamT = t.GetInt32();
			return true;
		}
		catch { return false; }
	}

	private static void ReadArr(JsonElement root, string name, float[] dst)
	{
		if (!root.TryGetProperty(name, out var arr)) return;
		int i = 0;
		foreach (var e in arr.EnumerateArray())
		{
			if (i >= dst.Length) break;
			dst[i++] = e.GetSingle();
		}
	}

	private static void RegisterDefaultRules()
	{
		RegisterAction((s, card, target) =>
		{
			if (card is FighterCardModel f && target is Fighter t && t.Model != null)
			{
				bool kill = f.Atk.Current >= t.Model.Hp.Current;
				bool die = t.Model.Atk.Current >= f.Hp.Current;
				if (kill && !die) return 0.5f;
				if (kill && die) return 0.2f;
				if (!kill && die) return -0.3f;
			}
			return 0f;
		});

		RegisterAction((s, card, target) =>
		{
			if (card == null) return 0f;
			int left = s.Hero.Cost.Current - card.Cost.Current;
			if (left <= 0) return 0f;
			bool canAgain = s.HandCards.Any(c => c != card && c.CanPlay() && c.Cost.Current <= left);
			return canAgain ? 0.2f : 0f;
		});

		RegisterAction((s, card, target) =>
			card != null && target is HeroInstance h && h.Hero.Camp != s.Hero.Camp ? 0.4f : 0f);

		RegisterAction((s, card, target) => card is FighterCardModel ? 0.2f : 0f);
		RegisterAction((s, card, target) => card == null ? -0.1f : 0f);
		RegisterAction((s, card, target) =>
			card != null && card.Cost.Current == s.Hero.Cost.Current ? 0.15f : 0f);
	}

	public Task<ITarget> ChoiceTarget(CardModel cardModel, TargetType targetType, bool fromBuff, Func<ITarget, bool> filter = null)
	{
		var candidates = CardCmd.GetTargets(cardModel, targetType, fromBuff, filter).ToList();
		if (candidates.Count == 0) return Task.FromResult<ITarget>(null);

		if (!InitDone) Init();

		var s = Status;
		if (s == null) return Task.FromResult(candidates[0]);

		Encode(s, xBuf);
		ForwardTrunk(xBuf);

		int handIdx = -1;
		var hand = s.HandCards;
		for (int k = 0; k < hand.Count && k < MaxHand; k++)
			if (ReferenceEquals(hand[k], cardModel)) { handIdx = k; break; }
		if (handIdx < 0) return Task.FromResult(candidates[0]);

		var scores = ScoreTargets(xBuf, handIdx, candidates);
		for (int k = 0; k < candidates.Count; k++)
			scores[k] += 0.1f * GetActionBonus(cardModel, candidates[k]);

		return Task.FromResult(candidates[ArgMax(scores)]);
	}
    private static (Road road, Location? loc) ParseGrid(ITarget t)
    {
        if (t is not NodeRoad nodeRoad || !nodeRoad.CanBeRoad(out var road))
            return (null, null);

        Location? loc = nodeRoad.LastTargetKind switch
        {
            RoadTargetKind.None => null,
            RoadTargetKind.Grid => nodeRoad.ClickedCamp == null
                ? null
                : (nodeRoad.ClickedCamp is Camp c && c.Plant() ? Location.Plant : Location.Zombie),
            RoadTargetKind.CoopGrid => Location.PlantFront,
            _ => null
        };
        return (road, loc);
    }

    private static float EvaluatePlacement(Road road, Location? loc, CardModel card)
    {
        if (road == null || card == null) return 0f;

        var myCamp = card.Camp;
        bool isPlant = myCamp == Camp.Plant;
        bool isZombie = myCamp == Camp.Zombie;
        float score = 0f;

        var roadUnits = TargetRegistry.All.OfType<Fighter>()
            .Where(f => f.Model != null && !f.Model.IsDie
                     && f.Road != null && f.Road.Index == road.Index)
            .ToList();

        Fighter occupant = null;
        if (loc != null)
            foreach (var f in roadUnits)
                if (f.Model.Location == loc) { occupant = f; break; }

        if (loc != null)
        {
            if (isPlant)
            {
                if (loc == Location.Plant) score += 0.6f;
                else if (loc == Location.PlantFront) score += 0.4f;
                else if (loc == Location.Zombie) score -= 0.6f;
            }
            else if (isZombie)
            {
                if (loc == Location.Zombie) score += 0.6f;
                else if (loc == Location.PlantFront) score += 0.3f;
                else if (loc == Location.Plant) score -= 0.6f;
            }

            if (occupant != null)
            {
                bool sameCamp = occupant.Model.Camp == myCamp;
                score += sameCamp ? -1.5f : 0.2f;
            }
        }

        float enemyPressure = 0f, friendPresence = 0f;
        foreach (var f in roadUnits)
        {
            var m = f.Model;
            float power = m.Atk.Current + m.Hp.Current * 0.5f;
            if (m.Camp == myCamp) friendPresence += power;
            else enemyPressure += power;
        }

        if (card is FighterCardModel fcard)
        {
            bool defensive = fcard.Atk.Current <= 0;

            if (defensive)
            {
                score += MathF.Min(enemyPressure, 25f) * 0.04f;
                if (loc == Location.Plant || loc == Location.Zombie) score += 0.15f;
                if (enemyPressure == 0f) score -= 0.3f;
            }
            else
            {
                if (enemyPressure == 0f) score += 0.35f;
                else
                {
                    score += MathF.Min(enemyPressure, 20f) * 0.02f;
                    if (friendPresence > enemyPressure * 1.5f) score -= 0.2f;
                }
            }
        }

        return score;
    }

    public Task<(Road road, Location? location)> ChoiceGrid(
    CardModel cardModel, bool fromBuff, Func<Road, bool> filter = null)
    {
        Func<ITarget, bool> itgFilter = itg =>
            itg.CanBeRoad(out var r) && (filter == null || filter(r));

        var candidates = CardCmd.GetTargets(cardModel, TargetType.Grids, fromBuff, itgFilter).ToList();
        if (candidates.Count == 0) return Task.FromResult<(Road, Location?)>((null, null));

        if (!InitDone) Init();
        var s = Status;

        var parsed = new (Road road, Location? loc)[candidates.Count];
        for (int k = 0; k < candidates.Count; k++)
            parsed[k] = ParseGrid(candidates[k]);

        if (s == null)
        {
            int bestFb = 0;
            float bestFbSc = float.NegativeInfinity;
            for (int k = 0; k < candidates.Count; k++)
            {
                float sc = EvaluatePlacement(parsed[k].road, parsed[k].loc, cardModel);
                if (sc > bestFbSc) { bestFbSc = sc; bestFb = k; }
            }
            return Task.FromResult(parsed[bestFb]);
        }

        Encode(s, xBuf);
        ForwardTrunk(xBuf);

        int handIdx = -1;
        var hand = s.HandCards;
        for (int k = 0; k < hand.Count && k < MaxHand; k++)
            if (ReferenceEquals(hand[k], cardModel)) { handIdx = k; break; }

        float[] netScores = handIdx >= 0
            ? ScoreTargets(xBuf, handIdx, candidates)
            : new float[candidates.Count];

        var total = new float[candidates.Count];
        for (int k = 0; k < candidates.Count; k++)
        {
            float net = netScores[k];
            float bonus = 0.1f * GetActionBonus(cardModel, candidates[k]);
            float place = EvaluatePlacement(parsed[k].road, parsed[k].loc, cardModel);
            total[k] = 0.5f * net + bonus + place;
        }

        int best = ArgMax(total);
        return Task.FromResult(parsed[best]);
    }
    private struct Candidate
	{
		public CardModel Card;
		public ITarget Target;
		public float Score;
		public int Cost;
	}

	public async Task Tick()
	{
		if (_tickRunning) return;
		_tickRunning = true;
		PhraseManager.PushProcessing();
		try
		{
			if (!InitDone) Init();
			var s = Status;

			int guard = 0;
			while (guard++ < 20)
			{
				var candidates = BuildCandidates(s);
				if (candidates.Count == 0) break;

				candidates.Sort((a, b) =>
					(b.Score / Math.Max(1, b.Cost)).CompareTo(a.Score / Math.Max(1, a.Cost)));

				Candidate pick = candidates[0];
				int beforeCount = s.HandCards.Count;

				if (TrainingMode)
				{
					Encode(s, xBuf);
					ForwardTrunk(xBuf);
					ComputeHandLogits(xBuf);
					var handMask = BuildHandMask();
					int handIdx = s.HandCards.ToList().IndexOf(pick.Card) + 1;
					_hasPending = true;
					Array.Copy(xBuf, _pendingState, InDim);
					Array.Copy(handMask, _pendingHandMask, HandOut);
					_pendingHandIdx = handIdx;
					_pendingTgtCount = candidates.Count;
					_pendingValue = CurrentValue();
				}

				int hp0 = s.Hero.Health.Current;
				int ehp0 = EnemyHp();
				int myU0 = CountUnits(s.Hero.Camp);
				int enU0 = CountUnits(EnemyCamp());
				int cost0 = s.Hero.Cost.Current;
				float myFv0 = FieldValue(s.Hero.Camp);
				float enFv0 = FieldValue(EnemyCamp());

				var node = NodeCard.GetNode(pick.Card);
				if (node == null || !GodotObject.IsInstanceValid(node))
				{
					if (TrainingMode) _hasPending = false;
					break;
				}

				await node.PlayMe(PlayContext.Play(pick.Target, TargetType.Grids, RoadTargetKind.Grid));

				if (TrainingMode)
				{
					float r = ShapeReward(
						hp0, s.Hero.Health.Current,
						ehp0, EnemyHp(),
						myU0, CountUnits(s.Hero.Camp),
						enU0, CountUnits(EnemyCamp()),
						cost0 - s.Hero.Cost.Current,
						pick.Card);
					r += (FieldValue(s.Hero.Camp) - myFv0) * 0.1f;
					r += (enFv0 - FieldValue(EnemyCamp())) * 0.15f;
					UpdateStep(r, false);
					_stepsSinceSave++;
					if (_stepsSinceSave >= SaveEverySteps)
					{
						_stepsSinceSave = 0;
						SaveWeights();
					}
				}

				if (s.HandCards.Count >= beforeCount) break;
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message + ex.StackTrace);
		}
		finally
		{
			_tickRunning = false;
			await PhraseManager.NextPhrase();
			PhraseManager.PopProcessing();
		}
	}

    private List<Candidate> BuildCandidates(PlayerBox s)
    {
        var list = new List<Candidate>();
        Encode(s, xBuf);
        ForwardTrunk(xBuf);

        int n = Math.Min(s.HandCards.Count, MaxHand);
        for (int k = 0; k < n; k++)
        {
            var card = s.HandCards[k];
            if (!card.CanPlay()) continue;
            if (card.Cost.Current > s.Hero.Cost.Current) continue;

            var targets = CardCmd.GetTargets(card, card.TargetType, false).ToList();
            if (targets.Count == 0) continue;

            var scores = ScoreTargets(xBuf, k, targets);
            for (int t = 0; t < targets.Count; t++)
            {
                scores[t] += 0.1f * GetActionBonus(card, targets[t]);

                if (card.TargetType == TargetType.Grids && targets[t].CanBeRoad(out var r))
                {
                    var (_, loc) = ParseGrid(targets[t]);
                    scores[t] += EvaluatePlacement(r, loc, card);
                }
            }

            int best = ArgMax(scores);
            list.Add(new Candidate
            {
                Card = card,
                Target = targets[best],
                Score = scores[best],
                Cost = card.Cost.Current
            });
        }
        return list;
    }

    private void UpdateStep(float reward, bool terminal)
	{
		if (!_hasPending) return;

		Array.Copy(_pendingState, xBuf, InDim);
		ForwardTrunk(xBuf);
		ComputeHandLogits(xBuf);

		float nextV = terminal ? 0f : CurrentValue();
		float target = reward + Gamma * nextV;
		float dV = _pendingValue - target;
		float adv = -dV;

		for (int i = 0; i < HidDim; i++) gWV[i] += ValueCoef * dV * h2[i];
		gBV[0] += ValueCoef * dV;

		var handP = SoftmaxMasked(logH, _pendingHandMask);
		if (_pendingHandIdx > 0)
		{
			for (int i = 0; i < HandOut; i++)
			{
				float d = -adv * ((i == _pendingHandIdx ? 1f : 0f) - handP[i]);
				if (d == 0f) continue;
				if (i == 0)
				{
					for (int j = 0; j < HidDim; j++) gWV[j] += d * h2[j];
					gBH[0] += d;
				}
				else
				{
					int featBase = (i - 1) * HandFeat;
					int wBase = i * (HidDim + HandFeat);
					for (int j = 0; j < HidDim; j++) gWH[wBase + j] += d * h2[j];
					for (int j = 0; j < HandFeat; j++) gWH[wBase + HidDim + j] += d * xBuf[featBase + j];
					gBH[i] += d;
				}
			}
		}
		else
		{
			for (int i = 0; i < HandOut; i++)
			{
				float d = -adv * ((i == 0 ? 1f : 0f) - handP[i]);
				if (d == 0f) continue;
				if (i == 0)
				{
					for (int j = 0; j < HidDim; j++) gWV[j] += d * h2[j];
					gBH[0] += d;
				}
				else
				{
					int featBase = (i - 1) * HandFeat;
					int wBase = i * (HidDim + HandFeat);
					for (int j = 0; j < HidDim; j++) gWH[wBase + j] += d * h2[j];
					for (int j = 0; j < HandFeat; j++) gWH[wBase + HidDim + j] += d * xBuf[featBase + j];
					gBH[i] += d;
				}
			}
		}

		AdamStep();
		ZeroGrad();
		_hasPending = false;
	}

	private void Encode(PlayerBox s, float[] v)
	{
		Array.Clear(v, 0, v.Length);
		int i = 0;
		var myCamp = s.Hero.Camp;

		var hand = s.HandCards;
		for (int k = 0; k < MaxHand; k++)
		{
			if (k < hand.Count)
			{
				var c = hand[k];
				v[i + 0] = 1f;
				v[i + 1] = c.Cost.Current / 10f;
				v[i + 2] = c.CanPlay() ? 1f : 0f;
				v[i + 3] = c.Cost.Current <= s.Hero.Cost.Current ? 1f : 0f;
				v[i + 4] = c.Camp == Camp.Zombie ? 1f : 0f;
				v[i + 5] = c.CardType.HasFlag(CardType.Fighter) ? 1f : 0f;
				v[i + 6] = c is FighterCardModel ? 1f : 0f;
				if (c is FighterCardModel f)
				{
					v[i + 7] = f.Atk.Current / 10f;
					v[i + 8] = f.Hp.Current / 10f;
				}
				v[i + 9] = StableId01(c);
			}
			i += HandFeat;
		}

		var fighters = TargetRegistry.All.OfType<Fighter>()
			.Where(f => f != null && f.Model != null)
			.OrderBy(f => f.Road.Index)
			.ThenBy(f => f.Model.Location)
			.Take(MaxFighters)
			.ToList();

		for (int k = 0; k < MaxFighters; k++)
		{
			if (k < fighters.Count)
			{
				var f = fighters[k];
				var m = f.Model;
				v[i + 0] = 1f;
				v[i + 1] = m.Hp.Current / 20f;
				v[i + 2] = m.Atk.Current / 10f;
				v[i + 3] = m.Camp == Camp.Zombie ? 1f : 0f;
				v[i + 4] = f.Road.Index / 5f;
				v[i + 5] = (int)m.Location / 2f;
				v[i + 6] = f.IsHiding ? 1f : 0f;
				v[i + 7] = m.CardTag.HasFlag(CardTag.Amphibious) ? 1f : 0f;
				v[i + 8] = m.CardTag.HasFlag(CardTag.Coop) ? 1f : 0f;
				v[i + 9] = m.IsDie ? 1f : 0f;
				v[i + 10] = (int)f.Road.Type / 2f;
				v[i + 11] = m.Camp == myCamp ? 1f : 0f;
			}
			i += FighterFeat;
		}

		var enemy = PlayerManager.GetPlayer(myCamp == Camp.Plant ? Camp.Zombie : Camp.Plant);
		var eBox = enemy?.Status;
		v[i++] = s.Hero.Health.Current / 30f;
		v[i++] = s.Hero.Cost.Current / 10f;
		v[i++] = s.Hero.Shield.Current / 8f;
		v[i++] = (eBox?.Hero.Health.Current ?? 0) / 30f;
		v[i++] = (eBox?.Hero.Cost.Current ?? 0) / 10f;
		v[i++] = (eBox?.Hero.Shield.Current ?? 0) / 8f;

		for (int p = 0; p < 5; p++) v[i++] = (int)PhraseManager.Phrase == p ? 1f : 0f;

		v[i++] = s.HandCards.Count / 10f;
		v[i++] = (eBox?.HandCards.Count ?? 0) / 10f;

		int myUnits = 0, enemyUnits = 0, myAtk = 0, enemyAtk = 0;
		float myFieldValue = 0f, enemyFieldValue = 0f;
		foreach (var f in fighters)
		{
			if (f.Model == null || f.Model.IsDie) continue;
			var m = f.Model;
			float fv = m.Atk.Current * 1.0f
					 + m.Hp.Current * 0.8f
					 + (f.IsHiding ? 0f : 0.3f)
					 + (m.CardTag.HasFlag(CardTag.Amphibious) ? 0.2f : 0f)
					 + (m.CardTag.HasFlag(CardTag.Coop) ? 0.2f : 0f);

			if (m.Camp == myCamp) { myUnits++; myAtk += m.Atk.Current; myFieldValue += fv; }
			else { enemyUnits++; enemyAtk += m.Atk.Current; enemyFieldValue += fv; }
		}
		v[i++] = myUnits / 12f;
		v[i++] = enemyUnits / 12f;
		v[i++] = myAtk / 30f;
		v[i++] = enemyAtk / 30f;
		v[i++] = myFieldValue / 40f;
		v[i++] = enemyFieldValue / 40f;

		v[i++] = IsMyTurn(myCamp) ? 1f : 0f;
		v[i++] = PhraseManager.Phrase == Phrases.Phrase.Fight ? 1f : 0f;
		v[i++] = CardCmd.StopCardPlay ? 1f : 0f;
		v[i++] = PhraseManager.Turn / 20f;
		i += 4;
	}

	private static bool IsMyTurn(Camp myCamp)
	{
		var p = PhraseManager.CurrentTurnPlayer;
		return p != null && p.Status.Hero.Camp == myCamp;
	}

	private static float StableId01(CardModel c)
	{
		if (c == null) return 0f;
		unchecked
		{
			uint h = c.Id;
			h *= 2654435761u;
			return (h % 1000u) / 1000f;
		}
	}

	private static void ForwardTrunk(float[] x)
	{
		MatMul(x, W1, h1, 1, InDim, HidDim);
		for (int i = 0; i < HidDim; i++) h1[i] = h1[i] + B1[i] > 0 ? h1[i] + B1[i] : 0f;

		MatMul(h1, W2, h2, 1, HidDim, HidDim);
		for (int i = 0; i < HidDim; i++) h2[i] = h2[i] + B2[i] > 0 ? h2[i] + B2[i] : 0f;
	}

	private static void ComputeHandLogits(float[] x)
	{
		int handIn = HidDim + HandFeat;
		for (int o = 0; o < HandOut; o++)
		{
			if (o == 0)
			{
				float sc = BH[0];
				for (int i = 0; i < HidDim; i++) sc += WV[i] * h2[i];
				logH[0] = sc;
				continue;
			}

			int featBase = (o - 1) * HandFeat;
			for (int i = 0; i < HidDim; i++) tmpFeat[i] = h2[i];
			for (int i = 0; i < HandFeat; i++) tmpFeat[HidDim + i] = x[featBase + i];

			float score = BH[o];
			int wBase = o * handIn;
			for (int i = 0; i < handIn; i++) score += WH[wBase + i] * tmpFeat[i];
			logH[o] = score;
		}
	}

	private float[] BuildHandMask()
	{
		var m = new float[HandOut];
		m[0] = 1f;
		var s = Status;
		int n = Math.Min(s.HandCards.Count, MaxHand);
		for (int k = 0; k < n; k++)
		{
			var c = s.HandCards[k];
			m[k + 1] = (c.CanPlay() && c.Cost.Current <= s.Hero.Cost.Current) ? 1f : 0f;
		}
		return m;
	}

	private float[] ScoreTargets(float[] x, int handIdx, List<ITarget> targets)
	{
		int featBase = handIdx * HandFeat;
		for (int i = 0; i < HidDim; i++) tmpFeat[i] = h2[i];
		for (int i = 0; i < HandFeat; i++) tmpFeat[HidDim + i] = x[featBase + i];

		int handIn = HidDim + HandFeat;
		for (int o = 0; o < HidDim; o++)
		{
			float sc = BtHand[o];
			int b = o * handIn;
			for (int i = 0; i < handIn; i++) sc += WtHand[b + i] * tmpFeat[i];
			handEmbBuf[o] = sc > 0 ? sc : 0f;
		}

		var myCamp = Status.Hero.Camp;
		var scores = new float[targets.Count];
		int tgtIn = HidDim + TgtFeat;
		for (int j = 0; j < targets.Count; j++)
		{
			var tf = EncodeTarget(targets[j], myCamp);
			for (int i = 0; i < HidDim; i++) tmpFeat[i] = h2[i];
			for (int i = 0; i < TgtFeat; i++) tmpFeat[HidDim + i] = tf[i];

			float dot = 0f;
			for (int o = 0; o < HidDim; o++)
			{
				float sc = BtTgt[o];
				int b = o * tgtIn;
				for (int i = 0; i < tgtIn; i++) sc += WtTgt[b + i] * tmpFeat[i];
				float te = sc > 0 ? sc : 0f;
				dot += handEmbBuf[o] * te;
			}
			scores[j] = dot / HidDim;
		}
		return scores;
	}

	private static float[] EncodeTarget(ITarget t, Camp myCamp)
	{
		var v = new float[TgtFeat];
		if (t is Fighter f && f.Model != null)
		{
			var m = f.Model;
			v[0] = 1f;
			v[1] = m.Hp.Current / 20f;
			v[2] = m.Atk.Current / 10f;
			v[3] = m.Camp == myCamp ? 1f : 0f;
			v[4] = f.Road != null ? f.Road.Index / 5f : 0f;
			v[5] = (int)m.Location / 2f;
			v[6] = f.IsHiding ? 1f : 0f;
			v[7] = 0f;
		}
		else if (t is HeroInstance h)
		{
			v[0] = 0f;
			v[1] = h.Hero.Health.Current / 30f;
			v[3] = h.Hero.Camp == myCamp ? 1f : 0f;
			v[7] = 1f;
		}
        else if (t.CanBeRoad(out var road))
        {
            var (_, loc) = ParseGrid(t);

            v[4] = road.Index / 5f;
            v[5] = (int)road.Type / 2f;
            v[7] = 0.5f;

            v[6] = loc switch
            {
                Location.Plant => 1f,
                Location.Zombie => -1f,
                Location.PlantFront => 0.5f,
                _ => 0f
            };

            var roadUnits = TargetRegistry.All.OfType<Fighter>()
                .Where(f => f.Model != null && !f.Model.IsDie
                         && f.Road != null && f.Road.Index == road.Index)
                .ToList();

            int occ = 0, friends = 0, enemies = 0;
            foreach (var ft in roadUnits)
            {
                if (loc != null && ft.Model.Location == loc) occ++;
                if (ft.Model.Camp == myCamp) friends++; else enemies++;
            }
            v[1] = occ > 0 ? 1f : 0f;
            v[2] = friends / 5f;
            v[3] = enemies / 5f;
        }
        return v;
	}

	private static int ArgMax(float[] arr)
	{
		int best = 0; float bv = float.NegativeInfinity;
		for (int i = 0; i < arr.Length; i++) if (arr[i] > bv) { bv = arr[i]; best = i; }
		return best;
	}

	private static int ArgMaxMasked(float[] arr, float[] mask)
	{
		int best = 0; float bv = float.NegativeInfinity;
		for (int i = 0; i < arr.Length; i++)
		{
			if (mask[i] <= 0f) continue;
			if (arr[i] > bv) { bv = arr[i]; best = i; }
		}
		return best;
	}

	private static void MatMul(float[] A, float[] B, float[] C, int m, int k, int n)
	{
		Array.Clear(C, 0, m * n);
		for (int i = 0; i < m; i++)
			for (int t = 0; t < k; t++)
			{
				float a = A[i * k + t];
				if (a == 0f) continue;
				int bBase = t * n;
				int cBase = i * n;
				for (int j = 0; j < n; j++)
					C[cBase + j] += a * B[bBase + j];
			}
	}

	private static int SampleMasked(float[] logits, float[] mask)
	{
		var masked = new float[logits.Length];
		float max = float.NegativeInfinity;
		for (int i = 0; i < logits.Length; i++)
		{
			masked[i] = mask[i] > 0 ? logits[i] : -1e9f;
			if (masked[i] > max) max = masked[i];
		}
		float sum = 0f;
		var p = new float[logits.Length];
		for (int i = 0; i < logits.Length; i++)
		{
			p[i] = MathF.Exp(masked[i] - max);
			sum += p[i];
		}
		for (int i = 0; i < p.Length; i++) p[i] /= sum;

		float r = (float)Rng.NextDouble();
		float acc = 0f;
		for (int i = 0; i < p.Length; i++)
		{
			acc += p[i];
			if (r <= acc) return i;
		}
		return ArgMaxMasked(logits, mask);
	}

	private static float CurrentValue()
	{
		float v = BV[0];
		for (int i = 0; i < HidDim; i++) v += WV[i] * h2[i];
		return v;
	}

	private Camp EnemyCamp()
	{
		var my = Status.Hero.Camp;
		return my == Camp.Plant ? Camp.Zombie : Camp.Plant;
	}

	private int EnemyHp()
	{
		var e = PlayerManager.GetPlayer(EnemyCamp());
		return e?.Status.Hero.Health.Current ?? 0;
	}

	private static int CountUnits(Camp camp)
		=> TargetRegistry.All.OfType<Fighter>().Count(f => f.Model != null && f.Model.Camp == camp && !f.IsHiding);

	private static float FieldValue(Camp camp)
	{
		float sum = 0f;
		foreach (var f in TargetRegistry.All.OfType<Fighter>())
		{
			if (f.Model == null || f.Model.IsDie || f.Model.Camp != camp || f.IsHiding) continue;
			var m = f.Model;
			sum += m.Atk.Current * 1.0f
				 + m.Hp.Current * 0.8f
				 + 0.3f
				 + (m.CardTag.HasFlag(CardTag.Amphibious) ? 0.2f : 0f)
				 + (m.CardTag.HasFlag(CardTag.Coop) ? 0.2f : 0f);
		}
		return sum;
	}

	private float ShapeReward(int hp0, int hp1, int ehp0, int ehp1,
							  int myU0, int myU1, int enU0, int enU1,
							  int costSpent, CardModel card)
	{
		float r = 0f;
		r += (hp0 - hp1) * -0.5f;
		r += (ehp0 - ehp1) * 0.5f;
		r += (myU1 - myU0) * 0.2f;
		r += (enU0 - enU1) * 0.3f;
		if (costSpent > 0) r += 0.02f * costSpent;
		if (card is FighterCardModel) r += 0.1f;
		r += 0.1f * GetActionBonus(card, null);
		return r;
	}

	private static float[] SoftmaxMasked(float[] logits, float[] mask)
	{
		int n = logits.Length;
		var p = new float[n];
		float max = float.NegativeInfinity;
		for (int i = 0; i < n; i++) if (mask[i] > 0 && logits[i] > max) max = logits[i];
		if (float.IsNegativeInfinity(max)) { p[0] = 1f; return p; }
		float sum = 0f;
		for (int i = 0; i < n; i++)
		{
			if (mask[i] <= 0) { p[i] = 0f; continue; }
			p[i] = MathF.Exp(logits[i] - max);
			sum += p[i];
		}
		for (int i = 0; i < n; i++) p[i] /= sum;
		return p;
	}

	private static void AdamStep()
	{
		adamT++;
		float bc1 = 1 - MathF.Pow(Beta1, adamT);
		float bc2 = 1 - MathF.Pow(Beta2, adamT);
		float step = Lr * MathF.Sqrt(bc2) / bc1;

		AdamApply(W1, gW1, mW1, vW1, step);
		AdamApply(B1, gB1, mB1, vB1, step);
		AdamApply(W2, gW2, mW2, vW2, step);
		AdamApply(B2, gB2, mB2, vB2, step);
		AdamApply(WH, gWH, mWH, vWH, step);
		AdamApply(BH, gBH, mBH, vBH, step);
		AdamApply(WtHand, gWtHand, mWtHand, vWtHand, step);
		AdamApply(BtHand, gBtHand, mBtHand, vBtHand, step);
		AdamApply(WtTgt, gWtTgt, mWtTgt, vWtTgt, step);
		AdamApply(BtTgt, gBtTgt, mBtTgt, vBtTgt, step);
		AdamApply(WV, gWV, mWV, vWV, step);
		AdamApply(BV, gBV, mBV, vBV, step);
	}

	private static void AdamApply(float[] w, float[] g, float[] m, float[] v, float step)
	{
		for (int i = 0; i < w.Length; i++)
		{
			m[i] = Beta1 * m[i] + (1 - Beta1) * g[i];
			v[i] = Beta2 * v[i] + (1 - Beta2) * g[i] * g[i];
			w[i] -= step * m[i] / (MathF.Sqrt(v[i]) + Eps);
		}
	}

	private static void ZeroGrad()
	{
		Array.Clear(gW1); Array.Clear(gB1);
		Array.Clear(gW2); Array.Clear(gB2);
		Array.Clear(gWH); Array.Clear(gBH);
		Array.Clear(gWtHand); Array.Clear(gBtHand);
		Array.Clear(gWtTgt); Array.Clear(gBtTgt);
		Array.Clear(gWV); Array.Clear(gBV);
	}

	public static void SaveWeights()
	{
		try
		{
			var data = new
			{
				W1,
				B1,
				W2,
				B2,
				WH,
				BH,
				WtHand,
				BtHand,
				WtTgt,
				BtTgt,
				WV,
				BV,
				adamT
			};
			var json = JsonSerializer.Serialize(data);
			string path = OS.HasFeature("editor") ? WeightPath : "user://ai_weights.json";
			using var f = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
			f.StoreString(json);
		}
		catch (Exception ex) { Log.Error(ex.Message + ex.StackTrace); }
	}
}
