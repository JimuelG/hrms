using Core.Enums;

namespace Core.Common;

public sealed record BracketRow(decimal From, decimal? To, decimal EmployeeFixed, decimal EmployerFixed, decimal EmployeeRateOnExcess);

public sealed record RuleSetData(
    StatutoryKind Kind,
    StatutoryMethod Method,
    DateOnly EffectiveFrom,
    PayFrequency? Basis,
    decimal EmployeeRate,
    decimal EmployerRate,
    decimal? BaseFloor,
    decimal? BaseCeiling,
    IReadOnlyList<BracketRow> Brackets);

public sealed record StatutoryAmount(decimal Employee, decimal Employer);

public sealed record InForceRules(RuleSetData? Sss, RuleSetData? PhilHealth, RuleSetData? PagIbig, RuleSetData? Tax);

public enum ContributionDeduction { SplitEvenly = 1, SecondPeriodOnly = 2 }