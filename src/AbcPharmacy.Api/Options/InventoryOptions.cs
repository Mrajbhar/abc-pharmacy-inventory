namespace AbcPharmacy.Api.Options;


public class InventoryOptions
{
    public const string SectionName = "Inventory";

    public int ExpiryWarningDays { get; set; } = 30;

    public int LowStockThreshold { get; set; } = 10;
}
