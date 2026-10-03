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
public partial class HeroModel : Control
{
    public List<CardModel> SuperPower { get; private set; }
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
    protected async Task PlayAnimation(string animName,bool loop = false)
    {
        var node = GetNode<SpineHandler>("%动画");
        if (node != null)
        {
            node.SetAnimation(0,animName,loop);
        }
    }
}
