namespace Core.Common;
public static class EffectiveDating
{
    public static T? PickInForce<T>(IEnumerable<T> items, Func<T, DateOnly> effectuveForm, DateOnly date) where T : class =>
        items.Where(i => effectuveForm(i) <= date).OrderByDescending(effectuveForm).FirstOrDefault();
}