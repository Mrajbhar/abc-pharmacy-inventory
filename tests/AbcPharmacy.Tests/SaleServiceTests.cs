using AbcPharmacy.Api.Contracts;
using AbcPharmacy.Api.Infrastructure;
using AbcPharmacy.Tests.TestSupport;

namespace AbcPharmacy.Tests;

public class SaleServiceTests : IDisposable
{
    private readonly TestApp _app = new();

    private static CreateSaleRequest Sell(Guid medicineId, int quantity, string? customer = null) =>
        new() { MedicineId = medicineId, Quantity = quantity, CustomerName = customer };

    [Fact]
    public async Task CreateAsync_reduces_stock_and_records_a_snapshot_of_the_sale()
    {
        var medicine = await _app.AddMedicineAsync("Paracetamol 650mg", quantity: 20, price: 30.50m);

        var sale = await _app.SaleService.CreateAsync(Sell(medicine.Id, 3, "  Ravi  "));

        Assert.Equal(17, (await _app.Medicines.GetByIdAsync(medicine.Id))!.Quantity);
        Assert.Equal("Paracetamol 650mg", sale.MedicineName);
        Assert.Equal(30.50m, sale.UnitPrice);
        Assert.Equal(91.50m, sale.TotalAmount);
        Assert.Equal("Ravi", sale.CustomerName);
        Assert.Equal(sale.Id, Assert.Single(await _app.Sales.GetAllAsync()).Id);
    }

    [Fact]
    public async Task CreateAsync_can_sell_the_last_units_in_stock()
    {
        var medicine = await _app.AddMedicineAsync(quantity: 4);

        await _app.SaleService.CreateAsync(Sell(medicine.Id, 4));

        Assert.Equal(0, (await _app.Medicines.GetByIdAsync(medicine.Id))!.Quantity);
    }

    [Fact]
    public async Task CreateAsync_refuses_to_sell_more_than_is_in_stock_and_changes_nothing()
    {
        var medicine = await _app.AddMedicineAsync(quantity: 4);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => _app.SaleService.CreateAsync(Sell(medicine.Id, 5)));

        Assert.Contains("Only 4", error.Message);
        Assert.Equal(4, (await _app.Medicines.GetByIdAsync(medicine.Id))!.Quantity);
        Assert.Empty(await _app.Sales.GetAllAsync());
    }

    [Fact]
    public async Task CreateAsync_refuses_to_sell_an_expired_medicine()
    {
        var medicine = await _app.AddMedicineAsync(quantity: 10, daysToExpiry: -1);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => _app.SaleService.CreateAsync(Sell(medicine.Id, 1)));

        Assert.Contains("expired", error.Message);
        Assert.Equal(10, (await _app.Medicines.GetByIdAsync(medicine.Id))!.Quantity);
    }

    [Fact]
    public async Task CreateAsync_throws_NotFound_for_an_unknown_medicine()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _app.SaleService.CreateAsync(Sell(Guid.NewGuid(), 1)));
        Assert.Empty(await _app.Sales.GetAllAsync());
    }

    [Fact]
    public async Task Simultaneous_sales_never_oversell()
    {
        var medicine = await _app.AddMedicineAsync(quantity: 10);

        // 25 sales of 1 unit fired at the same time against 10 units of stock.
        var attempts = Enumerable.Range(0, 25)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    await _app.SaleService.CreateAsync(Sell(medicine.Id, 1));
                    return true;
                }
                catch (BusinessRuleException)
                {
                    return false;
                }
            }));
        var results = await Task.WhenAll(attempts);

        Assert.Equal(10, results.Count(succeeded => succeeded));
        Assert.Equal(0, (await _app.Medicines.GetByIdAsync(medicine.Id))!.Quantity);
        Assert.Equal(10, (await _app.Sales.GetAllAsync()).Count);
    }

    [Fact]
    public async Task GetAllAsync_returns_newest_sale_first()
    {
        var medicine = await _app.AddMedicineAsync(quantity: 10);
        var first = await _app.SaleService.CreateAsync(Sell(medicine.Id, 1));
        _app.Clock.Now = _app.Clock.Now.AddMinutes(5);
        var second = await _app.SaleService.CreateAsync(Sell(medicine.Id, 1));

        var sales = await _app.SaleService.GetAllAsync();

        Assert.Equal(new[] { second.Id, first.Id }, sales.Select(s => s.Id).ToArray());
    }

    public void Dispose() => _app.Dispose();
}
