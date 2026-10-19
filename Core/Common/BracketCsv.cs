using System.Globalization;
using System.Text.RegularExpressions;

namespace Core.Common;
public static class BracketCsv
{
    public sealed record Result(IReadOnlyList<BracketRow> Rows, string? Error)
    {
        public static Result Fail(string error) => new Result([], error);
    }

    private static readonly Regex Grouped = new (@"^\d{1,3}(,\d{3})+(\.\d+)?$", RegexOptions.Compiled);

    public static Result Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Result.Fail("Paste the table first.");

        var delimiter = text.Contains('\t') ? '\t' : ',';
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var rows = new List<BracketRow>();
        var firstSeen = false;
        
        for (var i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var rowNo = i + 1;
            var cells = lines[i].Split(delimiter).Select(c => c.Trim()).ToArray();

            if (!firstSeen)
            {
                firstSeen = true;
                if (!TryNumber(cells[0], out  _)) continue;
            }
            
            if (cells.Length < 3) return Result.Fail($"Row {rowNo} needs at least 'from', 'to' and an amount.");

            if (!TryNumber(cells[0], out var from)) return Bad(rowNo, cells[0], "starting amount");

            decimal? to = null;
            if (cells[1].Length > 0 && cells[1] != "-")
            {
                if (!TryNumber(cells[1], out var t)) return Bad(rowNo, cells[1], "ending amount");
                to = t;
            }

            if (!TryNumber(cells[2], out var employee)) return Bad(rowNo, cells[2], "amount");

            var employer = 0m;
            if (cells.Length > 3 && cells[3].Length > 0 && !TryNumber(cells[3], out employer))
                return Bad(rowNo, cells[3], "employer amount");

            var rate = 0m;
            if (cells.Length > 4 && cells[4].Length > 0 && !TryRate(cells[4], out rate))
                return Bad(rowNo, cells[4], "rate");

            rows.Add(new BracketRow(from, to, employee, employer, rate));
        }

        return rows.Count == 0 ? Result.Fail("No rows were found in the pasted text.") : new Result(rows, null);

    }   

    private static bool TryRate(string cell, out decimal rate)
    {
        if (cell.EndsWith('%'))
        {
            if (TryNumber(cell[..^1], out var pct)) { rate = pct / 100m; return true; }
            rate = 0m;
            return false;
        }
        return TryNumber(cell, out rate);
    }

    private static bool TryNumber(string cell, out decimal value)
    {
        var s = cell.Trim();
        if (Grouped.IsMatch(s)) s = s.Replace(",", "");
        return decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    private static Result Bad(int rowNo, string cell, string what) =>
        Result.Fail($"Row {rowNo}: '{cell}' isn;t a valid {what}.");
}