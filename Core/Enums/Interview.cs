namespace Core.Enums;
public enum InterviewType
{
    Screening = 1,
    HrInterview = 2,
    DepartmentInterview = 3,
    FinalInterview = 4
}

public enum InterviewStatus
{
    Scheduled = 1,
    Completed = 2,
    Cancelled = 3,
    NoShow = 4
}

public enum InterviewRecommendation
{
    StrongNo = 1,
    No = 2,
    Maybe = 3,
    Yes = 4,
    StrongYes = 5
}