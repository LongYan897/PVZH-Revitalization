using Hero.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Variable;

namespace Hero;
public class HeroVariable<T>(HeroModel hero,string sign,T BaseValue) : Variable<T>(sign,BaseValue)
{
    public HeroModel hero { get; init; } = hero;
    public override int Set(T value, VariableReason reason)
    {
        return base.Set(value, reason);
    }
}
