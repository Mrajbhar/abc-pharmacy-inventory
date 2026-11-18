using AbcPharmacy.Api.Models;
using AbcPharmacy.Tests.TestSupport;

namespace AbcPharmacy.Tests;

public class InventoryRulesTests : IDisposable
{
    private readonly TestApp _app = new();

    private static Medicine With(int daysToExpiry = 365, int quantity = 50) => new()
    {
        ExpiryDate = TestApp.Today.AddDays(daysToExpiry),
        Quantity = quantity,
    };

    [Theory]
    [InlineData(-10, true)]  
    [InlineData(0, true)]    
    [InlineData(29, true)]  
    [InlineData(30, false)]  
    [InlineData(31, false)]
    public void IsExpiringSoon_is_true_when_fewer_than_30_days_remain(int daysToExpiry, bool expected)
    {
        Assert.Equal(expected, _app.Rules.IsExpiringSoon(With(daysToExpiry: daysToExpiry)));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(9, true)]    
    [InlineData(10, false)]  
    [InlineData(500, false)]
    public void IsLowStock_is_true_when_fewer_than_10_units(int quantity, bool expected)
    {
        Assert.Equal(expected, _app.Rules.IsLowStock(With(quantity: quantity)));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]   
    [InlineData(1, false)]
    public void IsExpired_is_true_only_after_the_expiry_date(int daysToExpiry, bool expected)
    {
        Assert.Equal(expected, _app.Rules.IsExpired(With(daysToExpiry: daysToExpiry)));
    }

    [Fact]
    public void DaysToExpiry_counts_calendar_days_from_today()
    {
        Assert.Equal(12, _app.Rules.DaysToExpiry(With(daysToExpiry: 12)));
        Assert.Equal(-3, _app.Rules.DaysToExpiry(With(daysToExpiry: -3)));
    }

    public void Dispose() => _app.Dispose();
}
