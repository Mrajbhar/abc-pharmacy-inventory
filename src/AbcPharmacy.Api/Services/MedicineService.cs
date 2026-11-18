using AbcPharmacy.Api.Contracts;
using AbcPharmacy.Api.Data;
using AbcPharmacy.Api.Infrastructure;
using AbcPharmacy.Api.Models;

namespace AbcPharmacy.Api.Services;

public interface IMedicineService
{
    /// <summary>All medicines, optionally filtered by a case-insensitive "name contains" search, ordered by name.</summary>
    Task<IReadOnlyList<MedicineResponse>> GetAllAsync(string? search, CancellationToken cancellationToken = default);

    Task<MedicineResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MedicineResponse> CreateAsync(CreateMedicineRequest request, CancellationToken cancellationToken = default);
}

public sealed class MedicineService(
    IMedicineRepository repository,
    InventoryRules rules,
    TimeProvider clock) : IMedicineService
{
    public async Task<IReadOnlyList<MedicineResponse>> GetAllAsync(string? search, CancellationToken cancellationToken = default)
    {
        var medicines = await repository.GetAllAsync(cancellationToken);
        var term = search?.Trim();

        return medicines
            .Where(m => string.IsNullOrEmpty(term) || m.FullName.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(ToResponse)
            .ToList();
    }

    public async Task<MedicineResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var medicine = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Medicine '{id}' was not found.");
        return ToResponse(medicine);
    }

    public async Task<MedicineResponse> CreateAsync(CreateMedicineRequest request, CancellationToken cancellationToken = default)
    {
        // Shape/format checks ([Required], [Range], 2 decimal places, ...) already ran in [ApiController]
        // model validation. Here we only enforce rules that need business context.
        var expiryDate = request.ExpiryDate!.Value;
        if (expiryDate < rules.Today)
        {
            throw new BusinessRuleException("Expiry date cannot be in the past.");
        }

        var medicine = new Medicine
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName!.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            ExpiryDate = expiryDate,
            Quantity = request.Quantity!.Value,
            Price = Money.Round2(request.Price!.Value),
            Brand = request.Brand!.Trim(),
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime,
        };

        await repository.AddAsync(medicine, cancellationToken);
        return ToResponse(medicine);
    }

    private MedicineResponse ToResponse(Medicine m) => new(
        m.Id,
        m.FullName,
        m.Notes,
        m.ExpiryDate,
        m.Quantity,
        m.Price,
        m.Brand,
        DaysToExpiry: rules.DaysToExpiry(m),
        IsExpired: rules.IsExpired(m),
        IsExpiringSoon: rules.IsExpiringSoon(m),
        IsLowStock: rules.IsLowStock(m));
}
