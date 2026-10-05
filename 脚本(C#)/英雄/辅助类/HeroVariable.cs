using Hero.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Variable;

namespace Hero;
public class HeroVariable<T>(HeroModel hero,string sign,T baseValue) : Variable<T>(sign,baseValue)
{
    public HeroModel hero { get; init; } = hero;
}

public class HeroIntVariable(HeroModel hero,string sign,int baseValue) : HeroVariable<int>(hero,sign,baseValue)
{
    public async Task<int> Remove(int value,VariableReason reason)
    {
        return base.Set(Current - value,reason);
    }
    public async Task<int> Add(int value,VariableReason reason)
    {
        return base.Set(Current + value, reason);
    }
}