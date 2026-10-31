using Battle;
using Battle.Entity;
using Card.Cmd;
using Card.String;
using Controller;
using Godot;
using Logger;
using Pack;
using Phrases;
using Play;
using Scene;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Target;
using Variable;

namespace Card;

/// <summary>
/// 卡牌模板(继承用)
/// </summary>
public class CardModel
{
    public Player Player => PlayerManager.GetPlayer(Camp);
    private static readonly Dictionary<string, CardModel> TemplateEntries = new();
    /// <summary>
    /// 按卡牌“类名”（cards.json 里键的前缀，如 Peashooter）索引的模板。<br/>
    /// cardPack.json 的 group.Cards 用的是类名而不是中文标题，所以需要这张表。
    /// </summary>
    private static readonly Dictionary<string, CardModel> ClassEntries = new();
    /// <summary>
    /// 获取卡牌已有的变量
    /// </summary>
    /// <param name="sign">变量名称</param>
    /// <returns></returns>
    public ICardVariable this[string sign]
    {
        get
        {
            foreach (var variable in _variables)
            {
                if (variable.Key == sign)
                    return variable;
            }
            return null;
        }
    }
    /// <summary>
    /// 从描述中获取整数变量的值
    /// </summary>
    /// <param name="sign">变量名称</param>
    /// <returns></returns>
    public int GetSignInt(string sign)
        => GetSignVariable(sign)?.AbsValue ?? 0;

    /// <summary>
    /// 从描述中获取按名字 + 标签整数变量的值，
    /// 等价于 GetSignInt($"{name}@{tag}")。
    /// </summary>
    public int GetSignInt(string name, string tag)
        => GetSignVariable(name, tag)?.AbsValue ?? 0;

    /// <summary>
    /// 从描述中获取整数变量
    /// </summary>
    /// <param name="sign">变量名称</param>
    /// <returns></returns>
    public DescriptionVariable GetSignVariable(string sign)
    {
        foreach (var variable in _variables)
        {
            if (variable.Key == sign && variable is DescriptionVariable description)
                return description;
        }
        return null;
    }


    /// <summary>
    /// 从描述中获取按名字 + 标签整数变量的值，
    /// 等价于 GetSignVariable($"{name}@{tag}")
    /// </summary>
    /// <param name="sign">变量名称</param>
    /// <returns></returns>
    public DescriptionVariable GetSignVariable(string sign, string tag)
        => GetSignVariable(string.IsNullOrEmpty(tag) ? sign : $"{sign}@{tag}");

    /// <summary>
    /// 检测卡牌是否能打出
    /// </summary>
    /// <returns></returns>
    public bool CanPlay()
    {
        var p = PlayerManager.GetPlayer(Camp);
        var isTurnMe =
            (PhraseManager.Phrase == Phrase.Plant && Camp == Camp.Plant)
            || (PhraseManager.Phrase == Phrase.Zombie && Camp == Camp.Zombie && CardType.HasFlag(CardType.Fighter))
            || (PhraseManager.Phrase == Phrase.Trick && Camp == Camp.Zombie && (CardType.HasFlag(CardType.Trick) || CardType.HasFlag(CardType.Environment)));

        return isTurnMe && (Fighter.CanTargetedBy(this, false) || NodeRoad.CanTargetedBy(this)) && !Road.IsBattling && !CardCmd.StopCardPlay && (p == null || (p != null && (p.Status.Hero.Cost == null || (p.Status.Hero.Cost != null && p.Status.Hero.Cost.Current >= Cost.Current))));
    }
    /// <summary>
    /// 按照名称获取卡牌模板
    /// </summary>
    public static CardModel GetTemplate(string title)
    {
        if (TemplateEntries.TryGetValue(title, out var template))
            return template.Clone();
        return null;
    }

    /// <summary>
    /// 按卡牌类名（cards.json 里键的前缀，如 Peashooter）获取卡牌模板。<br/>
    /// 卡包数据(cardPack.json 的 group.Cards)里存的就是类名。
    /// </summary>
    public static CardModel GetTemplateByClassName(string className)
    {
        if (!string.IsNullOrEmpty(className) && ClassEntries.TryGetValue(className, out var template))
            return template.Clone();
        return null;
    }

    /// <summary>
    /// 从id获取卡牌
    /// </summary>
    /// <param name="id">卡牌的ID</param>
    /// <returns></returns>
    public static CardModel FromId(uint id)
    {
        return TemplateEntries.FirstOrDefault(t=>t.Value.Id == id).Value;
    }

    /// <summary>
    /// 是否存在该名称的卡牌模板
    /// </summary>
    public static bool HasTemplate(string title)
    {
        return TemplateEntries.ContainsKey(title);
    }
    /// <summary>
    /// 卡牌的模板(用于获取卡牌数据)(不要修改)
    /// </summary>
    public static CardModel GetTemplate<T>() where T : CardModel
    {
        return Load<T>();
    }

    /// <summary>
    /// 获取战斗的中心
    /// </summary>
    /// <returns></returns>
    public static Vector2 CenterForCard() => BattleSubMenuScene.Center;

    /// <summary>
    /// 初始化卡牌基类
    /// </summary>
    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var loadMethod = typeof(CardModel)
            .GetMethod(nameof(Load), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);

        foreach (var type in assembly.GetTypes())
        {
            if (!typeof(CardModel).IsAssignableFrom(type)) continue;
            if (type.IsAbstract) continue;
            if (type == typeof(CardModel)) continue;
            if (typeof(FighterCardModel) == type) continue;

            try
            {
                var cardString = CardString.Loading(type.Name);
                var loadData = typeof(CardModel)
                    .GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static);

                var generic = loadData.MakeGenericMethod(type);
                var model = (CardModel)generic.Invoke(null, new object[] { cardString });
                if (!string.IsNullOrEmpty(model.Title))
                    TemplateEntries[model.Title] = model;
                ClassEntries[type.Name] = model;
            }
            catch (Exception ex)
            {
                Log.Error($"[CardModel.Init] Failed on type: {type.Name}, msg: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
    public static List<CardModel> AllCards => TemplateEntries.Values.ToList();
    /// <summary>
    /// 刷新卡牌的节点
    /// </summary>
    public void Fresh()
    {
        var cardNode = NodeCard.GetNode(this);
        cardNode?.Fresh();

        if (this is FighterCardModel fighter)
        {
            var fighterNode = Fighter.GetNode(fighter);

            if (fighterNode != null)
            {
                fighterNode.CardTryFresh(this);
            }
        }

        CardDes.instance?.CardTryFresh(this);
    }
    /// <summary>
    /// 获取卡牌处理后的描述
    /// </summary>
    public string GetDescription()
    {
        string v = "";
        if (this is FighterCardModel fighter)
        {
            var addition = new List<string>();
            if (fighter.CardTag.HasFlag(CardTag.Amphibious))
            {
                addition.Add("_两栖_");
            }
            if (!string.IsNullOrEmpty(fighter.AtkType.Current.ExLabel))
            {
                addition.Add(fighter.AtkType.Current.ExLabel);
            }
            if (!string.IsNullOrEmpty(fighter.HpType.Current.ExLabel))
            {
                addition.Add(fighter.HpType.Current.ExLabel);
            }
            if (fighter.CardTag.HasFlag(CardTag.GraveStone))
            {
                addition.Add("_墓碑_");
            }
            if (fighter.CardTag.HasFlag(CardTag.Coop))
            {
                addition.Add("_组队_");
            }
            if (addition.Count > 0)
                v += string.Join("，", addition);
            if (!string.IsNullOrEmpty(v))
            {
                v += "\n";
            }
        }
        return v + Description;
    }
    public async Task DestroyMe()
    {
        await Destroy();
    }
    /// <summary>
    /// 是否能把友军当作目标
    /// </summary>
    public virtual bool FriendTarget => false;
    /// <summary>
    /// 卡牌费用图片
    /// </summary>
    public Texture2D CampCostIcon
    {
        get
        {
            return Camp switch
            {
                Camp.Plant => GD.Load<Texture2D>("res://素材/卡牌属性图片/inhnd_sun_gb.png"),
                Camp.Zombie => GD.Load<Texture2D>("res://素材/卡牌属性图片/inhnd_brains.png"),
                _ => GD.Load<Texture2D>("res://素材/卡牌属性图片/inhnd_sun_gb.png"),
            };
        }
    }

    /// <summary>
    /// 变化卡牌
    /// </summary>
    /// <returns></returns>
    public async Task TransformTo(CardModel cardModel)
    {
        Player.Status.HandRemoveCard(this);
        Player.Status.HandAddCard(cardModel);
        await NodeCard.GetNode(this).TransformTo(cardModel);
        await TransferBuffsTo(cardModel);
        await FireTiming(Timing.WhenTransformed, this, cardModel);
        await CardCmd.TimingOnCards(this, Timing.WhenTransformed, this, cardModel);
    }
    /// <summary>
    /// 播放一个即时的动画,如果玩家是HostPlayer播放up,反之down
    /// </summary>
    /// <param name="up">向上的动画</param>
    /// <param name="down">向下的动画</param>
    /// <param name="target">目标</param>
    /// <param name="track">轨道</param>
    /// <param name="eventName">要等待到哪一个事件(null为等到动画完成)</param>
    /// <returns></returns>
    protected async Task PlayLocationalAnimation(string up,string down, ITarget target, int track = 0, string eventName = null)
    {
        var node = Main.Animator;
        var sp = SpineHandler.Get();
        sp.Position = ((Control)(GodotObject)target).GlobalPosition;
        sp.Scale = new(0.7f, 0.7f);
        node.AddChild(sp);
        sp.LoadSkeletonData(AnimationPath);
        var animName = PlayerManager.HostPlayer == Player ? up : down;
        if (eventName != null)
            await sp.SetAnimationAndFreeOnEvent(track, animName, eventName, false);
        else
            await sp.SetAnimationAndFreeOnEndTask(track, animName, false);
    }
    /// <summary>
    /// 播放一个即时的动画
    /// </summary>
    /// <param name="animName">动画名</param>
    /// <param name="target">目标</param>
    /// <param name="track">轨道</param>
    /// <param name="eventName">要等待到哪一个事件(null为等到动画完成)</param>
    /// <returns></returns>
    protected async Task PlayInstantAnimation(string animName, ITarget target, int track = 0, string eventName = null)
    {
        var node = Main.Animator;
        var sp = SpineHandler.Get();
        sp.Position = ((Control)(GodotObject)target).GlobalPosition;
        sp.Scale = new(0.7f, 0.7f);
        node.AddChild(sp);
        sp.LoadSkeletonData(AnimationPath);
        if (eventName != null)
            await sp.SetAnimationAndFreeOnEvent(track, animName, eventName, false);
        else
            await sp.SetAnimationAndFreeOnEndTask(track, animName, false);
    }
    /// <summary>
    /// 播放一个循环的动画
    /// </summary>
    /// <param name="animName">动画名</param>
    /// <param name="target">目标</param>
    /// <param name="track">轨道</param>
    /// <returns></returns>
    protected async Task PlayLoopAnimation(string animName, ITarget target, int track = 0)
    {
        var node = Main.Animator;
        var sp = SpineHandler.Get();
        sp.Position = ((Control)(GodotObject)target).GlobalPosition;
        sp.Scale = new(0.7f, 0.7f);
        node.AddChild(sp);
        sp.LoadSkeletonData(AnimationPath);
        await sp.SetAnimationTask(track, animName, true);
    }
    public static CardModel Load<T>() where T : CardModel
    {
        var model = Load<T>(CardString.Loading(typeof(T).Name));
        CardCmd.Add(model);
        return model;
    }
    /// <summary>
    /// 从CardString加载卡牌数据
    /// </summary>
    /// <param name="cardString">目标String</param>
    /// <returns>加载出来的卡牌</returns>
    private static T Load<T>(CardString cardString) where T : CardModel
    {
        var card = Activator.CreateInstance<T>();
        card.Cost = new CardIntVariable(card, "Cost", cardString.Cost);
        card.Title = cardString.Title;
        card.Description = cardString.Description;
        card.Flavor = cardString.Flavor;
        card.Rarity = cardString.Rarity;
        card.Class = cardString.Category;
        card.Camp = cardString.Camp;
        card.CardType = cardString.CardType;
        card.Pack = cardString.Pack;
        card.AnimationPath = cardString.AnimationPath;
        card.AnimationScenePath = cardString.AnimationScenePath;
        card.ParseDescriptionVariables(cardString.Description);
        card.Icon = cardString.IconPath.Load<PackedScene>();
        card.Id = cardString.Id;
        card.Labels = new CardVariable<List<string>>(card, "Labels", cardString.Labels);
        return (T)card.LoadData(cardString);
    }
    private async Task Destroy()
    {
        CardCmd.Remove(this);
        if (PhraseManager.IsInBattle)
            PlayerManager.HostPlayer.Status.HandRemoveCard(this);
        await NodeCard.DestoryCard(this);
    }
    /// <summary>
    /// 目标选择
    /// </summary>
    public virtual TargetType TargetType { get; } = TargetType.Fighters;
    /// <summary>
    /// 子类加载卡牌数据
    /// </summary>
    /// <param name="cardString">目标String</param>
    /// <returns>加载出来的卡牌</returns>
    protected virtual CardModel LoadData(CardString cardString)
    {
        return this;
    }
    /// <summary>
    /// 这个是筛选能成为目标的函数
    /// </summary>
    /// <param name="target">目标</param>
    /// <returns></returns>
    public virtual bool TargetFilter(ITarget target)
    {
        return true;
    }

    [IconText("000000")]
    public const string Sun = "res://素材(C#)/文本图片/inhnd_sun_gb.png";

    [IconText("000000")]
    public const string Brain = "res://素材(C#)/文本图片/inhnd_brains.png";
    /// <summary>
    /// 卡牌的特殊标签
    /// </summary>
    public string ExDescription { get; set; }
    /// <summary>
    /// 卡牌的标题(名字)
    /// </summary>
    public string Title { get; private set; }
    /// <summary>
    /// 卡牌的正规描述
    /// </summary>
    public string Description { get; private set; }
    /// <summary>
    /// 卡牌的非正规描述
    /// </summary>
    public string Flavor { get; private set; }
    /// <summary>
    /// 卡牌的卡包
    /// </summary>
    public string Pack { get; private set; }
    /// <summary>
    /// 卡牌的动画路径
    /// </summary>
    public string AnimationPath { get; private set; }
    /// <summary>
    /// 卡牌的动画场景路径
    /// </summary>
    public string AnimationScenePath { get; private set; }
    /// <summary>
    /// 卡牌的序列号(用于解析为实例卡牌)
    /// </summary>
    public uint Id {  get; private set; }
    /// <summary>
    /// 卡牌的稀有度
    /// </summary>
    public Rarity Rarity { get; private set; }
    /// <summary>
    /// 卡牌的派系
    /// </summary>
    public Class Class { get; private set; }
    /// <summary>
    /// 卡牌的阵营
    /// </summary>
    public Camp Camp { get; private set; }

    /// <summary>
    /// 卡牌的状态（正面/反面）
    /// </summary>
    public Status Status { get; set; } = Status.FaceUp;
    /// <summary>
    /// 卡牌的类别
    /// </summary>
    public CardType CardType { get; private set; }
    /// <summary>
    /// 卡牌的标签
    /// </summary>
    public CardVariable<List<string>> Labels { get; private set; }
    /// <summary>
    /// 卡牌的特殊变量
    /// </summary>
    private readonly List<ICardVariable> _variables = new List<ICardVariable>();
    /// <summary>
    /// 卡牌的特殊变量
    /// </summary>
    public IReadOnlyList<ICardVariable> Variables => _variables;
    /// <summary>
    /// 卡牌的费用
    /// </summary>
    public CardIntVariable Cost { get; private set; }
    /// <summary>
    /// 卡牌的卡面
    /// </summary>
    public PackedScene Icon { get; private set; }
    /// <summary>
    /// 卡牌打出时的效果
    /// </summary>
    /// <param name="target">游戏传入的卡牌目标</param>
    /// <returns></returns>
    public virtual Task Play(ITarget target) { return Task.CompletedTask; }
    /// <summary>
    /// 卡牌打出时的效果
    /// </summary>
    /// <param name="target">游戏传入的卡牌目标</param>
    /// <returns></returns>
    public async Task OnPlayWrap(ITarget target)
    {
        await Task.WhenAll(Play(target), CardCmd.ChangeCost(null, -Cost.Current, this));
    }
    /// <summary>
    /// 克隆之后的逻辑
    /// </summary>
    /// <param name="card"></param>
    protected virtual void AfterClone() { }
    /// <summary>
    /// 克隆卡牌
    /// </summary>
    /// <returns></returns>
    public CardModel Clone()
    {
        CardModel card = MemberwiseClone() as CardModel;
        card.Cost = new CardIntVariable(card, "Cost", Cost.Current);
        card.Labels = new CardVariable<List<string>>(card, "Labels", new List<string>(Labels.Current));
        card.AfterClone();
        CardCmd.Add(card);
        return card;
    }

    private static readonly Regex DescriptionVarRegex =
    new(@"=([^=]+)=\{([+-]?)(\d+)\}", RegexOptions.Compiled);

    private static readonly Regex TaggedKeyRegex =
        new(@"^([^#]+)#([^#]+)#$", RegexOptions.Compiled);

    /// <summary>
    /// 从描述文本里解析 =XXX={+N} / =XXX={-N} / =XXX={N}，
    /// 把数值按绝对值 + 方向存入 _variables。<br/>
    /// =Name#Tag#={N} 语法：内部 key 记为 "Name@Tag"<br/>
    /// </summary>
    private void ParseDescriptionVariables(string description)
    {
        if (string.IsNullOrEmpty(description)) return;

        foreach (Match m in DescriptionVarRegex.Matches(description))
        {
            string rawKey = m.Groups[1].Value;
            string sign = m.Groups[2].Value;
            string numStr = m.Groups[3].Value;

            if (!int.TryParse(numStr, out int absValue)) continue;
            if (absValue < 0) absValue = -absValue;

            bool isNegative = sign == "-";

            string key = rawKey;
            var tm = TaggedKeyRegex.Match(rawKey);
            if (tm.Success)
                key = $"{tm.Groups[1].Value}@{tm.Groups[2].Value}";

            if (_variables.Any(v => v.Key == key))
                continue;

            _variables.Add(new DescriptionVariable(key, absValue, isNegative));
        }
    }
    /// <summary>
    /// 子类重置卡牌数值的逻辑
    /// </summary>
    protected virtual Task ResetData() => Task.CompletedTask;
    /// <summary>
    /// 重置卡牌的数值
    /// </summary>
    public async Task Reset()
    {
        await Cost.Reset();
        await Labels.Reset();
        ExDescription = null;
        await ResetData();
    }
    /// <summary>
    /// 修改这张卡牌时
    /// </summary>
    /// <typeparam name="T">修改值的类型</typeparam>
    /// <param name="valueAfter">修改之前的值</param>
    /// <param name="valueBefore">修改之后的值</param>
    /// <returns></returns>
    public virtual Task Changed<T>(string key, T valueAfter, T valueBefore,VariableReason reason)
    {
        return Task.CompletedTask;
    }
    /// <summary>
    /// 挂在卡牌上的效果
    /// </summary>
    private List<Buff> _buffs;

    private List<Buff> BuffList
    {
        get
        {
            _buffs ??= [.. InitBuffs];
            return _buffs;
        }
    }

    public IReadOnlyList<Buff> Buffs => BuffList;
    /// <summary>
    /// 卡牌初始的Buff
    /// </summary>
    protected virtual List<Buff> InitBuffs { get; } = [];
    /// <summary>
    /// 添加效果
    /// </summary>
    public async Task AddBuff(Buff buff, VariableReason reason)
    {
        BuffList.Add(buff);
        await buff.OnTiming(Timing.WhenApply, this, reason);
    }

    protected void RegistBuff(Buff buff)
    {
        if (buff != null) BuffList.Add(buff);
    }
    /// <summary>
    /// 移除效果
    /// </summary>
    public async Task RemoveBuff(Buff buff, VariableReason reason)
    {
        BuffList.Remove(buff);
        await buff.OnTiming(Timing.WhenRemove, this, reason);
    }

    /// <summary>
    /// 把时点转发给所有关心它的 Buff
    /// </summary>
    public async Task FireTiming(Timing timing, params object[] parameters)
    {
        var fired = new List<Buff>();

        foreach (var buff in BuffList)
        {
            if (this is FighterCardModel fighterCard && !fighterCard.IsPlaced && buff.OnlyPlaced) continue;
            if (!buff.Timings.Contains(timing)) continue;
            fired.Add(buff);
        }

        foreach (var buff in fired.OrderByDescending(b => b.Priority))
        {
            await buff.OnTiming(timing, this, parameters);
        }
    }

    /// <summary>
    /// 把 Buff 转移到新卡
    /// </summary>
    public async Task TransferBuffsTo(CardModel newCard)
    {
        foreach (var buff in BuffList.ToList())
        {
            if (!buff.IsTransferable) continue;
            await RemoveBuff(buff, VariableReason.Self);
            await newCard.AddBuff(buff, VariableReason.Self);
        }
    }

}
public static class CardModelExtensions
{
    public static bool HasLabel(this CardModel model, string label)
        => model != null && model.Labels.Current.Contains(label);
    public static bool IsClass(this CardModel model, Class @class)
        => model != null && model.Class.HasFlag(@class);
    public static List<string> GetAllStrings(this CardModel model)
    {
        List<string> strings =
        [
            model.Rarity.ToName(),
            model.CardType.ToName(),
            model.Title,
            model.Pack,
            model.Camp.ToName(),
            model.Class.ToName(),
            model.Description,
            .. model.Labels.Current,
            model.Flavor,
            model.CardType.ToName(),
            model.ExDescription,
            model.Cost.Current.ToString(),
        ];
        List<string> result = new List<string>(strings);
        foreach (string s in strings) if (string.IsNullOrEmpty(s)) { result.Remove(s); }
        return result;
    }
}