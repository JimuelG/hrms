namespace Core.Interfaces;
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}