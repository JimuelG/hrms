using Core.Interfaces;

namespace Core.Entities;
public class SoftDeletableEntity : BaseEntity, ISoftDelete
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}