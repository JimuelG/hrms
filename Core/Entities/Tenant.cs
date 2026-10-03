using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class Tenant : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public TenantStatus Status { get; set; } = TenantStatus.Active;
}

public enum TenantStatus
{
    Active = 1,
    Suspended = 2,
    Closed = 3
}

public class TenantSettings : BaseEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    public string? CompanyLegalName { get; set; }
    public string? LogoUrl { get; set; }
    public string? Primarycolor { get; set; }

    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    public string TimeZoneId { get; set; } = "UTC";
    public string Currency { get; set; } = "PHP";
    public string DateFormat { get; set; } = "yyyy-MM-dd";

    public WorkingDay WorkingDays { get; set; } =
        WorkingDay.Monday | WorkingDay.Tuesday | WorkingDay.Wednesday | WorkingDay.Thursday | WorkingDay.Friday;
}