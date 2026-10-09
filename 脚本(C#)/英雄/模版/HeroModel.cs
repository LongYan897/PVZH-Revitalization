using Card;
using Controller;
using Godot;
using Hero.String;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Hero.Model;
public partial class HeroModel
{
    protected HeroModel(string animPath) => AnimPath = animPath;
    /// <summary>
    /// 通过卡牌实例创建英雄实例
    /// </summary>
    /// <param name="animPath">动画路径</param>
    /// <param name="superPower">超能力卡牌实例</param>
    protected HeroModel(string animPath, List<CardModel> superPower)
    {
        AnimPath = animPath;
        SuperPower = superPower;
    }
    /// <summary>
    /// 通过超能力卡牌类列表直接创建实例
    /// </summary>
    /// <param name="animPath">动画路径</param>
    /// <param name="superPower">超能力卡牌的类</param>
    protected HeroModel(string animPath, List<Type> superPower)
    {

    }
    public List<CardModel> SuperPower { get; private set; }
    public string AnimPath;
    /// <summary>
    /// 通过派系和阵营查找所有相关英雄的类
    /// </summary>
    /// <param name="camp"></param>
    /// <param name="category"></param>
    /// <returns></returns>
    public static List<Type> SeekHeroModel(string camp = null,string category = null) 
    {
        List<Type> heroModels = new List<Type>();
        List<Type> types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(HeroModel))).ToList();
        foreach(Type type in types)
        {
            string name = type.Name;
            if (camp == null || HeroString.GetData(name, "Camp").AsString() == camp && (category == null || HeroString.GetData(name, "Category").AsStringArray().Contains(category)))
            {
                heroModels.Add(type);
            }
        }
        return heroModels;
    }
}
