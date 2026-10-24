using Card;
using Godot;
using Hero.Model;
using Pack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Scene;

[GlobalClass]
public partial class DeckSubMenuScene : SubMenuScene
{
	private Sprite2D BackGround; // 背景图
	private Sprite2D Top1; // 顶部遮挡
	private Sprite2D Top2; // 顶部遮挡1
	private TextureButton Plant; // 植物按钮
	private TextureButton Zombie; // 僵尸按钮
	private Control Container; //图鉴信息
	private ColorRect HeroLabel; //英雄标志
	private ColorRect Cards; //卡片信息
	private HFlowContainer CardInformation; //卡牌列表
	private NButton Search; //搜索
	private ColorRect SearchRect; //搜索框
	private Label Amount; //数量
	private PackedScene classScene;
	private List<Node> CurrentInformation = new List<Node>();
	private Dictionary<Class, List<CardModel>> CurrentClassCards = new Dictionary<Class, List<CardModel>>();
	private List<Control> CurrentClasses = new List<Control>();
	private Camp CurrentCamp = Camp.Plant;
	private List<NodeCard> AllNodeCards = new List<NodeCard>();
	private bool IsShowed = false;
	private List<Color> ThemeColors1 => new List<Color> {
		new Color(0.411f, 0.69f, 0.38f, 1.0f),
		new Color(0.535f, 0.9f, 0.495f, 1.0f),
		new Color(0.274f, 0.46f, 0.253f, 1.0f),
	};
	private List<Color> ThemeColors2 => new List<Color> {
		new Color(0.365f, 0.192f, 0.439f, 1.0f),
		new Color(0.657f, 0.348f, 0.79f, 1.0f),
		new Color(0.25f, 0.132f, 0.3f, 1.0f),
	};
	public override async Task _Load()
	{
		BackGround = FindChild("背景图") as Sprite2D;
		Top1 = FindChild("顶部遮挡") as Sprite2D;
		Top2 = FindChild("顶部遮挡1") as Sprite2D;
		Plant = FindChild("植物按钮") as TextureButton;
		Zombie = FindChild("僵尸按钮") as TextureButton;
		Container = GetNode<HFlowContainer>("%容器");
		HeroLabel = GetNode<ColorRect>("%英雄标志");
		Cards = GetNode<ColorRect>("%卡牌标志");
		CardInformation = GetNode<HFlowContainer>("%卡牌列表");
		Search = GetNode<NButton>("%搜索");
		SearchRect = GetNode<ColorRect>("%搜索框");
		Amount = GetNode<Label>("%数量");
		classScene = ResourceLoader.Load<PackedScene>("res://场景(C#)/派系条.tscn");
		Plant.Pressed += OnPlantPressed;
		Zombie.Pressed += OnZombiePressed;
		Search.Pressed += OnSearchPressed;
		Search.GetNode<NButton>("取消").Pressed += OnCancelPressed;
		BackGround.Modulate = ThemeColors1[0];
		Top1.Modulate = ThemeColors1[1];
		Top2.Modulate = ThemeColors1[2];
		CurrentCamp = Camp.Plant;
		await InitializeInformation(CurrentCamp.ToName());
	}
	private async void OnCancelPressed()
	{
		ShowCards(c => true);
		Button cancelFrame = Search.GetNode<Button>("取消框");
		NButton cancel = Search.GetNode<NButton>("取消");
		Tween t1 = cancelFrame.CreateTween();
		Tween t2 = Search.CreateTween();
		t1.TweenProperty(cancelFrame, "size", new Vector2(255, 250), 0.5);
		t2.TweenProperty(Search, "position", new Vector2(370, 27), 0.5);
		cancel.Visible = false;
		cancel.Position = new Vector2(0, 0);
		IsShowed = false;
	}
	private async void OnSearchPressed()
	{
		SearchRect.Visible = true;
		LineEdit lineEdit = SearchRect.GetNode<LineEdit>("LineEdit");
		lineEdit.GrabFocus();
		await lineEdit.ToSignal(lineEdit, LineEdit.SignalName.TextSubmitted);
		string text = lineEdit.Text;
		SearchRect.Visible = false;
		lineEdit.Text = "";
		if (!string.IsNullOrEmpty(text)) {
			SearchCards(text);
			if (IsShowed) return;
			Button cancelFrame = Search.GetNode<Button>("取消框");
			NButton cancel = Search.GetNode<NButton>("取消");
			Tween t1 = cancelFrame.CreateTween();
			Tween t2 = Search.CreateTween();
			t1.TweenProperty(cancelFrame,"size",new Vector2(510,250),0.5);
			t2.TweenProperty(Search, "position", new Vector2(370-81.6f, 27), 0.5);
			await t1.ToSignal(t1,Tween.SignalName.Finished);
			cancel.Visible = true;
			cancel.Position = new Vector2(255,0);
			IsShowed = true;
		}
		else
		{
			ShowCards(c => true);
		}
	}
	private async void OnPlantPressed()
	{
		BackGround.Modulate = ThemeColors1[0];
		Top1.Modulate = ThemeColors1[1];
		Top2.Modulate = ThemeColors1[2];
		CurrentCamp = Camp.Plant;
		await InitializeInformation(CurrentCamp.ToName());
	}
	private async void OnZombiePressed()
	{
		BackGround.Modulate = ThemeColors2[0];
		Top1.Modulate = ThemeColors2[1];
		Top2.Modulate = ThemeColors2[2];
		CurrentCamp = Camp.Zombie;
		await InitializeInformation(CurrentCamp.ToName());
	}
	private void CreateSpirtes(string camp)
	{
		if (CurrentInformation.Count > 0)
		{
			foreach (Node node in CurrentInformation)
			{
				node.QueueFree();
			}
		}
		CurrentInformation = new List<Node>();
		List<Type> types = HeroModel.SeekHeroModel(camp);
		Vector2I position = new Vector2I(1, 1);
		foreach (Type type in types)
		{
			string name = type.Name;
			HeroSprite hs = HeroSprite.Create(name);
			Container.GetNode<HFlowContainer>("标志栏").AddChild(hs);
			CurrentInformation.Add(hs);
			position.X++;
			if (position.X > 5)
			{
				position.X = 1;
			}
		}
		if (position.X > 1)
		{
			for (int i = position.X; i < 6; i++)
			{
				Control hs;
				if (camp == Camp.Plant.ToName()) { hs = ResourceLoader.Load<PackedScene>("res://场景(C#)/植物英雄图标.tscn").Instantiate<Control>(); }
				else { hs = ResourceLoader.Load<PackedScene>("res://场景(C#)/僵尸英雄图标.tscn").Instantiate<Control>(); }
				CurrentInformation.Add(hs);
				Container.GetNode<HFlowContainer>("标志栏").AddChild(hs);
				position.X++;
			}
		}
	}
	private async Task CreateCurrentCards()
	{
		int count = 0;
		foreach (Class c in CurrentClassCards.Keys) count += CurrentClassCards[c].Count;
		Class currentClass;
		Control classSprite;
		for (int n = 0; n < CurrentClassCards.Keys.Count; n++)
		{
			currentClass = CurrentClassCards.Keys.ToList()[n];
			classSprite = CreateClassSprite(currentClass);
			CurrentClasses.Add(classSprite);
		}
		for (int n = 0; n < CurrentClassCards.Keys.Count; n++)
		{
			currentClass = CurrentClassCards.Keys.ToList()[n];
			classSprite = CurrentClasses[n];
			foreach (CardModel card in CurrentClassCards[currentClass])
			{
				CardModel cardClone = card;
				cardClone.Status = Status.FaceUp;
				NodeCard Ncard = await NodeCard.DisplayCardAt(cardClone, Vector2.Zero, classSprite.GetNode<HFlowContainer>("排列"), CardView.Collection, 1);
				AllNodeCards.Add(Ncard);
			}
		}
	}
	private async Task InitializeInformation(string camp)
	{
		CreateSpirtes(camp);
		if (camp == Camp.Plant.ToName()) {
			Plant.GetNode<TextureRect>("高亮").Visible = true;
			Zombie.GetNode<TextureRect>("高亮").Visible = false;
		}
		else
		{
			Plant.GetNode<TextureRect>("高亮").Visible = false;
			Zombie.GetNode<TextureRect>("高亮").Visible = true;
		}
		foreach (Control control in CurrentClasses) control.QueueFree();
		CurrentClasses = new List<Control>();
		CurrentClassCards = new Dictionary<Class, List<CardModel>>();
		AllNodeCards = new List<NodeCard>();
		List<Class> classes = sortClassFromCamp(camp);
		SortCards(cm => cm.Camp.ToName() == camp && cm.Rarity != Rarity.Token, classes);
		await CreateCurrentCards();
        Amount.Text = AllNodeCards.Count.ToString();
		SelectDeck.CurrentCamp = CurrentCamp;
    }
	private Control CreateClassSprite(Class c)
	{
		Control classI = classScene.Instantiate<MinimumHFlowContainer>();
		classI.GetNode<TextureRect>("底板/图标").Texture = ResourceLoader.Load<Texture2D>(c.ToPath());
		classI.GetNode<Label>("底板/文本").Text = c.ToName();
		GetNode<HFlowContainer>("%卡牌列表").AddChild(classI);
		return classI;
	}
	private void ShowCards(Func<CardModel,bool> predicate)
	{
		SortCards(predicate,CurrentClassCards.Keys.ToList());
		foreach (NodeCard ncard in AllNodeCards)
		{
			if (!predicate(ncard.Model)) ncard.Visible = false;
			else ncard.Visible = true;
			GD.Print(predicate(ncard.Model), "->", ncard.Model.Title);
		}
	}
	private void SearchCards(string keywords)
	{
		List<string> keyWords = keywords.Split(" ").ToList();
		Dictionary<Func<CardModel, bool>,string> predicates = new Dictionary<Func<CardModel,bool>,string>();
		List<string> classNames = new List<string>();
		foreach (Class c in Enum.GetValues<Class>()) classNames.Add(c.ToName());
		foreach (string keyword in keyWords)
		{
			string word = keyword;
			string predicateMod = "";
			if (word[0] == '+')
			{
				predicateMod = "+";
				word.Remove(0);
			}
			else if (word[0] == '-')
			{
				predicateMod = "-";
				word.Remove(0);
			}
			Func<CardModel, bool> p = (c) => c.GetAllStrings().Any(s => s.Contains(word));
			predicates[p] = predicateMod;
		}
		Func<CardModel, bool> predicate = (c) =>
		{
			bool result = true;
			foreach (Func<CardModel,bool> p in predicates.Keys)
			{
				if (predicates[p] == "") result = result && p(c);
				else if (predicates[p] == "-") result = result && !p(c);
				else if (predicates[p] == "+") result = result || p(c);
			}
			return result;
		};
		ShowCards(predicate);
	} 
	private List<Class> sortClassFromCamp(string camp)
	{
		return camp switch
		{
			"植物" => [
				Class.MegaGrow,
				Class.Guardian,
				Class.Kabloom,
				Class.Smarty,
				Class.Solar
			],
			"僵尸" => [
				Class.Sneaky,
				Class.Hearty,
				Class.Crazy,
				Class.Brainy,
				Class.Beastly,
			],
			_ => []
		};
	}
	private void SortCards(Func<CardModel, bool> predicate, List<Class> classes)
	{
		Vector2I costRange = new Vector2I(0, -1);
		foreach (CardModel card in CardModel.AllCards.Where(predicate))
		{
			if (costRange[0] > costRange[1]) costRange = new Vector2I(card.Cost.Current, card.Cost.Current);
			else
			{
				if (costRange[0] > card.Cost.Current) costRange[0] = card.Cost.Current;
				if (costRange[1] < card.Cost.Current) costRange[1] = card.Cost.Current;
			}
		}
		foreach (Class c in classes)
		{
			CurrentClassCards[c] = new List<CardModel>();
			for (int cost = costRange[0]; cost <= costRange[1]; cost++)
			{
				List<CardModel> cards = CardModel.AllCards.Where(cm => cm.Cost.Current == cost && predicate(cm) && cm.Class == c).ToList();
				CurrentClassCards[c].AddRange(cards);
			}
		}
	}
   

}
