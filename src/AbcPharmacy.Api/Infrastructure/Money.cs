namespace AbcPharmacy.Api.Infrastructure;

public static class Money
{
    
    public static decimal Round2(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero) + 0.00m;
}
