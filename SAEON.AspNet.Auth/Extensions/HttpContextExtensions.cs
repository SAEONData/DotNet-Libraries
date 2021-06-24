#if NET472
using System.Security.Claims;
using System.Web;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace SAEON.AspNet.Auth
{
    public static class HttpContextExtensions
    {
        public static object UserInfo(this HttpContext context)
        {
            var result = new
            {
                BearerToken = context.Request?.GetBearerToken(),
                context.User.Identity.IsAuthenticated,
                UserIsAdmin = context.UserIsAdmin(),
                UserId = context.UserId(),
                UserName = context.UserName(),
                UserEmail = context.UserEmail(),
#if NET472
                Claims = (context.User as ClaimsPrincipal)?.Claims.ToClaimsList()
#else
                Claims = context.User.Claims.ToClaimsList()
#endif
            };
            return result;
        }

        public static string UserId(this HttpContext context)
        {
            return context.User.UserId();
        }

        public static bool UserIsAdmin(this HttpContext context)
        {
            return context.User.IsAdmin();
        }
        public static string UserName(this HttpContext context)
        {
            return context.User.Name();
        }

        public static string UserEmail(this HttpContext context)
        {
            return context.User.Email();
        }
    }
}
