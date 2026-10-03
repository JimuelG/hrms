using System.Linq.Expressions;
using Core.Entities;

namespace Core.Specifications;

public class EmergencyContactsByEmployeeSpecification : BaseSpecfication<EmployeeEmergencyContact>
{
    public EmergencyContactsByEmployeeSpecification(Guid employeeId) 
        : base(c => c.EmployeeId == employeeId)
    {
        AddOrderByDescending(c => c.IsPrimary);
    }

}