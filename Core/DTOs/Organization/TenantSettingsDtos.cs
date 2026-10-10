using System.ComponentModel.DataAnnotations;
using Core.Common;
using Core.Enums;

namespace Core.DTOs.Organization;
public record TenantSettingsDto(
    string? CompanyLegalName,
    string? LogoUrl,
    string? PrimaryColor,
    string? AddressLine,
    string? City,
    string? Country,
    string? ContactEmail,
    string? ContactPhone,
    string TimeZoneId,
    string Currency,
    string DateFormat,
    int WorkingDays,
    DateTime? UpdatedAtUtc);

public sealed class UpdateTenantSettingsDto
{
    [MaxLength(200)]
    public string? CompanyLegalName { get; init; }
    [MaxLength(500)]
    public string? LogoUrl { get; init; }
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Primary color must be a hex value like #1C4ED8")]
    public string? PrimaryColor { get; init; }

    [MaxLength(300)]
    public string? AddressLine { get; init; }
    [MaxLength(100)]
    public string? City { get; init; }
    [MaxLength(100)]
    public string? Country { get; init; }
    [EmailAddress, MaxLength(256)]
    public string? ContactEmail { get; init; }
    [MaxLength(30)]
    public string? ContactPhone { get; init; }
    [Required, MaxLength(100)]
    [TimeZoneId]
    public string TimeZoneId { get; init; } = "UTC";
    [Required, MaxLength(3)]
    public string Currency { get; init; } = "PHP";
    [Required, MaxLength(20)]
    public string DateFormat { get; init; } = "yyyy-MM-dd";
    public int WorkingDays { get; init; }
}
