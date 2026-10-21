namespace Core.Common;

public enum IdentifierField { Tin = 1, Sss= 2, PhilHealth = 3, PagIbig = 4, BankAccountNumber = 5 }
public enum IdentifierAction { Revealed = 1, Updated = 2, Cleared = 3 }
public static class IdentifierRules
{
    public static string Label(IdentifierField f) => f switch
    {
        IdentifierField.Tin => "TIN",
        IdentifierField.Sss => "SSS number",
        IdentifierField.PhilHealth => "PhilHealth number",
        IdentifierField.PagIbig => "Pag-IBIG number",
        _ => "Bank account number"
    };

    private static (Func<int, bool> Ok, string Describe) Length(IdentifierField f) => f switch
    {
        IdentifierField.Tin => (n => n is 9 or 12, "9 or 12"),
        IdentifierField.Sss => (n => n == 10, "10"),
        IdentifierField.PhilHealth => (n => n == 12, "12"),
        IdentifierField.PagIbig => (n => n == 12, "12"),
        _ => (n => n is >= 5 and <= 20, "5 to 20")
    };

    public static (string? Value, string? Error) Check(IdentifierField field, string? raw)
    {
        var s = new string((raw ?? "").Where(c => c is not (' ' or '-' or '.')).ToArray());
        if (s.Length == 0) return (null, $"Enter the {Label(field)}, or clear it.");
        if (!s.All(char.IsAsciiDigit)) return (null, $"{Label(field)} can contain digits only (spaces and dashes are fine).");

        var (ok, describe) = Length(field);
        return ok(s.Length) ? (s, null) : (null, $"{Label(field)} should have {describe} digits.");
    }

    public static string? Mask(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return value.Length <= 4 ? new string('*', value.Length) : new string('*', value.Length - 4) + value[^4..];
    }
}