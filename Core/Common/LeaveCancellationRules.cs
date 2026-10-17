using Core.Enums;

namespace Core.Common;

public enum LeaveCancelKind
{
    Withdraw = 1,
    CancelApproved = 2
}

public sealed record LeaveCancelDecision(LeaveCancelKind? Kind, string? Error);

public static class LeaveCancellationRules
{
    public static LeaveCancelDecision Decide(ApprovalStatus status, DateOnly startDate, DateOnly today, bool isRequester, bool isHr)
    {
        switch (status)
        {
            case ApprovalStatus.Pending:
                return isRequester
                    ? new(LeaveCancelKind.Withdraw, null)
                    : new(null, "Only the person who made the request can withdraw it. Approvers can reject it instead.");

            case ApprovalStatus.Approved:
                if (isHr && !isRequester) return new(LeaveCancelKind.CancelApproved, null);
                if (isRequester)
                    return today < startDate
                        ? new(LeaveCancelKind.CancelApproved, null)
                        : new(null, "Leave that has started can only be cancelled by HR.");
                return new(null, "You can't cancel this request.");

            case ApprovalStatus.Rejected:
                return new(null, "A rejected request can't be cancelled.");

            default:
                return new(null, "This request is already cancelled.");
        }
    }
}