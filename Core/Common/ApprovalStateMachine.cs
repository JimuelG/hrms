using Core.Enums;

namespace Core.Common;
public static class ApprovalStateMachine
{
    public static StepOutcome Approve(IReadOnlyList<StepState> steps, int stepOrder) =>
        Decide(steps, stepOrder, approve: true);

    public static StepOutcome Reject(IReadOnlyList<StepState> steps, int stepOrder) =>
        Decide(steps, stepOrder, approve: false);

    public static StepOutcome Cancel(IReadOnlyList<StepState> steps)
    {
        if (!AnyPending(steps))
            return StepOutcome.Fail(steps, StatusOf(steps), "Only a pending request can be cancelled.");

        var next = steps.Select(s => IsOpen(s) ? s with { Status = ApprovalStepStatus.Voided } : s).ToList();
        return new StepOutcome(next, ApprovalStatus.Cancelled, null, true, null);
    }

    public static ApprovalStatus StatusOf(IReadOnlyList<StepState> steps)
    {
        if (steps.Any(s => s.Status == ApprovalStepStatus.Rejected)) return ApprovalStatus.Rejected;
        if (steps.Any(s => s.Status is ApprovalStepStatus.Pending or ApprovalStepStatus.Waiting)) return ApprovalStatus.Pending;
        if (steps.Any(s => s.Status == ApprovalStepStatus.Voided)) return ApprovalStatus.Cancelled;
        return steps.Any(s => s.Status == ApprovalStepStatus.Approved) ? ApprovalStatus.Approved : ApprovalStatus.Pending;
    }

    private static StepOutcome Decide(IReadOnlyList<StepState> steps, int stepOrder, bool approve)
    {
        var current = steps.FirstOrDefault(s => s.Status == ApprovalStepStatus.Pending);
        if (current is null)
            return StepOutcome.Fail(steps, StatusOf(steps), "This request has already been decided.");

        if (current.Order != stepOrder)
            return StepOutcome.Fail(steps, ApprovalStatus.Pending, "That step isn't waiting for a decision.");

        if (!approve)
        {
            var rejected = steps.Select(s =>
                s.Order == stepOrder ? s with { Status = ApprovalStepStatus.Rejected }
                : IsOpen(s) ? s with { Status = ApprovalStepStatus.Voided }
                : s).ToList();

            return new StepOutcome(rejected, ApprovalStatus.Rejected, null, true, null);
        }

        var approved = steps.Select(s =>
            s.Order == stepOrder ? s with { Status = ApprovalStepStatus.Approved } : s).ToList();

        var next = approved.Where(s => s.Status == ApprovalStepStatus.Waiting).OrderBy(s => s.Order).FirstOrDefault();
        if (next is null)
            return new StepOutcome(approved, ApprovalStatus.Approved, null, true, null);

        var advanced = approved.Select(s => s.Order == next.Order ? s with { Status = ApprovalStepStatus.Pending } : s).ToList();
        return new StepOutcome(advanced, ApprovalStatus.Pending, next.Order, false, null);
    }

    private static bool AnyPending(IReadOnlyList<StepState> steps) =>
        steps.Any(s => s.Status is ApprovalStepStatus.Pending or ApprovalStepStatus.Waiting);
    
    private static bool IsOpen(StepState s) => s.Status is ApprovalStepStatus.Pending or ApprovalStepStatus.Waiting;
}