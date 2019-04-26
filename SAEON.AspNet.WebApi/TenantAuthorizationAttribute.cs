using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SAEON.AspNet.WebApi
{
    public class TenantAuthorizationAttribute : AuthorizationFilterAttribute
    {
        private readonly string tenantHeader = "x-data-tenant";
        public List<string> Tenants { get; private set; } = new List<string>();
        public string DefaultTenant { get; private set; } = string.Empty;

        public TenantAuthorizationAttribute() : base() { }

        public TenantAuthorizationAttribute(List<string> tenants, string defaultTenant) : this()
        {
            Tenants.Clear();
            Tenants.AddRange(tenants);
            DefaultTenant = DefaultTenant;
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            using (Logging.MethodCall(GetType(), new ParameterList { { "Tenants", Tenants }, { "Default", DefaultTenant } }))
            {
                base.OnAuthorization(actionContext);
                if (!Tenants.Any())
                {
                    // Try read from WebConfig
                    var tenants = (ConfigurationManager.AppSettings["Tenants"] ?? string.Empty).Split(new string[] { "; " }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    Tenants.AddRange(tenants);
                }
                if (!Tenants.Any())
                {
                    Logging.Error("Tenant Authorization Failed (No tenants)");
                    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden);
                    actionContext.Response.ReasonPhrase = "Tenant Authorization Failed (No tenants)";
                    return;
                }
                if (string.IsNullOrWhiteSpace(DefaultTenant))
                {
                    // Try read from WebConfig
                    DefaultTenant = (ConfigurationManager.AppSettings["Tenants"] ?? string.Empty);
                }
                if (string.IsNullOrWhiteSpace(DefaultTenant))
                {
                    Logging.Error("Tenant Authorization Failed (No default tenant)");
                    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden);
                    actionContext.Response.ReasonPhrase = "Tenant Authorization Failed (No default tenant)";
                    return;
                }
                string tenant = string.Empty;
                if (!actionContext.Request.Headers.Contains(tenantHeader))
                {
                    tenant = DefaultTenant;
                }
                else
                {
                    tenant = actionContext.Request.Headers.GetValues(tenantHeader).FirstOrDefault();
                }
                if (string.IsNullOrWhiteSpace(tenant))
                {
                    Logging.Error("Tenant Authorization Failed (No tenant)");
                    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden);
                    actionContext.Response.ReasonPhrase = "Tenant Authorization Failed (No tenant)";
                    return;
                }
                if (!Tenants.Contains(tenant))
                {
                    Logging.Error("Tenant Authorization Failed (Unknown tenant)");
                    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden);
                    actionContext.Response.ReasonPhrase = "Tenant Authorization Failed (Unknown tenant)";
                    return;
                }
            }
        }

    }
}
