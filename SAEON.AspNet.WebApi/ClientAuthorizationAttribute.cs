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
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ClientAuthorizationAttribute : AuthorizationFilterAttribute
    {
        private List<string> Clients { get; } = new List<string>();

        public ClientAuthorizationAttribute() : base() { }

        public ClientAuthorizationAttribute(string client) : this()
        {
            if (!Clients.Contains(client))
            {
                Clients.Add(client);
            }
        }

        public ClientAuthorizationAttribute(params string[] clients) : this()
        {
            foreach (var client in clients)
            {
                if (!Clients.Contains(client))
                {
                    Clients.Add(client);
                }
            }
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            using (Logging.MethodCall(GetType(), new MethodCallParameters { { "Client", Clients } }))
            {
                if (actionContext == null) throw new ArgumentNullException(nameof(actionContext));
                var principal = actionContext.RequestContext.Principal as ClaimsPrincipal;
                var callerClientId = principal.Claims.FirstOrDefault(i => i.Type == AspNetConstants.ClaimClientId)?.Value;
                Logging.Information("ClientId: {ClientId}", callerClientId);
                bool found = false;
                foreach (var clientId in Clients)
                {
                    Logging.Verbose("Client: {client} Claims: {claims}", clientId, principal.Claims.Select(i => i.Type + "=" + i.Value));
                    if (callerClientId == clientId)
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
