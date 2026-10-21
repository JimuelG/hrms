using Core.Common;
using Core.Interfaces;

namespace Core.Entities;
public class EmployeeIdentifiers : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }

    public string? TinProtected { get; set; }
    public string? SssProtected { get; set; }
    public string? PhilHealthProtected { get; set; }
    public string? PagIbigProtected { get; set; }
    public string? BankAccountNumberProtected { get; set; }

    public string? BankName { get; set; }
    public string? BankAccountName { get; set; }

    public Guid UpdatedByUserId { get; set; }
}

public class IdentifierAccessLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid UserId { get; set; }
    public IdentifierField Field { get; set; }
    public IdentifierAction Action { get; set; }
}