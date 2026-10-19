using Core.Common;

namespace Tests;

public class EffectiveDatingTests
{
    private sealed record Item(DateOnly From);
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    private static readonly Item[] Items = [new(D(2026, 11, 1)), new(D(2024, 1, 1)), new(D(2025, 7, 1))];

    [Fact] public void The_Newest_Started_Item_Wins() =>
        Assert.Equal(D(2025, 7, 1), EffectiveDating.PickInForce(Items, i => i.From, D(2026, 10, 31))!.From);

    [Fact] public void An_Item_Applies_On_Its_Effective_Date() =>
        Assert.Equal(D(2026, 11, 1), EffectiveDating.PickInForce(Items, i => i.From, D(2026, 11, 1))!.From);

    [Fact] public void Before_The_First_Item_Nothing_Applies() =>
        Assert.Null(EffectiveDating.PickInForce(Items, i => i.From, D(2023, 12, 31)));

    [Fact] public void An_Empty_List_Has_Nothing_In_Force() =>
        Assert.Null(EffectiveDating.PickInForce(Array.Empty<Item>(), i => i.From, D(2026, 1, 1)));
}