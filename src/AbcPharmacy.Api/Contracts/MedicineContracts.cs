using System.ComponentModel.DataAnnotations;
using AbcPharmacy.Api.Infrastructure;

namespace AbcPharmacy.Api.Contracts;


public class CreateMedicineRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters.")]
    public string? FullName { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot be longer than 1000 characters.")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "Expiry date is required.")]
    public DateOnly? ExpiryDate { get; set; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(0, 1_000_000, ErrorMessage = "Quantity must be between 0 and 1,000,000.")]
    public int? Quantity { get; set; }

    [Required(ErrorMessage = "Price is required.")]
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Price must be between 0.01 and 1,000,000.")]
    [MaxDecimalPlaces(2)]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Brand is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Brand must be between 1 and 100 characters.")]
    public string? Brand { get; set; }
}

public record MedicineResponse(
    Guid Id,
    string FullName,
    string? Notes,
    DateOnly ExpiryDate,
    int Quantity,
    decimal Price,
    string Brand,
    int DaysToExpiry,
    bool IsExpired,
    bool IsExpiringSoon,
    bool IsLowStock);
