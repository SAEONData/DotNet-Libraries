using Newtonsoft.Json.Linq;
using SAEON.AspNet.Common;
using SAEON.Core;
using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SAEON.AspNet.WebApi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
    public sealed class ODPAuthorizeAttribute : AuthorizationFilterAttribute
    {
        private bool requireLogin = false;

        public ODPAuthorizeAttribute() : base() { }

        public ODPAuthorizeAttribute(bool requireLogin = false) : this()
        {
            this.requireLogin = requireLogin;
        }

        [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "<Pending>")]
        public override async Task OnAuthorizationAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            using (Logging.MethodCall(GetType()))
            {
                try
                {
                    if (actionContext == null) throw new ArgumentNullException(nameof(actionContext));
                    var introspectionUrl = ConfigurationManager.AppSettings[AspNetConstants.ODPAuthInspectionUrl];
                    if (string.IsNullOrWhiteSpace(introspectionUrl)) throw new ArgumentNullException($"AppSettings[{AspNetConstants.ODPAuthInspectionUrl}]");
                    // Get token
                    var token = actionContext?.Request?.Headers?.Authorization?.Parameter;
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        Logging.Error("ODP Authorization Failed, no token");
                        actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, new SecurityException("ODP Authorization Failed, no token"));
                    }
                    Logging.Verbose("Token: {Token}", token);
                    // Validate token
                    using (var handler = new HttpClientHandler())
                    {
                        // Remove once Mark has sorted out the certificate
                        if (ConfigurationManager.AppSettings[AspNetConstants.ODBAuthIgnoreInvalidCertificates].IsTrue())
                        {
                            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                        }
                        using (var client = new HttpClient(handler))
                        {
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                            client.DefaultRequestHeaders.Accept.Clear();
                            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(AspNetConstants.ApplicationJson));
                            using (var formContent = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("token", token) }))
                            {
                                var response = await client.PostAsync(new Uri(introspectionUrl), formContent);
                                if (!response.IsSuccessStatusCode)
                                {
                                    Logging.Error("HttpError: {StatusCode} {Reason}", response.StatusCode, response.ReasonPhrase);
                                    Logging.Error("Response: {Response}", await response.Content.ReadAsStringAsync());
                                }
                                response.EnsureSuccessStatusCode();
                                var json = await response.Content.ReadAsStringAsync();
                                Logging.Information("Result: {Result}", json);
                                var jObject = JObject.Parse(json);
                                var isActive = jObject.Value<bool>("active");
                                if (!isActive)
                                {
                                    Logging.Error("Invalid token {Token}", token);
                                    actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, new SecurityException("Invalid token"));
                                    return;
                                }
                                // If requireLogin make sure token is authentication token
                                if (requireLogin)
                                {

                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }
    }
}
