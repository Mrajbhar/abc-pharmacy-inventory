using System.ComponentModel.DataAnnotations;

namespace AbcPharmacy.Api.Contracts;

public class CreateSaleRequest
{
    [Required(ErrorMessage = "Medicine is required.")]
    public Guid? MedicineId { get; set; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, 10_000, ErrorMessage = "Quantity must be between 1 and 10,000.")]
    public int? Quantity { get; set; }

    [StringLength(100, ErrorMessage = "Customer name cannot be longer than 100 characters.")]
    public string? CustomerName { get; set; }
}

public record SaleResponse(
    Guid Id,
    Guid MedicineId,
    string MedicineName,
    string Brand,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    string? CustomerName,
    DateTime SoldAtUtc);
