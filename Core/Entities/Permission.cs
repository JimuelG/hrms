namespace Core.Entities;
public class Permission : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Module { get; set; } = default!;
    public string Description { get; set; } = default!;
}