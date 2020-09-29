using SAEON.AspNet.Common;
using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SAEON.AspNet.WebApi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class TenantAuthorizationAttribute : AuthorizationFilterAttribute
    {
        public List<string> Tenants { get; private set; } = new List<string>();
        public string DefaultTenant { get; private set; } = string.Empty;

        public TenantAuthorizationAttribute() : base()
        {
            //using (SAEONLogs.MethodCall(GetType()))
            {
                var tenants = (ConfigurationManager.AppSettings[AspNetConstants.TenantTenants] ?? string.Empty).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                Tenants.AddRange(tenants);
                DefaultTenant = (ConfigurationManager.AppSettings[AspNetConstants.TenantDefault] ?? string.Empty);
                //SAEONLogs.Verbose("Tenants: {Tenants} DefaultTenant: {DefaultTenant}", Tenants.ToArray(), DefaultTenant);
            }
        }

        public TenantAuthorizationAttribute(List<string> tenants, string defaultTenant) : this()
        {
            //using (SAEONLogs.MethodCall(GetType(), new ParameterList { { "Tenants", string.Join(", ",tenants) }, { "Default", defaultTenant } }))
            {
                Tenants.Clear();
                Tenants.AddRange(tenants);
                DefaultTenant = defaultTenant;
                //SAEONLogs.Verbose("Tenants: {Tenants} DefaultTenant: {DefaultTenant}", Tenants.ToArray(), DefaultTenant);
            }
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            using (SAEONLogs.MethodCall(GetType()))
            {
                if (actionContext == null) throw new ArgumentNullException(nameof(actionContext));
                //if (!actionContext.Request.Headers.Contains(AspNetConstants.TenantHeader))
                //{
                //    SAEONLogs.Error("Tenant Authorization Failed (No tenant header)");
                //    actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Tenant Authorization Failed (No tenant header)");
                //    return;
                //}
                var tenant = actionContext.Request.Headers.Contains(AspNetConstants.TenantHeader) ? actionContext.Request.Headers.GetValues(AspNetConstants.TenantHeader).FirstOrDefault() : null;
                SAEONLogs.Verbose("Tenants: {Tenants} DefaultTenant: {DefaultTenant} Tenant: {Tenant}", Tenants.ToArray(), DefaultTenant, tenant);
                if (string.IsNullOrWhiteSpace(tenant))
                {
                    tenant = DefaultTenant;
                }
                if (string.IsNullOrWhiteSpace(tenant))
                {
                    SAEONLogs.Error("Tenant Authorization Failed (No tenant)");
                    actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Tenant Authorization Failed (No tenant)");
                    return;
                }
                if (!Tenants.Any())
                {
                    SAEONLogs.Error("Tenant Authorization Failed (No tenants)");
                    actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Tenant Authorization Failed (No tenants)");
                    return;
                }
                if (!Tenants.Contains(tenant))
                {
                    SAEONLogs.Error("Tenant Authorization Failed (Unknown tenant)");
                    actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Tenant Authorization Failed (Unknown tenant)");
                    return;
                }
            }
        }

        public static string GetTenantFromHeaders(HttpRequestMessage request)
        {
            //using (SAEONLogs.MethodCall(GetType()))
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }
                var tenant = request.Headers.Contains(AspNetConstants.TenantHeader) ? request.Headers.GetValues(AspNetConstants.TenantHeader).FirstOrDefault() : null;
                if (string.IsNullOrWhiteSpace(tenant))
                {
                    tenant = (ConfigurationManager.AppSettings[AspNetConstants.TenantDefault] ?? string.Empty);
                }
                if (string.IsNullOrWhiteSpace(tenant))
                {
                    throw new HttpResponseException(request.CreateErrorResponse(HttpStatusCode.NotFound, "Tenant header not found"));
                }
                return tenant;
            }
        }

    }
}
