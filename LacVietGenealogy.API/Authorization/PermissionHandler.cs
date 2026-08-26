using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace LacVietGenealogy.API.Authorization
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // Lấy tất cả các claim có tên là "permissions" trong User của HttpContext
            var permissions = context.User.FindAll("permissions").Select(c => c.Value);

            if (permissions.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}