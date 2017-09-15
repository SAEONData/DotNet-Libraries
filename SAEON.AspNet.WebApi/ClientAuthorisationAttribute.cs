using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SAEON.AspNet.WebAPI
{
    public class ClientAuthorizationAttribute : AuthorizationFilterAttribute
    {
        private string clientId;

        public ClientAuthorizationAttribute() : base() { }

        public ClientAuthorizationAttribute(string ClientId) : this()
        {
            clientId = ClientId;
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            using (Logging.MethodCall(GetType(), new ParameterList { { "ClientId", clientId } }))
            {
                base.OnAuthorization(actionContext);
                var principal = actionContext.RequestContext.Principal as ClaimsPrincipal;
                Logging.Verbose("ClientId: {clientId} Claims: {claims}", clientId, principal.Claims.Select(i => i.Type + "=" + i.Value));
                if (!(principal.HasClaim(x => x.Type == "client_id" && x.Value == clientId)))
                {
                    Logging.Error("Client Authorization Failed");
                    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden);
                    actionContext.Response.ReasonPhrase = "Client Authorization Failed";
                    return;
                }
            }
        }
    }
}
