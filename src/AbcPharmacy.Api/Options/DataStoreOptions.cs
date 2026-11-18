namespace AbcPharmacy.Api.Options;


public class DataStoreOptions
{
    public const string SectionName = "DataStore";

    public string DataDirectory { get; set; } = "App_Data";

    public bool SeedSampleData { get; set; } = true;
}
