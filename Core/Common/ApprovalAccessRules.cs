using Core.Enums;

namespace Core.Common;
public static class ApprovalAccessRules
{
    public sealed record Viewer(Guid UserId, Guid? EmployeeId, IReadOnlySet<string> Permissions);

    public static ApprovalAccess Evaluate(
        Viewer viewer, Guid requesterEmployeeId, ApprovalStatus requestStatus, IReadOnlyList<StepState> steps)
    {
        var isRequester = viewer.EmployeeId == requesterEmployeeId;

        var participates = steps.Any(s => IsAddressedTo(s, viewer));
        var canDecideNow = requestStatus == ApprovalStatus.Pending && !isRequester
            && steps.Any(s => s.Status == ApprovalStepStatus.Pending && IsAddressedTo(s, viewer));
        
        return new ApprovalAccess(isRequester, participates, isRequester || participates, canDecideNow);
    }

    public static bool IsAddressedTo(StepState step, Viewer viewer)
    {
        if (step.Status == ApprovalStepStatus.Skipped) return false;

        return step.Kind switch
        {
            ApprovalStepKind.Manager => step.ApproverEmployeeId is Guid a && a == viewer.EmployeeId,
            ApprovalStepKind.Permission => step.Permission is string p && viewer.Permissions.Contains(p),
            _ => false
        };
    }

    public sealed record NoteCheck(string? Note, string? Error);

    public static NoteCheck CheckNote(ApprovalEventType type, string? note, int maxLength = 500)
    {
        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        if (trimmed is null && type is ApprovalEventType.Rejected or ApprovalEventType.Commented)
            return new(null, type == ApprovalEventType.Rejected
                ? "A reason is required when rejecting a request."
                : "Write a comment first.");

        if (trimmed is not null && trimmed.Length > maxLength)
            return new(null, $"Notes can be at most {maxLength} characters.");

        return new(trimmed, null);
    }
}