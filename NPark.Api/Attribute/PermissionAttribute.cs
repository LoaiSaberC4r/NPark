using Microsoft.AspNetCore.Authorization;

namespace NPark.Api.Attribute
{
    public class PermissionAttribute : AuthorizeAttribute
    {
        private const string POLICY_PREFIX = "PERMISSION_";

        public PermissionAttribute(string permissions)
        {
            Policy = POLICY_PREFIX + permissions;
        }
    }
}