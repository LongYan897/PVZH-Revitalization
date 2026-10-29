using FileExtension;
using Godot;
using Play;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Card.Pack;

/// <summary>
/// CardPack 的扩展方法，提供基于可控种子的伪随机抽取与洗牌。
/// 使用 System.Random 并传入 seed 保证可复现（伪随机）。
/// </summary>
public partial class CardPackExtensions
{
	public int seed { get; private set; }
	//private string _path { get; } = "res://数据(C#)/cardPack.json";
	//private FileJson _file { get; set; } = new();
	private Random _random { get; set; }
	public CardPackExtensions(int seed = -1)
	{
		this.seed = seed;
		if (seed == -1)
		{
			_random = new();
		}
		else _random = new(seed);
	}
	public List<(string Card, int Count)> DrawCard(string packName, int Count,PlayerAsset player = null, bool save = false)
	{
		player ??= new();
		List<(string Card, int Count)> res = new();
		FileJson file = new();
		FileJson playerAssetFile = new();

		var playerAsset = playerAssetFile.Open("res://数据(C#)/playerAsset.json")
			.OpenObject("CardCounts");

		file.Open("res://数据(C#)/cardPack.json");
		var packs = file.OpenObject(packName).GetArray<JsonObject>("group");
		List<(string Name, int Count)> packRemains = new();
		//计算每个组可抽取的数量
		foreach (var i in packs)
		{
			if (!i.ContainsKey("Name")) throw new FormatException("文件没有Name");
			if (!i.ContainsKey("Cards")) throw new FormatException("文件没有Cards");
			if (!i.ContainsKey("MaxCount")) throw new FormatException("文件没有MaxCount");
			if (!i.ContainsKey("BaseRate")) throw new FormatException("文件没有BaseRate");
			if (!i.ContainsKey("UltimateRate")) throw new FormatException("文件没有UltimateRate");

			int remainCount = 0;
			int maxCount = JsonInt(i["MaxCount"]);
			bool hasMax = maxCount != 0;
			if (i["Cards"] is JsonArray cards && hasMax)
			{
				foreach (JsonNode card in cards)
				{
					string cardName = card?.GetValue<string>();
					remainCount += Math.Max(maxCount - playerAsset.GetVariable(cardName, 0), 0);
				}
			}
			packRemains.Add((i["Name"].GetValue<string>(), hasMax ? remainCount : -1));
		}

		//计算每个组的基础概率
		List<(string Name, double Rate,double UtRate)> BasePackRates = new();
		for (int index = 0; index < packs.Count; index++)
		{
			var cardName = packRemains[index].Name;
			double baseRate = JsonDouble(packs[index]["BaseRate"]);
			double utRate = JsonDouble(packs[index]["UltimateRate"]);
			BasePackRates.Add((cardName,
				packRemains[index].Count == 0
				? 0
				: baseRate,
				packRemains[index].Count == 0
				? 0
				: utRate));
		}
		//基础概率归一化处理
		Normalize(BasePackRates);
		//初始化实际概率
		List<double> RealRates = new();
		foreach(var i in BasePackRates)
		{
			RealRates.Add(i.Rate * (1 - player.Lucky.Current) + i.UtRate * player.Lucky.Current);
		}
		//计算每组抽取的数量
		List<(string Name, int Count)> drawCounts = new();
		foreach (var i in BasePackRates)
		{
			drawCounts.Add((i.Name, 0));
		}
		for (int i = 0; i < Count; i++)
		{
			double drawRate = _random.NextDouble();
			double cumulativeRate = 0;
			int index = -1;
			foreach (var packRate in BasePackRates)
			{
				index++;
				cumulativeRate += RealRates[index];
				if (RealRates[index] == 0) continue;
				if (drawRate < cumulativeRate)
				{
					string groupName = drawCounts[index].Name;
					drawCounts[index] = (groupName, drawCounts[index].Count + 1);
					if (packRemains[index].Count == -1) break;
					packRemains[index] = (groupName, packRemains[index].Count - 1);
					//检测到组内可抽取的数量为0，重新归一化概率。
					if (packRemains[index].Count == 0)
					{
						BasePackRates[index] = (groupName, 0, 0);
						Normalize(BasePackRates);
						setRealRates(BasePackRates, RealRates, player);
					}
					break;
				}
			}
		}
		//开始处理每个组抽到的具体卡牌
		var groupIndex = -1;
		foreach (var group in packs)
		{
			groupIndex++;
			List<(string Name, int Count)> drawcards = new();
			List<(int Index, int Count)> cardsCount = new();
			//初始化列表
			int index = -1;
			var maxCount = JsonInt(group["MaxCount"]);
			foreach (var card in group["Cards"] as JsonArray)
			{
				index++;
				string cardName = card?.GetValue<string>();
				drawcards.Add((cardName, 0));
				int remainCount = maxCount == 0
					? -1
					: Math.Max(maxCount - playerAsset.GetVariable<int>(cardName, 0), 0);
				if (remainCount != 0)
				{
					cardsCount.Add((index, remainCount));
				}
			}
			int drawNum = drawCounts[groupIndex].Count;
			for (int i = 0; i < drawNum; i++)
			{
				int ri = _random.Next(cardsCount.Count);
				int cardIndex = cardsCount[ri].Index;
				string cardName = drawcards[cardIndex].Name;
				drawcards[cardIndex] = (cardName, drawcards[cardIndex].Count + 1);
				if (cardsCount[ri].Count == -1) continue;
				cardsCount[ri] = (cardsCount[ri].Index, cardsCount[ri].Count - 1);
				if (cardsCount[ri].Count == 0) cardsCount.RemoveAt(ri);
			}
			//调试信息
			foreach (var i in drawcards)
			{
				//GD.Print($"CardName:{i.Name.PadRight(15)}\t Count:{i.Count}");
			}
			//修改实际卡牌数量
			System.Collections.Generic.Dictionary<string, int> fileCardsCount = playerAsset.GetEntries<int>();
			foreach (var i in drawcards)
			{
				if (i.Count == 0) continue;
				string cardName = i.Name;
				int count = 0;
				fileCardsCount.TryGetValue(cardName, out count);
				fileCardsCount[cardName] = count + i.Count;
				//返回结果
				int resultIndex = res.FindIndex(item => item.Card == cardName);
				if (resultIndex >= 0)
				{
					var item = res[resultIndex];
					res[resultIndex] = (item.Card, item.Count + i.Count);
				}
				else
				{
					res.Add((cardName, i.Count));
				}
			}
			playerAsset.SetEntries(fileCardsCount);
			if (save) playerAsset.Save();
		}

		//打印调试信息
		foreach (var i in packRemains)
		{
			GD.Print(i.Name + "可抽取数量:" + i.Count);
		}
		foreach (var i in BasePackRates)
		{
			GD.Print(i.Name + "基础概率:" + i.Rate);
		}
		foreach (var i in drawCounts)
		{
			GD.Print($"{i.Name}抽取数量: {i.Count}");
		}
		//file.SetVariable("group", packs);
		//file.Save();
		return res;
	}
	/// <summary>
	/// 将固定元组类型里的概率自动归一化处理
	/// </summary>
	/// <param name="list"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	private static List<(string Name, double Rate,double UtRate)> Normalize(List<(string Name, double Rate,double UtRate)> list)
	{
		double sumRate = 0;
		double sumutRate = 0;
		foreach (var i in list)
		{
			sumRate += i.Rate;
			sumutRate += i.UtRate;
		}
		if (sumRate <= 0 && sumutRate <= 0)
			throw new InvalidOperationException("基础概率和幸运概率均为0，没有可抽取的组");

		for (int i = 0; i < list.Count; i++)
		{
			double rate = sumRate > 0
				? list[i].Rate / sumRate
				: list[i].UtRate / sumutRate;
			double utRate = sumutRate > 0
				? list[i].UtRate / sumutRate
				: rate;
			list[i] = (list[i].Name, rate, utRate);
		}
		return list;
	}
	private static List<double> setRealRates(List<(string Name, double Rate, double UtRate)> baseList, List<double> reallist, PlayerAsset player)
	{
		int index = -1;
		foreach (var i in baseList)
		{
			index++;
			reallist[index] = (i.Rate * (1 - player.Lucky.Current) + i.UtRate * player.Lucky.Current);
		}
		return reallist;
	}

	/// <summary>
	/// 读数字字段：兼容 JSON 里写成数字或字符串（比如 UltimateRate 被写成 "0" 或 "0.70"）。
	/// 直接 GetValue&lt;double&gt;() 时，只要数据里是字符串就会抛
	/// "An element of type 'String' cannot be converted to a 'System.Double'"。
	/// </summary>
	private static double JsonDouble(JsonNode node)
	{
		if (node is not JsonValue value) return 0;
		if (value.TryGetValue<double>(out var d)) return d;
		return double.TryParse(value.ToString(), System.Globalization.NumberStyles.Float,
			System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
	}

	/// <summary>读整数字段，同样兼容字符串写法</summary>
	private static int JsonInt(JsonNode node)
	{
		if (node is not JsonValue value) return 0;
		if (value.TryGetValue<int>(out var i)) return i;
		return double.TryParse(value.ToString(), System.Globalization.NumberStyles.Float,
			System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? (int)parsed : 0;
	}
}
