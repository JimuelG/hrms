using Microsoft.AspNetCore.Authorization;

namespace API.Authorization;
public class PlatformAdminOnlyAttribute() : AuthorizeAttribute(policy: "PlatformAdmin") {}