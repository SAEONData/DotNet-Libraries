using Newtonsoft.Json.Linq;
using SAEON.AspNet.Common;
using SAEON.Core;
using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.Configuration;
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
    public sealed class ODPAuthorizeAttribute : AuthorizationFilterAttribute
    {
        private readonly bool requireLogin = false;

        public ODPAuthorizeAttribute() : base() { }

        public ODPAuthorizeAttribute(bool requireLogin = false) : this()
        {
            this.requireLogin = requireLogin;
        }

        public override async Task OnAuthorizationAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            using (Logger.MethodCall(GetType()))
            {
                try
                {
                    if (actionContext == null) throw new ArgumentNullException(nameof(actionContext));
                    var introspectionUrl = ConfigurationManager.AppSettings["ODPAuthInspectionUrl"];
                    if (string.IsNullOrWhiteSpace(introspectionUrl)) throw new ArgumentNullException($"AppSettings[ODPAuthInspectionUrl]");
                    // Get token
                    var token = actionContext?.Request?.Headers?.Authorization?.Parameter;
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        Logger.Error("ODP Authorization Failed, no token");
                        actionContext.Response = actionContext.Request.CreateErrorResponse(HttpStatusCode.Forbidden, new SecurityException("ODP Authorization Failed, no token"));
                    }
                    Logger.Verbose("Token: {Token}", token);
                    // Validate token
                    using (var handler = new HttpClientHandler())
                    {
                        // Remove once Mark has sorted out the certificate
                        if (ConfigurationManager.AppSettings["ODPAuthIgnoreInvalidCertificates"].IsTrue())
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
                                    Logger.Error("HttpError: {StatusCode} {Reason}", response.StatusCode, response.ReasonPhrase);
                                    Logger.Error("Response: {Response}", await response.Content.ReadAsStringAsync());
                                }
                                response.EnsureSuccessStatusCode();
                                var json = await response.Content.ReadAsStringAsync();
                                Logger.Information("Result: {Result}", json);
                                var jObject = JObject.Parse(json);
                                var isActive = jObject.Value<bool>("active");
                                if (!isActive)
                                {
                                    Logger.Error("Invalid token {Token}", token);
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
                    Logger.Exception(ex);
                    throw;
                }
            }
        }
    }
}
