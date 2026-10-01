using Card;
using Controller;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hero.Model;
public class HeroModel
{
    public List<CardModel> SuperPower { get; private set; }
    protected async Task PlayAnimation(string animName)
    {
        var node = SpineHandler.Get();
        
    }
}
