using Card;
using Godot;
using Godot.Collections;
using Play;
using System;
using System.Collections.Generic;

namespace Quest.String;

/// <summary>
/// 专门为任务文本制作的类<br/>
/// 可以自动从 Json 文本中提取任务基础信息，统一使用 "任务名.对应的键" (这里的任务名称指的是"任务对应类的名称"):<br/>
///     名称    : 使用 "Title" 当作 Json 的键<br/>
///     描述    : 使用 "Description" 或 "Des" 当作 Json 的键<br/>
///     特别描述 : 使用 "Flavor" 当作 Json 的键<br/>
///     奖励描述 : 使用 "Reward" 当作 Json 的键(仅用于 UI 显示的文本)<br/>
///     奖励列表 : 使用 "Rewards" 当作 Json 的键(结构化奖励，用 <see cref="Reward"/> 表示)，值是列表，每个元素包含:<br/>
///         Type   : 奖励类型，字符串，取值 Gem / Spark / Ticket / Card / Hero<br/>
///         Amount : 数量，整数，Card 为张数，其余为数值，缺省 1<br/>
///         Title  : 卡牌或英雄的中文名称，Card / Hero 时必填<br/>
///     序列化代码 : 使用 "Id" 当作 Json 的键<br/>
///     阵营 : 使用 "Camp" 当作 Json 的键 如果值是非 "植物/plant/Plant" 或 "僵尸/zombie/Zombie" 默认是无阵营任务<br/>
///     类别 : 使用 "Type" 当作 Json 的键 可以是字符串或数组，取值 Normal / Daily / Event / Story / Tutorial(支持中英文)<br/>
///     <br/>
///     注意：本类只负责任务元数据；任务目标(Condition)完全由 Quest 子类自己构造，JSON 不参与。<br/>
///     <br/>
///     ***<br/>
///         描述(Description/Des)、特别描述(Flavor)、奖励描述(Reward) 共用同一套关键字系统：<br/>
///         文本中使用 _XXX_ 可以实现原版下划线功能 例如召唤<br/>
///         文本中使用 =XXX= 可以实现图标显示 例如致命 必中<br/>
///         文本中使用 =XXX={N} 可以实现带数字的图标显示 例如 =生命值={2} 显示 获得+2(生命图标)<br/>
///         文本中使用 $XXX$ 可以实现标签(绿色下划线)<br/>
///         文本中使用 _#ABC#XXX_ 或者 _&amp;ABC&amp;XXX_ 可以实现修改参数对应区域注册的 {#/&amp;:n} 例如 注册了 融合 => "在这个单位上打出{#/&amp;:n}来使用此能力。" 然后写 _&amp;僵尸&amp;融合_ 显示 (url)僵尸融合 点击后显示 在这个单位上打出僵尸来使用此能力。<br/>
///         文本中使用 _XXX#ABC#_ 或者 _XXX&amp;ABC&amp;_ 可以实现修改参数对应区域注册的 {#/&amp;:x} 例如 注册了 融合 => "在这个单位上打出{#/&amp;:x}来使用此能力。" 然后写 _融合&amp;2&amp;_ 显示 (url)融合2 点击后显示 在这个单位上打出2来使用此能力。<br/>
///         {#/&amp;:n}语法中 #ABC# 在描述中会实现 $ABC$ 的效果 而 &amp;ABC&amp; 在描述中直接显示 ABC<br/>
///         文本中使用 ==XXX== 或 ==XXX{_#ABC#YYY_}== 可以实现带 url 的图标显示 点击触发 { } 内链接提取出的关键字/参数<br/>
///     ***<br/>
///     <br/>
/// 例如:<br/>
///     造成伤害并击杀单位 DamageAndKillQuest<br/>
///     ***Json***<br/>
///     {<br/>
///         "DamageAndKillQuest.Id": 2,<br/>
///         "DamageAndKillQuest.Title": "造成伤害并击杀单位",<br/>
///         "DamageAndKillQuest.Des": "造成 =伤害={N} 点伤害，并击杀 =击杀={N} 个单位。",<br/>
///         "DamageAndKillQuest.Reward": "获得 =金币={N} 宝石与一张豌豆射手。",<br/>
///         "DamageAndKillQuest.Rewards": [<br/>
///             { "Type": "Gem",    "Amount": 10 },<br/>
///             { "Type": "Spark",  "Amount": 100 },<br/>
///             { "Type": "Ticket", "Amount": 1 },<br/>
///             { "Type": "Card",   "Amount": 1, "Title": "豌豆射手" },<br/>
///             { "Type": "Hero",   "Title": "绿影侠" }<br/>
///         ],<br/>
///         "DamageAndKillQuest.Camp": "Plant",<br/>
///         "DamageAndKillQuest.Type": "Story",<br/>
///         "DamageAndKillQuest.Flavor": "先打疼它，再谈别的。"<br/>
///     }<br/>
///     *********<br/>
/// </summary>
public class QuestString
{
    /// <summary>
    /// 序列化代码
    /// </summary>
    public uint Id { get; private set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// 特别描述
    /// </summary>
    public string Flavor { get; private set; }

    /// <summary>
    /// 奖励描述(仅用于 UI 显示的文本)
    /// </summary>
    public string RewardDes { get; private set; }

    /// <summary>
    /// 结构化奖励列表(用于实际发放)
    /// </summary>
    public IReadOnlyList<Reward> Rewards { get; private set; } = new List<Reward>();

    /// <summary>
    /// 阵营(null 表示无阵营任务)
    /// </summary>
    public Camp? Camp { get; private set; }

    /// <summary>
    /// 任务类别
    /// </summary>
    public QuestType Type { get; private set; } = QuestType.None;

    /// <summary>
    /// 全局的字典用于查找数据
    /// </summary>
    private static Dictionary _questDict;

    /// <summary>
    /// 从 quests.json 中加载任务文本
    /// </summary>
    /// <param name="questTitle">任务类名，例如 DamageAndKillQuest</param>
    /// <returns>任务文本</returns>
    public static QuestString Loading(string questTitle)
    {
        var questString = new QuestString();

        if (GetData(questTitle, "Id").VariantType != Variant.Type.Nil)
        {
            questString.Id = GetData(questTitle, "Id").AsUInt32();
        }
        if (GetData(questTitle, "Title").VariantType != Variant.Type.Nil)
        {
            questString.Title = GetData(questTitle, "Title").AsString();
        }
        if (GetData(questTitle, "Des").VariantType != Variant.Type.Nil)
        {
            questString.Description = GetData(questTitle, "Des").AsString();
        }
        if (GetData(questTitle, "Description").VariantType != Variant.Type.Nil)
        {
            questString.Description = GetData(questTitle, "Description").AsString();
        }
        if (GetData(questTitle, "Flavor").VariantType != Variant.Type.Nil)
        {
            questString.Flavor = GetData(questTitle, "Flavor").AsString();
        }
        if (GetData(questTitle, "Reward").VariantType != Variant.Type.Nil)
        {
            questString.RewardDes = GetData(questTitle, "Reward").AsString();
        }
        if (GetData(questTitle, "Camp").VariantType != Variant.Type.Nil)
        {
            questString.Camp = ParseCamp(GetData(questTitle, "Camp").AsString());
        }
        if (GetData(questTitle, "Type").VariantType != Variant.Type.Nil)
        {
            var typeData = GetData(questTitle, "Type");
            if (typeData.VariantType == Variant.Type.Array)
            {
                foreach (var item in typeData.As<Godot.Collections.Array>())
                {
                    var parsed = ParseType(item.AsString());
                    if (parsed != QuestType.None)
                    {
                        questString.Type = parsed;
                        break;
                    }
                }
            }
            else
            {
                questString.Type = ParseType(typeData.AsString());
            }
        }
        if (GetData(questTitle, "Rewards").VariantType != Variant.Type.Nil)
        {
            var rewards = new List<Reward>();
            var data = GetData(questTitle, "Rewards");
            if (data.VariantType == Variant.Type.Array)
            {
                foreach (var item in data.As<Godot.Collections.Array>())
                {
                    var reward = ParseReward(item.As<Dictionary>());
                    if (reward != null) rewards.Add(reward);
                }
            }
            else if (data.VariantType == Variant.Type.Dictionary)
            {
                var reward = ParseReward(data.As<Dictionary>());
                if (reward != null) rewards.Add(reward);
            }
            questString.Rewards = rewards;
        }
        int rawRank = -1;
        if (GetData(questTitle, "Rank").VariantType != Variant.Type.Nil)
        {
            rawRank = GetData(questTitle, "Rank").AsInt32();
        }
        questString.Rank = ResolveRank(questString.Type, rawRank);

        return questString;
    }

    /// <summary>
    /// 按任务类别决定最终等级<br/>
    /// </summary>
    /// <param name="type">任务类别</param>
    /// <param name="rawRank">JSON 中的原始等级</param>
    /// <returns>最终等级(1~50)</returns>
    private static int ResolveRank(QuestType type, int rawRank)
    {
        if (type == QuestType.Hero) return 50;
        if (type == QuestType.Event) return 25;

        int rank = rawRank <= 0 ? 1 : rawRank;
        return Math.Clamp(rank, 1, 50);
    }

    /// <summary>
    /// 把字符串解析为 <see cref="Camp"/>(支持中英文)<br/>
    /// 无法识别时返回 null
    /// </summary>
    /// <param name="camp">字符串</param>
    /// <returns>阵营(null 表示无阵营)</returns>
    private static Camp? ParseCamp(string camp) => camp?.ToLowerInvariant() switch
    {
        "plant" or "植物" => Card.Camp.Plant,
        "zombie" or "僵尸" => Card.Camp.Zombie,
        _ => null
    };

    /// <summary>
    /// 把字符串解析为 <see cref="QuestType"/>(支持中英文)
    /// </summary>
    /// <param name="type">字符串</param>
    /// <returns>任务类别</returns>
    private static QuestType ParseType(string type) => type?.ToLowerInvariant() switch
    {
        "normal" or "常驻" => QuestType.Normal,
        "daily" or "日常" => QuestType.Daily,
        "event" or "活动" => QuestType.Event,
        "hero" or "英雄" => QuestType.Hero,
        "tutorial" or "教程" => QuestType.Tutorial,
        _ => QuestType.None
    };

    /// <summary>
    /// 任务等级(1~50)<br/>
    /// </summary>
    public int Rank { get; private set; } = 1;

    /// <summary>
    /// 解析一条结构化奖励
    /// </summary>
    /// <param name="dict">奖励条目</param>
    /// <returns>奖励(无法识别时返回 null)</returns>
    private static Reward ParseReward(Dictionary dict)
    {
        if (dict == null) return null;

        var type = dict.GetValueOrDefault("Type").AsString();

        int amount = 1;
        var amountVariant = dict.GetValueOrDefault("Amount");
        if (amountVariant.VariantType != Variant.Type.Nil)
        {
            amount = amountVariant.AsInt32();
        }

        return type switch
        {
            "Gem" or "Diamond" => Reward.Diamond(amount),
            "Spark" => Reward.Spark(amount),
            "Ticket" => Reward.Ticket(amount),
            "Card" => Reward.Card(dict.GetValueOrDefault("Title").AsString(), amount),
            "Hero" => Reward.Hero(dict.GetValueOrDefault("Title").AsString()),
            _ => null
        };
    }

    /// <summary>
    /// 从 quests.json 中获得数据
    /// </summary>
    /// <param name="questTitle">任务类名，例如 DamageAndKillQuest</param>
    /// <param name="key">键，例如 Title、Des、Flavor、Reward、Rewards、Camp、Type、Id</param>
    /// <returns>对应的 Variant</returns>
    public static Variant GetData(string questTitle, string key)
    {
        if (_questDict == null)
        {
            using var file = FileAccess.Open("res://数据(C#)/quests.json", FileAccess.ModeFlags.Read);
            if (file == null)
            {
                return default;
            }

            var json = new Json();
            Error err = json.Parse(file.GetAsText());
            if (err != Error.Ok)
            {
                return default;
            }

            _questDict = json.Data.As<Dictionary>();
        }

        return _questDict.GetValueOrDefault($"{questTitle}.{key}");
    }
}