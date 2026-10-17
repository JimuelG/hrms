namespace Core.Common;
public sealed class ConcurrencyConflictException(string message, Exception? inner = null) : Exception(message, inner);