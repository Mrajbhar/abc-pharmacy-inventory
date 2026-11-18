using AbcPharmacy.Api.Contracts;
using AbcPharmacy.Api.Infrastructure;
using AbcPharmacy.Tests.TestSupport;

namespace AbcPharmacy.Tests;

public class MedicineServiceTests : IDisposable
{
    private readonly TestApp _app = new();

    private static CreateMedicineRequest ValidRequest() => new()
    {
        FullName = "  Cetirizine 10mg Tablets  ",
        Brand = " Okacet ",
        Notes = "   ",
        ExpiryDate = TestApp.Today.AddDays(200),
        Quantity = 6,
        Price = 18.5m,
    };

    [Fact]
    public async Task CreateAsync_saves_a_cleaned_up_medicine_and_returns_its_status_flags()
    {
        var created = await _app.MedicineService.CreateAsync(ValidRequest());

        Assert.Equal("Cetirizine 10mg Tablets", created.FullName);
        Assert.Equal("Okacet", created.Brand);
        Assert.Null(created.Notes);                     // whitespace-only notes are dropped
        Assert.Equal("18.50", created.Price.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.True(created.IsLowStock);                // 6 < 10
        Assert.False(created.IsExpiringSoon);

        var stored = Assert.Single(await _app.Medicines.GetAllAsync());
        Assert.Equal(created.Id, stored.Id);
    }

    [Fact]
    public async Task CreateAsync_rejects_an_expiry_date_in_the_past()
    {
        var request = ValidRequest();
        request.ExpiryDate = TestApp.Today.AddDays(-1);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => _app.MedicineService.CreateAsync(request));

        Assert.Contains("past", error.Message);
        Assert.Empty(await _app.Medicines.GetAllAsync());
    }

    [Fact]
    public async Task GetAllAsync_searches_by_name_ignoring_case_and_sorts_by_name()
    {
        await _app.AddMedicineAsync("Paracetamol 650mg");
        await _app.AddMedicineAsync("Azithromycin 500mg");
        await _app.AddMedicineAsync("amoxicillin 250mg");

        var all = await _app.MedicineService.GetAllAsync(search: null);
        var matches = await _app.MedicineService.GetAllAsync(search: "  MG  ");
        var mycin = await _app.MedicineService.GetAllAsync(search: "MYCIN");

        Assert.Equal(new[] { "amoxicillin 250mg", "Azithromycin 500mg", "Paracetamol 650mg" }, all.Select(m => m.FullName).ToArray());
        Assert.Equal(3, matches.Count);
        Assert.Equal("Azithromycin 500mg", Assert.Single(mycin).FullName);
        Assert.Empty(await _app.MedicineService.GetAllAsync(search: "ibuprofen"));
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFound_for_an_unknown_id()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _app.MedicineService.GetByIdAsync(Guid.NewGuid()));
    }

    public void Dispose() => _app.Dispose();
}
