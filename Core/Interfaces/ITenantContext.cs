namespace Core.Interfaces;
public interface ITenantContext
{
    Guid? TenantId { get; }
    bool HasTenant { get; }
    Guid RequiredTenantId { get; }
}