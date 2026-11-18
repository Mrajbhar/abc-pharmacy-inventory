using AbcPharmacy.Api.Infrastructure;
using AbcPharmacy.Api.Models;
using AbcPharmacy.Api.Options;
using Microsoft.Extensions.Options;

namespace AbcPharmacy.Api.Data;


public sealed class DataSeeder(
    JsonFileStore<Medicine> medicineStore,
    TimeProvider clock,
    IOptions<DataStoreOptions> options,
    ILogger<DataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.SeedSampleData || medicineStore.FileExists)
        {
            return;
        }

        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var now = clock.GetUtcNow().UtcDateTime;

        Medicine Make(string name, string brand, int daysToExpiry, int quantity, decimal price, string notes) => new()
        {
            Id = Guid.NewGuid(),
            FullName = name,
            Brand = brand,
            ExpiryDate = today.AddDays(daysToExpiry),
            Quantity = quantity,
            Price = Money.Round2(price),
            Notes = notes,
            CreatedAtUtc = now,
        };

        var samples = new List<Medicine>
        {
            Make("Paracetamol 650mg Tablets", "Dolo", 420, 120, 30.50m, "Fever and mild pain. Strip of 15."),
            Make("Amoxicillin 500mg Capsules", "Mox", 20, 45, 95.00m, "Antibiotic. Prescription only."),
            Make("Cetirizine 10mg Tablets", "Okacet", 300, 6, 18.75m, "Antihistamine for allergies."),
            Make("Ibuprofen 400mg Tablets", "Brufen", 12, 4, 42.00m, "Expiring soon AND low stock."),
            Make("Azithromycin 500mg Tablets", "Azithral", 180, 60, 119.90m, "Antibiotic. Prescription only."),
            Make("Omeprazole 20mg Capsules", "Omez", 29, 80, 56.40m, "Boundary case: 29 days to expiry (red)."),
            Make("Vitamin D3 60000 IU Capsules", "Uprise-D3", 365, 9, 125.00m, "Boundary case: 9 in stock (yellow)."),
            Make("Metformin 500mg Tablets", "Glycomet", 30, 10, 22.10m, "Boundary case: 30 days and 10 in stock (no colour)."),
            Make("Cough Syrup 100ml", "Benadryl", -5, 15, 110.00m, "Already expired. Cannot be sold."),
        };

        await medicineStore.UpdateAsync(items =>
        {
            items.AddRange(samples);
            return true;
        }, cancellationToken);

        logger.LogInformation("Seeded {Count} sample medicines into {Path}", samples.Count, medicineStore.FilePath);
    }
}
