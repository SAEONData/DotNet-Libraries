using System.Security.Claims;

namespace SAEON.AspNet.Auth
{
    public static class ClaimsPrincipalExtensions
    {
        public static string UserId(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public static bool IsAdmin(this ClaimsPrincipal principal)
        {
            return
                principal.Identity.IsAuthenticated &&
                principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier) &&
                principal.IsInRole("admin") || principal.IsInRole("Admin");
        }

        public static string Name(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Name)?.Value;
        }

        public static string Email(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Email)?.Value;
        }
    }
}
