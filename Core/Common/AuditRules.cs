using System.Text.Json;

namespace Core.Common;

public sealed record FieldChange(string Field, string? Old, string? New, bool Hidden);
public static class AuditRules
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;
    public const int MaxRangeDays = 366;
    public const string HiddenValue = "[hidden]";

    public static readonly string[] HiddenEntities =
    [
        "EmployeeCompensation",
        "EmployeeIdentifiers",
        "IdentifierAccessLog",
        "PayrollResultRecord",
        "PayrollAdjustmentRecord",
        "RecurringPayrollItem",
        "LoadRepayment",
        "PayrollLoanApplication"
    ];

    private static readonly string[] SensitiveFragments = ["salary", "password", "hash", "token", "secret", "protected", "stamp", "bankaccount"];

    private static readonly HashSet<string> NoiseFields = new(StringComparer.OrdinalIgnoreCase) { "RowVersion" };

    public static bool IsHiddenEntity(string entity) =>
        HiddenEntities.Contains(entity, StringComparer.OrdinalIgnoreCase);

    public static bool IsSensitiveField(string field)
    {
        var f = field.ToLowerInvariant();
        return SensitiveFragments.Any(f.Contains);
    }

    public static (int Page, int PageSize) ClampPage(int page, int pageSize) =>
        (Math.Max(1, page), pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize));
    
    public static string? CheckRange(DateOnly? from, DateOnly? to)
    {
        if (from is DateOnly f && to is DateOnly t)
        {
            if (t < f) return "The end date can't be before the start date.";
            if (t.DayNumber - f.DayNumber + 1 > MaxRangeDays) return $"Choose at most {MaxRangeDays} days at a time.";
        }
        return null;
    }

    public static IReadOnlyList<FieldChange> Changes(string entity, string? oldJson, string? newJson, Func<string, Type?>? fieldType = null)
    {
        if (IsHiddenEntity(entity)) return [];

        var before = Parse(oldJson);
        var after = Parse(newJson);
        var isModified = before.Count > 0 && after.Count > 0;

        var result = new List<FieldChange>();
        foreach (var field in before.Keys.Union(after.Keys, StringComparer.OrdinalIgnoreCase)
                        .Where(f => !NoiseFields.Contains(f))
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            before.TryGetValue(field, out var o);
            after.TryGetValue(field, out var n);
            if (isModified && o == n && o != HiddenValue) continue;

            var hidden = IsSensitiveField(field);
            var type = fieldType?.Invoke(field);

            result.Add(new FieldChange(field,
                hidden && o is not null ? HiddenValue : FormatValue(type, o),
                hidden && n is not null ? HiddenValue : FormatValue(type, n),
                hidden));
        }

        return result;
    }

    private static Dictionary<string, string?> Parse(string? json)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json)) return values;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return values;
            foreach (var p in doc.RootElement.EnumerateObject())
                values[p.Name] = p.Value.ValueKind switch
                {
                    JsonValueKind.String => p.Value.GetString(),
                    JsonValueKind.Null => null,
                    _ => p.Value.GetRawText()
                };
        }
        catch (JsonException)
        {
            
        }
        return values;
    }

    public static string? FormatValue(Type? type, string? raw)
    {
        if (raw is null || type is null) return raw;
        var t = Nullable.GetUnderlyingType(type) ?? type;
        if (!t.IsEnum || !long.TryParse(raw, out var number)) return raw;

        var name = Enum.ToObject(t, number).ToString();
        return name == number.ToString() ? raw : name;
    }

    public static object? Redact(string field, object? value) =>
        value is not null && IsSensitiveField(field) ? HiddenValue : value;
}