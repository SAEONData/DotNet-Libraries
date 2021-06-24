using System.Security.Claims;
using System.Security.Principal;

namespace SAEON.AspNet.Auth
{
    public static class IPrincipalExtensions
    {
        public static string UserId(this IPrincipal principal)
        {
            return (principal as ClaimsPrincipal).UserId();
        }

        public static bool IsAdmin(this IPrincipal principal)
        {
            return (principal as ClaimsPrincipal).IsAdmin();
        }

        public static string Name(this IPrincipal principal)
        {
            return (principal as ClaimsPrincipal).Name();
        }

        public static string Email(this IPrincipal principal)
        {
            return (principal as ClaimsPrincipal).Email();
        }

    }
}
