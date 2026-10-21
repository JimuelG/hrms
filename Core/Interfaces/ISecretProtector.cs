namespace Core.Interfaces;
public interface ISecretProtector
{
    string Protect(Guid scope, string plainText);
    bool TryUnprotect(Guid scope, string? protectedValue, out string value);
}