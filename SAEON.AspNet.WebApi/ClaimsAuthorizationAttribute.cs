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
    public class ClaimsAuthorizationAttribute : AuthorizationFilterAttribute
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
            using (Logging.MethodCall(GetType(), new ParameterList { { "Claim", ClaimType }, { "Value", ClaimValue } }))
            {
                base.OnAuthorization(actionContext);
                var principal = actionContext.RequestContext.Principal as ClaimsPrincipal;
                if (!principal.Identity.IsAuthenticated)
                {
                    Logging.Error("Not Authenticated");
                    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
                    actionContext.Response.ReasonPhrase = "Not Authenticated";
                    return;
                }
                Logging.Verbose("Claims: {claims}", principal.Claims.Select(i => i.Type + "=" + i.Value));
                if (!(principal.HasClaim(x => x.Type.Equals(ClaimType,StringComparison.CurrentCultureIgnoreCase) && x.Value.Equals(ClaimValue,StringComparison.CurrentCultureIgnoreCase))))
                {
                    Logging.Error("Claims Authorization Failed");
                    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden);
                    actionContext.Response.ReasonPhrase = "Claims Authorization Failed";
                    return;
                }
            }
        }
    }
}
