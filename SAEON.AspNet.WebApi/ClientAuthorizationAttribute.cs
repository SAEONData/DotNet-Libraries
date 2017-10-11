using SAEON.Logs;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SAEON.AspNet.WebApi
{
    public class ClientAuthorizationAttribute : AuthorizationFilterAttribute
    {
        private string client;

        public ClientAuthorizationAttribute() : base() { }

        public ClientAuthorizationAttribute(string Client) : this()
        {
            client = Client;
        } 

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            using (Logging.MethodCall(GetType(), new ParameterList { { "Client", client } }))
            {
                base.OnAuthorization(actionContext);
                var principal = actionContext.RequestContext.Principal as ClaimsPrincipal;
                Logging.Verbose("Client: {client} Claims: {claims}", client, principal.Claims.Select(i => i.Type + "=" + i.Value));
                if (!(principal.HasClaim(x => x.Type == "client_id" && x.Value == client)))
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
