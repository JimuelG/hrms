using Core.Common;

namespace Core.DTOs.Reports;

public sealed record CelebrationDto(string Name, int Month, int Day, DateOnly Next, int? Years);

public sealed record WorkforceReportDto(
    DateOnly Date,
    int Total,
    IReadOnlyList<CountRow> ByDepartment,
    IReadOnlyList<CountRow> ByBranch,
    IReadOnlyList<CountRow> ByPosition,
    IReadOnlyList<CountRow> ByEmployentType,
    IReadOnlyList<CountRow> ByStatus,
    IReadOnlyList<CountRow> Tenure,
    IReadOnlyList<CelebrationDto> Birthdays,
    IReadOnlyList<CelebrationDto> Anniversaries);

public sealed record CsvFile(string FileName, string Content);
