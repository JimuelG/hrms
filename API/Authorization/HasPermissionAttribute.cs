using Microsoft.AspNetCore.Authorization;

namespace API.Authorization;
public sealed class HasPermissionAttribute(string permission) 
    : AuthorizeAttribute(policy: $"Permission:{permission}")
{

}