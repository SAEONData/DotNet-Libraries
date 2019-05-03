using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SAEON.AspNet.WebApi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class TenantAuthorizationAttribute : AuthorizationFilterAttribute
    {
        private static readonly string tenantHeaderId = "x-data-tenant";
        public List<string> Tenants { get; private set; } = new List<string>();
        public string DefaultTenant { get; private set; } = string.Empty;

        public TenantAuthorizationAttribute() : base()
        {
            //using (Logging.MethodCall(GetType()))
            {
                var tenants = (ConfigurationManager.AppSettings["Tenants"] ?? string.Empty).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                Tenants.AddRange(tenants);
                DefaultTenant = (ConfigurationManager.AppSettings["DefaultTenant"] ?? string.Empty);
                //Logging.Verbose("Tenants: {Tenants} DefaultTenant: {DefaultTenant}", Tenants.ToArray(), DefaultTenant);
            }
        }

        public TenantAuthorizationAttribute(List<string> tenants, string defaultTenant) : this()
        {
            //using (Logging.MethodCall(GetType(), new ParameterList { { "Tenants", string.Join(", ",tenants) }, { "Default", defaultTenant } }))
            {
                Tenants.Clear();
                Tenants.AddRange(tenants);
                DefaultTenant = defaultTenant;
                //Logging.Verbose("Tenants: {Tenants} DefaultTenant: {DefaultTenant}", Tenants.ToArray(), DefaultTenant);
            }
        }

        private void DoTenantAuthorization(HttpActionContext actionContext)
        {
            if (!actionContext.Request.Headers.Contains(tenantHeaderId))
            {
                Logging.Error("Tenant Authorization Failed (No tenant header)");
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Tenant Authorization Failed (No tenant header)");
                return;
            }
            var tenant = actionContext.Request.Headers.GetValues(tenantHeaderId).FirstOrDefault();
            Logging.Verbose("Tenants: {Tenants} DefaultTenant: {DefaultTenant} Tenant: {Tenant}", Tenants.ToArray(), DefaultTenant, tenant);
            if (string.IsNullOrWhiteSpace(tenant))
            {
                tenant = DefaultTenant;
            }
            if (string.IsNullOrWhiteSpace(tenant))
            {
                Logging.Error("Tenant Authorization Failed (No tenant)");
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Tenant Authorization Failed (No tenant)");
                return;
            }
            if (!Tenants.Any())
            {
                Logging.Error("Tenant Authorization Failed (No tenants)");
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Tenant Authorization Failed (No tenants)");
                return;
            }
            if (!Tenants.Contains(tenant))
            {
                Logging.Error("Tenant Authorization Failed (Unknown tenant)");
                actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Tenant Authorization Failed (Unknown tenant)");
                return;
            }
        }

        public override async Task OnAuthorizationAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            using (Logging.MethodCall(GetType()))
            {
                await base.OnAuthorizationAsync(actionContext, cancellationToken);
                DoTenantAuthorization(actionContext);
            }
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            using (Logging.MethodCall(GetType()))
            {
                base.OnAuthorization(actionContext);
                DoTenantAuthorization(actionContext);
            }
        }

        public static string GetTenantFromHeaders(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var tenant = request.Headers.Contains(tenantHeaderId) ? request.Headers.GetValues(TenantAuthorizationAttribute.tenantHeaderId).FirstOrDefault() : null;
            if (string.IsNullOrWhiteSpace(tenant))
            {
                throw new HttpResponseException(request.CreateErrorResponse(HttpStatusCode.NotFound, "Tenant header not found"));
            }
            return tenant;
        }

    }
}
