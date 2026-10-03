using Core.Entities;

namespace Core.Specifications;
public class EmployeeDocumentsByEmployeeSpecification : BaseSpecfication<EmployeeDocument>
{
    public EmployeeDocumentsByEmployeeSpecification(Guid employeeId) 
        : base(d => d.EmployeeId == employeeId)
    {
        AddOrderByDescending(d => d.UploadedAtUtc);
    }
}