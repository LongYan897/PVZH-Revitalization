namespace Card.String;

public static class CardRegistryParams
{
    [UrlParams(false)]
    public static string Camp(CardModel cardModel)
    {
        return cardModel.Camp switch
        {
            Card.Camp.Plant => "植物",
            Card.Camp.Zombie => "僵尸",
            _ => "植物"
        };
    }
    [UrlParams(false)]
    public static string Campifier(CardModel cardModel)
    {
        return cardModel.Camp switch
        {
            Card.Camp.Plant => "株",
            Card.Camp.Zombie => "只",
            _ => "株"
        };
    }
    [UrlParams(false)]
    public static string AntiCamp(CardModel cardModel)
    {
        return cardModel.Camp switch
        {
            Card.Camp.Zombie => "植物",
            Card.Camp.Plant => "僵尸",
            _ => "植物"
        };
    }
    [UrlParams(false)]
    public static string AntiCampifier(CardModel cardModel)
    {
        return cardModel.Camp switch
        {
            Card.Camp.Zombie => "株",
            Card.Camp.Plant => "只",
            _ => "株"
        };
    }
}
