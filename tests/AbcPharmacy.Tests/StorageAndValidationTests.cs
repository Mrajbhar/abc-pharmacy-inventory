using System.ComponentModel.DataAnnotations;
using System.Globalization;
using AbcPharmacy.Api.Contracts;
using AbcPharmacy.Api.Data;
using AbcPharmacy.Api.Infrastructure;
using AbcPharmacy.Api.Models;
using AbcPharmacy.Tests.TestSupport;

namespace AbcPharmacy.Tests;

public class JsonFileStoreTests : IDisposable
{
    private readonly TestApp _app = new();

    [Fact]
    public async Task A_missing_file_reads_as_an_empty_list()
    {
        Assert.False(_app.MedicineStore.FileExists);
        Assert.Empty(await _app.MedicineStore.ReadAllAsync());
    }

    [Fact]
    public async Task Saved_data_is_readable_by_a_new_store_instance_after_a_restart()
    {
        var medicine = await _app.AddMedicineAsync("Omeprazole 20mg");

        var reopened = new JsonFileStore<Medicine>(_app.MedicineStore.FilePath);
        var stored = Assert.Single(await reopened.ReadAllAsync());

        Assert.Equal(medicine.Id, stored.Id);
        Assert.Equal("Omeprazole 20mg", stored.FullName);
        Assert.Equal(medicine.ExpiryDate, stored.ExpiryDate);
    }

    [Fact]
    public async Task A_failed_update_leaves_the_file_unchanged()
    {
        await _app.AddMedicineAsync(quantity: 5);
        var before = await File.ReadAllTextAsync(_app.MedicineStore.FilePath);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _app.MedicineStore.UpdateAsync<bool>(items =>
        {
            items.Clear();
            throw new InvalidOperationException("boom");
        }));

        Assert.Equal(before, await File.ReadAllTextAsync(_app.MedicineStore.FilePath));
        Assert.False(File.Exists(_app.MedicineStore.FilePath + ".tmp"));
    }

    public void Dispose() => _app.Dispose();
}

public class ValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static bool HasErrorFor(List<ValidationResult> results, string member) =>
        results.Any(r => r.MemberNames.Contains(member));

    [Fact]
    public void An_empty_medicine_request_reports_every_required_field()
    {
        var results = Validate(new CreateMedicineRequest());

        Assert.True(HasErrorFor(results, nameof(CreateMedicineRequest.FullName)));
        Assert.True(HasErrorFor(results, nameof(CreateMedicineRequest.Brand)));
        Assert.True(HasErrorFor(results, nameof(CreateMedicineRequest.ExpiryDate)));
        Assert.True(HasErrorFor(results, nameof(CreateMedicineRequest.Quantity)));
        Assert.True(HasErrorFor(results, nameof(CreateMedicineRequest.Price)));
        Assert.False(HasErrorFor(results, nameof(CreateMedicineRequest.Notes))); // notes are optional
    }

    [Theory]
    [InlineData("12.34", true)]
    [InlineData("12.3", true)]
    [InlineData("12", true)]
    [InlineData("12.345", false)]
    public void Price_allows_at_most_two_decimal_places(string price, bool valid)
    {
        var attribute = new MaxDecimalPlacesAttribute(2);
        Assert.Equal(valid, attribute.IsValid(decimal.Parse(price, CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(-2, false)]
    public void Sale_quantity_must_be_at_least_one(int quantity, bool valid)
    {
        var results = Validate(new CreateSaleRequest { MedicineId = Guid.NewGuid(), Quantity = quantity });
        Assert.Equal(valid, !HasErrorFor(results, nameof(CreateSaleRequest.Quantity)));
    }

    [Theory]
    [InlineData("64.5", "64.50")]
    [InlineData("64", "64.00")]
    [InlineData("10.005", "10.01")]
    public void Money_Round2_always_keeps_exactly_two_decimal_places(string input, string expected)
    {
        var result = Money.Round2(decimal.Parse(input, CultureInfo.InvariantCulture));
        Assert.Equal(expected, result.ToString(CultureInfo.InvariantCulture));
    }
}
