namespace Core.Common;
public sealed class DuplicateKeyException(string message, Exception? inner = null) : Exception(message, inner);