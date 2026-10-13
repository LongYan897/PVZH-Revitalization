using FileExtension;
using Godot;
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
	private Random _random { get; set; } = new();
	public CardPackExtensions(int seed)  => this.seed = seed;
	public List<CardPackEntry> DrawCard(string packName, int Count)
	{
		List<CardPackEntry> res = new();
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
		double sumRate = 0;
		foreach (var i in BasePackRates)
		{
			sumRate += i.Rate;
		}
		for (int index = 0;index < BasePackRates.Count; index++)
		{
			var item = BasePackRates[index];
			item.Rate = item.Rate / sumRate;
			BasePackRates[index] = item;
		}
        //List<(string Name,double Rate)>Normalize(ref )
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

                    }
					break;
				}
			}
		}

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
		file.SetVariable("group", packs);
		file.Save();
		return res;
	}
}
