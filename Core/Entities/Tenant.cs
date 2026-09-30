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