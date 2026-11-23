using Battle.Entity;
using Card;
using Hero;
using Hero.Model;

namespace Battle;

public interface IAttackable
{
    public FighterCardModel InferAsCard()
    {
        return this is Fighter fighter ? fighter.Model : null;
    }
    public HeroModel InferAsHero()
    {
        return this is HeroInstance instance ? instance.Hero : null;
    }
}
