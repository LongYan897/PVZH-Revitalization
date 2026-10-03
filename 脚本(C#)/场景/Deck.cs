using Card;
using Card.String;
using Controller;
using Godot;
using Hero.Model;
using Hero.String;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace Scene;

[GlobalClass]
public partial class Deck : Node2D
{
    private Sprite2D BackGround; // 背景图
    private Sprite2D Top1; // 顶部遮挡
    private Sprite2D Top2; // 顶部遮挡1
    private TextureButton Return; // 返回按钮
    private TextureButton Plant; // 植物按钮
    private TextureButton Zombie; // 僵尸按钮
    private Control Information; //图鉴信息
    private ColorRect Cards; //卡片信息
    private HFlowContainer CardInformation; //卡牌列表
    private PackedScene classScene;
    private List<Node> CurrentInformation = new List<Node>();
    private Dictionary<Class, List<CardModel>> CurrentClassCards = new Dictionary<Class, List<CardModel>>();
    private List<Control> CurrentClasses = new List<Control>();
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
    public override void _Ready()
    {
        BackGround = FindChild("背景图") as Sprite2D;
        Top1 = FindChild("顶部遮挡") as Sprite2D;
        Top2 = FindChild("顶部遮挡1") as Sprite2D;
        Return = FindChild("返回按钮") as TextureButton;
        Plant = FindChild("植物按钮") as TextureButton;
        Zombie = FindChild("僵尸按钮") as TextureButton;
        Information = GetNode<Control>("%图鉴信息");
        Cards = GetNode<ColorRect>("%卡牌标志");
        CardInformation = GetNode<HFlowContainer>("%卡牌列表");
        classScene = ResourceLoader.Load<PackedScene>("res://场景(C#)/派系条.tscn");
        Return.Pressed += OnReturnPressed;
        Plant.Pressed += OnPlantPressed;
        Zombie.Pressed += OnZombiePressed;
        BackGround.Modulate = ThemeColors1[0];
        Top1.Modulate = ThemeColors1[1];
        Top2.Modulate = ThemeColors1[2];
        InitializeInformation(Camp.Plant.ToName());
    }

    private async void OnReturnPressed() {
        await SceneManager.ChangeScene(GetTree(), "res://场景/UI对象/主界面.tscn");
    }
    private void OnPlantPressed(){
        BackGround.Modulate = ThemeColors1[0];
        Top1.Modulate = ThemeColors1[1];
        Top2.Modulate = ThemeColors1[2];
        InitializeInformation(Camp.Plant.ToName());
    }
    private void OnZombiePressed(){
        BackGround.Modulate = ThemeColors2[0];
        Top1.Modulate = ThemeColors2[1];
        Top2.Modulate = ThemeColors2[2];
        InitializeInformation(Camp.Zombie.ToName());
    }
    private float CreateSpirtes(string camp)
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
            Information.AddChild(hs);
            CurrentInformation.Add(hs);
            hs.Position = new Vector2((position.X - 1) * (128 + 6.4f) + 24, (position.Y - 1) * 100 + 280);
            position.X++;
            if (position.X > 5)
            {
                position.Y++;
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
                Information.AddChild(hs);
                hs.Position = new Vector2((position.X - 1) * (128 + 6.4f) + 24, (position.Y - 1) * 100 + 280);
                position.X++;
            }
        }
        Cards.Position = new Vector2(-88, (position.Y - 1) * 100 + 280 + 72+64);
        CardInformation.Position = new Vector2(0, Cards.Position.Y + 60);
        return (position.Y - 1) * 100 + 280 + 72 + 64;
    }

    private void CreateCurrentCards()
    {
        
        int count = 0;
        foreach (Class c in CurrentClassCards.Keys) count += CurrentClassCards[c].Count;
        Class currentClass = CurrentClassCards.Keys.ToList()[0];
        Control classSprite = CreateClassSprite(currentClass);
        CurrentClasses.Add(classSprite);
        for (int n = 1;n < CurrentClassCards.Keys.Count;n++)
        {
            int x = 3;
            Class last = currentClass;
            foreach (CardModel card in CurrentClassCards[last])
            {
                x++;
                if (x > 3)
                {
                    x = 0;
                    classSprite.CustomMinimumSize += new Vector2(0,1)*134;
                }
            }
            classSprite.CustomMinimumSize += new Vector2(0,1)*40;
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
                NodeCard.DisplayCardAt(cardClone,Vector2.Zero,classSprite.GetNode<HFlowContainer>("排列"));
            }
        }
    }
    
    /// <summary>
    /// 初始化卡片，用于改变阵营
    /// </summary>
    /// <param name="camp">阵营</param>
    private void InitializeInformation(string camp) 
    {
        float cardOriginY = CreateSpirtes(camp);
        foreach (Control control in CurrentClasses) control.QueueFree();
        CurrentClasses = new List<Control>();
        CurrentClassCards = new Dictionary<Class, List<CardModel>>();
        List<Class> classes = sortClassFromCamp(camp);
        SortCards(cm => cm.Camp.ToName() == camp && cm.Rarity != Rarity.Token,classes);
        CreateCurrentCards();
    }
    /// <summary>
    /// 创建派系对应图标条
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    private Control CreateClassSprite(Class c)
    {
        Control classI = classScene.Instantiate<Control>();
        classI.GetNode<TextureRect>("图标").Texture = ResourceLoader.Load<Texture2D>(c.ToPath());
        classI.GetNode<Label>("文本").Text = c.ToName();
        GetNode<HFlowContainer>("%卡牌列表").AddChild(classI);
        return classI;
    }
    /// <summary>
    /// 通过阵营获取派系
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
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
    /// <summary>
    /// 把CurrentClassesCards内容以predicate检索并按照class分类、cost排序
    /// </summary>
    /// <param name="predicate"></param>
    private void SortCards(Func<CardModel,bool> predicate,List<Class> classes)
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
