namespace Card;

public static class CardExtension
{
    public static T Switch<T>(this Camp camp, T objPlant, T objZombie)
    {
        return camp.Plant() ? objPlant : objZombie;
    }
    public static bool Plant(this Camp camp) => camp == Camp.Plant;
    public static bool Zombie(this Camp camp) => camp == Camp.Zombie;
}
