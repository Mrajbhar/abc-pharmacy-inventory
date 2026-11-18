using AbcPharmacy.Api.Contracts;
using AbcPharmacy.Api.Data;
using AbcPharmacy.Api.Infrastructure;
using AbcPharmacy.Api.Models;

namespace AbcPharmacy.Api.Services;

public interface ISaleService
{
    /// <summary>All sales, newest first.</summary>
    Task<IReadOnlyList<SaleResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SaleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Records a sale and reduces the medicine's stock by the quantity sold.</summary>
    Task<SaleResponse> CreateAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);
}

public sealed class SaleService(
    IMedicineRepository medicines,
    ISaleRepository sales,
    InventoryRules rules,
    TimeProvider clock,
    ILogger<SaleService> logger) : ISaleService
{
    public async Task<IReadOnlyList<SaleResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var all = await sales.GetAllAsync(cancellationToken);
        return all
            .OrderByDescending(s => s.SoldAtUtc)
            .Select(ToResponse)
            .ToList();
    }

    public async Task<SaleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var all = await sales.GetAllAsync(cancellationToken);
        var sale = all.FirstOrDefault(s => s.Id == id)
            ?? throw new NotFoundException($"Sale '{id}' was not found.");
        return ToResponse(sale);
    }

    public async Task<SaleResponse> CreateAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        var medicineId = request.MedicineId!.Value;
        var quantity = request.Quantity!.Value;

        // Step 1 — check and reduce stock as ONE atomic operation inside the store's lock.
        // Doing the check inside the lock means two simultaneous sales can never both pass the
        // check and oversell. If a rule fails we throw, and the store writes nothing.
        var medicine = await medicines.UpdateAsync(medicineId, m =>
        {
            if (rules.IsExpired(m))
            {
                throw new BusinessRuleException($"'{m.FullName}' expired on {m.ExpiryDate:yyyy-MM-dd} and cannot be sold.");
            }

            if (m.Quantity < quantity)
            {
                throw new BusinessRuleException(
                    $"Only {m.Quantity} unit(s) of '{m.FullName}' in stock; cannot sell {quantity}.");
            }

            m.Quantity -= quantity;
            m.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        }, cancellationToken) ?? throw new NotFoundException($"Medicine '{medicineId}' was not found.");

        // Step 2 — record the sale, snapshotting name, brand and price as they were at the time of sale.
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            MedicineId = medicine.Id,
            MedicineName = medicine.FullName,
            Brand = medicine.Brand,
            Quantity = quantity,
            UnitPrice = medicine.Price,
            TotalAmount = Money.Round2(medicine.Price * quantity),
            CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim(),
            SoldAtUtc = clock.GetUtcNow().UtcDateTime,
        };

        try
        {
            // The stock is already reduced; don't let a cancelled request leave it unrecorded.
            await sales.AddAsync(sale, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Two JSON files cannot share a real transaction, so if writing the sale fails
            // we compensate by putting the stock back.
            logger.LogError(ex, "Recording sale failed; restoring {Quantity} unit(s) to medicine {MedicineId}", quantity, medicineId);
            await medicines.UpdateAsync(medicineId, m => m.Quantity += quantity, CancellationToken.None);
            throw;
        }

        return ToResponse(sale);
    }

    private static SaleResponse ToResponse(Sale s) => new(
        s.Id, s.MedicineId, s.MedicineName, s.Brand, s.Quantity, s.UnitPrice, s.TotalAmount, s.CustomerName, s.SoldAtUtc);
}
