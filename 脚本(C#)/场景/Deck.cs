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
    private PackedScene classScene;
    private List<Node> CurrentInformation = new List<Node>();
    private List<NodeCard> CurrentCards = new List<NodeCard>();
    private Dictionary<Class, List<CardModel>> CurrentClassCards = new Dictionary<Class, List<CardModel>>();
    
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
        classScene = ResourceLoader.Load<PackedScene>("res://场景(C#)/派系条.tscn");
        Return.Pressed += OnReturnPressed;
        Plant.Pressed += OnPlantPressed;
        Zombie.Pressed += OnZombiePressed;
        BackGround.Modulate = ThemeColors1[0];
        Top1.Modulate = ThemeColors1[1];
        Top2.Modulate = ThemeColors1[2];
        InitializeInformation("植物");
    }

    private async void OnReturnPressed() {
        await SceneManager.ChangeScene(GetTree(), "res://场景/UI对象/主界面.tscn");
    }
    private void OnPlantPressed(){
        BackGround.Modulate = ThemeColors1[0];
        Top1.Modulate = ThemeColors1[1];
        Top2.Modulate = ThemeColors1[2];
        InitializeInformation("植物");
    }
    private void OnZombiePressed(){
        BackGround.Modulate = ThemeColors2[0];
        Top1.Modulate = ThemeColors2[1];
        Top2.Modulate = ThemeColors2[2];
        InitializeInformation("僵尸");
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
                if (camp == "植物") { hs = ResourceLoader.Load<PackedScene>("res://场景(C#)/植物英雄图标.tscn").Instantiate<Control>(); }
                else { hs = ResourceLoader.Load<PackedScene>("res://场景(C#)/僵尸英雄图标.tscn").Instantiate<Control>(); }
                CurrentInformation.Add(hs);
                Information.AddChild(hs);
                hs.Position = new Vector2((position.X - 1) * (128 + 6.4f) + 24, (position.Y - 1) * 100 + 280);
                position.X++;
            }
        }
        Cards.Position = new Vector2(-88, (position.Y - 1) * 100 + 280 + 72+64);
        return (position.Y - 1) * 100 + 280 + 72 + 64;
    }

    private void CreateCurrent80Cards(int line,float y)
    {
        int count = 0;
        foreach (Class c in CurrentClassCards.Keys) count += CurrentClassCards[c].Count;
        int currentClassN = 0;
        Vector2 originPosition = new Vector2(-80+16,y+36);
        Vector2 position = originPosition;
        Control classI = classScene.Instantiate<Control>();
        classI.Position = new Vector2(0, y - 36);
        classI.GetNode<TextureRect>("图标").Texture = ResourceLoader.Load<Texture2D>(CurrentClassCards.Keys.ToList()[currentClassN].ToPath());
        classI.GetNode<Label>("文本").Text = CurrentClassCards.Keys.ToList()[currentClassN].ToString();
        int ReadedClassCount = 0;
        GD.Print(count);
        Class currentClass = CurrentClassCards.Keys.ToList()[currentClassN];
        for (int i = 0;i < Math.Min(count,80); i++)
        {
            NodeCard nodeCard = NodeCard.DisplayCardAt(CurrentClassCards[currentClass][i-ReadedClassCount],position,GetNode<Control>("图鉴信息"));
            nodeCard.Scale = Vector2.One * 0.625f;
            position += new Vector2(1, 0) * 176;
            if (position.X > originPosition.X + 4 * 176)
            {
                position.X = originPosition.X;
                position.Y += 140.625f;
            }
            if (i + 1 > CurrentClassCards[currentClass].Count && currentClassN+1 != CurrentClassCards.Keys.ToList().Count)
            {
                ReadedClassCount += CurrentClassCards[currentClass].Count;
                currentClassN++;
                currentClass = CurrentClassCards.Keys.ToList()[currentClassN];
                Control neoclassI = classScene.Instantiate<Control>();
                neoclassI.Position = new Vector2(0, position.Y + 36+120.625f/2);
                neoclassI.GetNode<TextureRect>("图标").Texture = ResourceLoader.Load<Texture2D>(CurrentClassCards.Keys.ToList()[currentClassN].ToPath());
                neoclassI.GetNode<Label>("文本").Text = CurrentClassCards.Keys.ToList()[currentClassN].ToString();
                position.Y += 72 + 120.625f;
            }
        }
    }
    

    private void InitializeInformation(string camp)
    {
        float cardOriginY = CreateSpirtes(camp);
        foreach (NodeCard card in CurrentCards)
        {
            _ = card.Model.DestroyMe();
        }
        List<Class> classes = sortClassFromCamp(camp);
        foreach (Class c in classes)
        {
            CurrentClassCards[c] = new List<CardModel>();
            int cost = 0;
            while (true){
                List<CardModel> cards = CardModel.AllCards.Where(cm => cm.Cost.Current == cost && cm.Class == c).ToList();
                if (cards.Count == 0) break;
                CurrentClassCards[c].AddRange(cards);
                cost++;
            }
        }
        CreateCurrent80Cards(1,cardOriginY);
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
}
