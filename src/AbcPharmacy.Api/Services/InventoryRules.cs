using AbcPharmacy.Api.Models;
using AbcPharmacy.Api.Options;
using Microsoft.Extensions.Options;

namespace AbcPharmacy.Api.Services;

/// <summary>
/// The colour-coding rules from the requirements, kept in one place on the server:
/// <list type="bullet">
///   <item>Red: expiry date less than <see cref="InventoryOptions.ExpiryWarningDays"/> (30) days away — includes already expired.</item>
///   <item>Yellow: quantity in stock less than <see cref="InventoryOptions.LowStockThreshold"/> (10).</item>
/// </list>
/// "Today" comes from <see cref="TimeProvider"/>, so tests can pin the date.
/// </summary>
public sealed class InventoryRules(IOptions<InventoryOptions> options, TimeProvider clock)
{
    public DateOnly Today => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    public int DaysToExpiry(Medicine medicine) => medicine.ExpiryDate.DayNumber - Today.DayNumber;

    public bool IsExpired(Medicine medicine) => DaysToExpiry(medicine) < 0;

    public bool IsExpiringSoon(Medicine medicine) => DaysToExpiry(medicine) < options.Value.ExpiryWarningDays;

    public bool IsLowStock(Medicine medicine) => medicine.Quantity < options.Value.LowStockThreshold;
}
