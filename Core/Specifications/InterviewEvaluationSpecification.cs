using System.Linq.Expressions;
using Core.Entities;

namespace Core.Specifications;

public class EvaluationByInterviewIdSpecification  : BaseSpecfication<InterviewEvaluation>
{
    public EvaluationByInterviewIdSpecification (Guid interviewId) : base(e => e.InterviewId == interviewId) {}

}