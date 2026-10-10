using System.ComponentModel.DataAnnotations;

namespace Core.Common;
public sealed class TimeZoneIdAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) =>
        value is not string s || string.IsNullOrWhiteSpace(s) || LocalClock.IsValid(s)
            ? ValidationResult.Success
            : new ValidationResult("Unknown time zone. Use an IANA name such as Asia/Manila.");
}