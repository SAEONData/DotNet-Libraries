using SAEON.Logs;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SAEON.AspNet.WebApi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ClaimsAuthorizationAttribute : AuthorizationFilterAttribute
    {
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }

        public ClaimsAuthorizationAttribute() : base() { }

        public ClaimsAuthorizationAttribute(string claimType, string claimValue) : this()
        {
            ClaimType = claimType;
            ClaimValue = claimValue;
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            using (Logger.MethodCall(GetType(), new MethodCallParameters { { "Claim", ClaimType }, { "Value", ClaimValue } }))
            {
                if (actionContext == null) throw new ArgumentNullException(nameof(actionContext));
                var principal = actionContext.RequestContext.Principal as ClaimsPrincipal;
                if (!principal.Identity.IsAuthenticated)
                {
                    Logger.Error("Not Authenticated");
                    actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Not Authenticated");
                    return;
                }
                Logger.Verbose("Claims: {claims}", principal.Claims.Select(i => i.Type + "=" + i.Value));
                if (!(principal.HasClaim(x => x.Type.Equals(ClaimType, StringComparison.CurrentCultureIgnoreCase) && x.Value.Equals(ClaimValue, StringComparison.CurrentCultureIgnoreCase))))
                {
                    Logger.Error("Claims Authorization Failed");
                    actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Claims Authorization Failed");
                    return;
                }
            }
        }
    }
}
