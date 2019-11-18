using SAEON.AspNet.Common;
using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SAEON.AspNet.WebApi
{
    //[Obsolete("ClientAuthorizationAttribute is obsolete", true)]
    [Obsolete]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ClientAuthorizationAttribute : AuthorizationFilterAttribute
    {
        private List<string> Clients { get; } = new List<string>();

        public ClientAuthorizationAttribute() : base() { }

        public ClientAuthorizationAttribute(string client) : this()
        {
            if (!Clients.Any(i => i == client))
            {
                Clients.Add(client);
            }
        }

        public ClientAuthorizationAttribute(params string[] clients) : this()
        {
            foreach (var client in clients)
            {
                if (!Clients.Any(i => i == client))
                {
                    Clients.Add(client);
                }
            }
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            using (Logging.MethodCall(GetType(), new ParameterList { { "Client", Clients } }))
            {
                base.OnAuthorization(actionContext);
                var principal = actionContext.RequestContext.Principal as ClaimsPrincipal;
                bool found = false;
                foreach (var client in Clients)
                {
                    Logging.Verbose("Client: {client} Claims: {claims}", client, principal.Claims.Select(i => i.Type + "=" + i.Value));
                    if (principal.HasClaim(x => x.Type == Constants.ClaimClientId && x.Value == client))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Logging.Error("Client Authorization Failed");
                    actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Client Authorization Failed");
                    return;
                }
            }
        }
    }
}
