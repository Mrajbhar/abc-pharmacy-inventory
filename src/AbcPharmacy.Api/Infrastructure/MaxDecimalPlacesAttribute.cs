using System.ComponentModel.DataAnnotations;

namespace AbcPharmacy.Api.Infrastructure;


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MaxDecimalPlacesAttribute : ValidationAttribute
{
    public MaxDecimalPlacesAttribute(int places)
    {
        Places = places;
        ErrorMessage = $"{{0}} can have at most {places} decimal places.";
    }

    public int Places { get; }

    public override bool IsValid(object? value) =>
        value is not decimal number || decimal.Round(number, Places) == number;
}
