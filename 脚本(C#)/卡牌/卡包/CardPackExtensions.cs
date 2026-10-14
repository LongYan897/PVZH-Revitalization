using FileExtension;
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;

namespace Card.Pack;

/// <summary>
/// CardPack 的扩展方法，提供基于可控种子的伪随机抽取与洗牌。
/// 使用 System.Random 并传入 seed 保证可复现（伪随机）。
/// </summary>
public partial class CardPackExtensions
{
	public int seed { get; private set; }
	private string _path { get; } = "res://数据(C#)/cardPack.json";
	private FileJson _file { get; set; } = new();
	private Random _random { get; set; }
	public CardPackExtensions(int seed)
	{
		this.seed = seed;
		_random = new(seed);
	}
	public List<(string Card,int Count)> DrawCard(string packName, int Count,bool save = false)
	{
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
			if (!i.ContainsKey("BonusRate")) throw new FormatException("文件没有BonusRate");

			int remainCount = 0;
			int maxCount = i["MaxCount"]?.GetValue<int>() 
						   ?? throw new FormatException("MaxCount 缺失或不是整数");
			bool hasMax = maxCount != 0;
			if (i["Cards"] is JsonArray cards && hasMax)
			{
				foreach (JsonNode? card in cards)
				{
					string cardName = card?.GetValue<string>();
					remainCount += Math.Max(maxCount - playerAsset.GetVariable(cardName, 0),0);
				}
			}
			packRemains.Add((i["Name"].GetValue<string>(),hasMax ? remainCount : -1));
		}

		//计算每个组的基础概率
		List<(string Name, double Rate)> BasePackRates = new();
		for (int index = 0; index < packs.Count; index++)
		{
			var cardName = packRemains[index].Name;
			double baseRate = packs[index]["BaseRate"].GetValue<double>();
			BasePackRates.Add((cardName, 
				packRemains[index].Count == 0
				? 0
				: baseRate));
		}
		//基础概率归一化处理
		Normalize(BasePackRates);

		//double sumRate = 0;
		//foreach (var i in BasePackRates)
		//{
		//	sumRate += i.Rate;
		//}
		//for (int index = 0;index < BasePackRates.Count; index++)
		//{
		//	var item = BasePackRates[index];
		//	item.Rate = item.Rate / sumRate;
		//	BasePackRates[index] = item;
		//}
        //计算每组抽取的数量
        List<(string Name,int Count)> drawCounts = new();
		foreach (var i in BasePackRates)
		{
			drawCounts.Add((i.Name,0));
		}
		for (int i = 0; i < Count; i++)
		{
			double drawRate = _random.NextDouble();
			double cumulativeRate = 0;
			int index = -1;
			foreach (var packRate in BasePackRates)
			{
				index++;
				cumulativeRate += packRate.Rate;
				if (packRate.Rate == 0) continue;
				if (drawRate < cumulativeRate)
				{
					string groupName = drawCounts[index].Name;
					drawCounts[index] = (groupName, drawCounts[index].Count + 1);
					if (packRemains[index].Count == -1) break;
					packRemains[index] = (groupName, packRemains[index].Count - 1);
					if (packRemains[index].Count == 0)
					{
						BasePackRates[index] = (groupName, 0);
						Normalize(BasePackRates);
                    }
					break;
				}
			}
		}
		//开始处理每个组抽到的具体卡牌
		var groupIndex = -1;
		foreach(var group in packs)
		{
			groupIndex++;
            List<(string Name, int Count)> drawcards = new();
			List<(int Index, int Count)> cardsCount = new();
			//初始化列表
			int index = -1;
			var maxCount = group["MaxCount"].GetValue<int>();
			foreach(var card in group["Cards"] as JsonArray)
			{
				index++;
				string cardName = card?.GetValue<string>();
                drawcards.Add((cardName, 0));
				int remainCount = maxCount == 0
					? -1
					: Math.Max(maxCount - playerAsset.GetVariable<int>(cardName, 0), 0);
                cardsCount.Add((index, remainCount));
            }
			int drawNum = drawCounts[groupIndex].Count;
			for(int i = 0; i < drawNum; i++)
			{
				int ri = _random.Next(cardsCount.Count);
                int cardIndex = cardsCount[ri].Index;
				string cardName = drawcards[cardIndex].Name;
				drawcards[cardIndex] = (cardName, drawcards[cardIndex].Count + 1);
				if (cardsCount[ri].Count == -1) continue;
				cardsCount[ri] = (cardsCount[ri].Index, cardsCount[ri].Count - 1);
				if(cardsCount[ri].Count == 0) cardsCount.RemoveAt(ri);
            }
			//调试信息
			foreach(var i in drawcards)
			{
				//GD.Print($"CardName:{i.Name.PadRight(15)}\t Count:{i.Count}");
			}
            //修改实际卡牌数量
            System.Collections.Generic.Dictionary<string, int> fileCardsCount= playerAsset.GetEntries<int>();
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
	private static List<(string Name,double Rate)>Normalize( List<(string Name, double Rate)> list)
	{
		double sumRate = 0;
		foreach(var i in list)
		{
			sumRate += i.Rate;
		}
		if (sumRate <= 0) throw new InvalidOperationException("概率为0");
        for (int i = 0;i< list.Count; i++)
		{
			list[i] = (list[i].Name, list[i].Rate / sumRate);
		}
		return list;
	}
}
