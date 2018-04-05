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
    public class RoleAuthorizationAttribute : AuthorizationFilterAttribute
    {
        private string role;

        public RoleAuthorizationAttribute() : base() { }

        public RoleAuthorizationAttribute(string Role) : this()
        { 
            role = Role;
        } 

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            using (Logging.MethodCall(GetType(), new ParameterList { { "Role", role } }))
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
                Logging.Verbose("Role: {role} Claims: {claims}", role, principal.Claims.Select(i => i.Type + "=" + i.Value));
                if (!(principal.HasClaim(x => x.Type.Equals("role",StringComparison.CurrentCultureIgnoreCase) && x.Value.Equals(role,StringComparison.CurrentCultureIgnoreCase))))
                {
                    Logging.Error("Role Authorization Failed");
                    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden);
                    actionContext.Response.ReasonPhrase = "Role Authorization Failed";
                    return;
                }
            }
        }
    }
}
