namespace Core.Enums;
public enum ApprovalSubjectType
{
    LeaveRequest = 1
}

public enum ApprovalStepKind
{
    Manager = 1,
    Permission = 2
}

public enum ApprovalStepStatus
{
    Waiting = 1,
    Pending = 2,
    Approved = 3,
    Rejected = 4,
    Skipped = 5,
    Voided = 6
}

public enum ApprovalEventType
{
    Submitted = 1,
    StepSkipped = 2,
    StepApproved = 3,
    Rejected = 4,
    Commented = 5,
    Cancelled = 6
}

