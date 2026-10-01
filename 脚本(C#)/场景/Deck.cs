using Card;
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
        Return.Pressed += OnReturnPressed;
        Plant.Pressed += OnPlantPressed;
        Zombie.Pressed += OnZombiePressed;
        BackGround.Modulate = ThemeColors1[0];
        Top1.Modulate = ThemeColors1[1];
        Top2.Modulate = ThemeColors1[2];

        List<Type> types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(HeroModel))).ToList();
        Vector2I position = new Vector2I(1, 1);
        foreach (Type type in types)
        {
            string name = type.Name;
            if (HeroString.GetData(name, "Camp").AsString() != "植物")
            {
                continue;
            }
            HeroSprite hs = HeroSprite.Create(name);
            AddChild(hs);
            hs.Position = new Vector2((position.X-1) * (128+6.4f)+88, (position.Y-1) * 100 + 344);
            position.X++;
            if (position.X > 5)
            {
                position.Y++;
                position.X = 1;
            }
        }
    }

    private async void OnReturnPressed() {
        await SceneManager.ChangeScene(GetTree(), "res://场景/UI对象/主界面.tscn");
    }
    private void OnPlantPressed(){
        BackGround.Modulate = ThemeColors1[0];
        Top1.Modulate = ThemeColors1[1];
        Top2.Modulate = ThemeColors1[2];
    }
    private void OnZombiePressed(){
        BackGround.Modulate = ThemeColors2[0];
        Top1.Modulate = ThemeColors2[1];
        Top2.Modulate = ThemeColors2[2];
    }
}
