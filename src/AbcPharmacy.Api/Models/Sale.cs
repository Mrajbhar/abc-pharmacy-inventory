namespace AbcPharmacy.Api.Models;


public class Sale
{
    public Guid Id { get; set; }

    public Guid MedicineId { get; set; }

    public string MedicineName { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalAmount { get; set; }

    public string? CustomerName { get; set; }

    public DateTime SoldAtUtc { get; set; }
}
