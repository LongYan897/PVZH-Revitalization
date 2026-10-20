using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Card;

public static class CardExtension
{
    public static T Switch<T>(this Camp camp,T objPlant,T objZombie)
    {
        return camp.Plant() ? objPlant : objZombie;
    }
    public static bool Plant(this Camp camp) => camp == Camp.Plant;
    public static bool Zombie(this Camp camp) => camp == Camp.Zombie;
}
