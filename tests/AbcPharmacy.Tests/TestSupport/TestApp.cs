using AbcPharmacy.Api.Data;
using AbcPharmacy.Api.Models;
using AbcPharmacy.Api.Options;
using AbcPharmacy.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AbcPharmacy.Tests.TestSupport;

/// <summary>A clock frozen at a chosen instant, so "today" is predictable in tests.</summary>
public sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

/// <summary>
/// Wires the real services, repositories and JSON stores together against a fresh temp folder,
/// so tests exercise the actual file storage without touching the app's data.
/// </summary>
public sealed class TestApp : IDisposable
{
    public static readonly DateOnly Today = new(2026, 11, 17);

    public TestApp()
    {
        DataDirectory = Path.Combine(Path.GetTempPath(), "abc-pharmacy-tests", Guid.NewGuid().ToString("N"));
        Clock = new TestClock(new DateTimeOffset(Today.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero));

        MedicineStore = new JsonFileStore<Medicine>(Path.Combine(DataDirectory, "medicines.json"));
        SaleStore = new JsonFileStore<Sale>(Path.Combine(DataDirectory, "sales.json"));
        Medicines = new JsonMedicineRepository(MedicineStore);
        Sales = new JsonSaleRepository(SaleStore);
        Rules = new InventoryRules(Microsoft.Extensions.Options.Options.Create(new InventoryOptions()), Clock);
        MedicineService = new MedicineService(Medicines, Rules, Clock);
        SaleService = new SaleService(Medicines, Sales, Rules, Clock, NullLogger<SaleService>.Instance);
    }

    public string DataDirectory { get; }
    public TestClock Clock { get; }
    public JsonFileStore<Medicine> MedicineStore { get; }
    public JsonFileStore<Sale> SaleStore { get; }
    public IMedicineRepository Medicines { get; }
    public ISaleRepository Sales { get; }
    public InventoryRules Rules { get; }
    public MedicineService MedicineService { get; }
    public SaleService SaleService { get; }

    /// <summary>Adds a medicine straight into storage and returns it.</summary>
    public async Task<Medicine> AddMedicineAsync(
        string name = "Paracetamol 500mg", int quantity = 50, int daysToExpiry = 365, decimal price = 25.00m)
    {
        var medicine = new Medicine
        {
            Id = Guid.NewGuid(),
            FullName = name,
            Brand = "Test Brand",
            Quantity = quantity,
            ExpiryDate = Today.AddDays(daysToExpiry),
            Price = price,
            CreatedAtUtc = Clock.GetUtcNow().UtcDateTime,
        };
        await Medicines.AddAsync(medicine);
        return medicine;
    }

    public void Dispose()
    {
        if (Directory.Exists(DataDirectory))
        {
            Directory.Delete(DataDirectory, recursive: true);
        }
    }
}
