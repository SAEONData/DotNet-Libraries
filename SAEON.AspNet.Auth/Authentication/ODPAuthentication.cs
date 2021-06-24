#if NET5_0_OR_GREATER
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SAEON.AspNet.Auth
{
    public static class ODPAuthenticationDefaults
    {
        public const string AuthenticationScheme = "ODP";
        public const string AccessTokenClaim = "ODPAccessToken";
        public const string AccessTokenPolicy = "ODPAccessToken";
        public const string AdminTokenClaim = "ODPAdminToken";
        public const string AdminTokenPolicy = "ODPAdminPolicy";
        public const string ClientIdClaim = "ClientId";
        public const string IdTokenClaim = "ODPIdToken";
        public const string IdTokenPolicy = "ODPIdToken";
    }

    public class ODPAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string IntrospectionUrl { get; set; }
    }

    public class ODPAuthenticationPostConfigureOptions : IPostConfigureOptions<ODPAuthenticationOptions>
    {
        public void PostConfigure(string name, ODPAuthenticationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.IntrospectionUrl))
            {
                throw new InvalidOperationException("IntrospectionUrl must be provided in options");
            }
        }
    }

    public class ODPAuthenticationHandler : AuthenticationHandler<ODPAuthenticationOptions>
    {
        public ODPAuthenticationHandler(IOptionsMonitor<ODPAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) :
            base(options, logger, encoder, clock)
        { }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            using (SAEONLogs.MethodCall(GetType()))
            {
                try
                {
                    SAEONLogs.Debug("IntrospectionUrl: {IntrospectionUrl}", Options.IntrospectionUrl);
                    var token = Request.GetBearerToken();
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        SAEONLogs.Error("ODPAuthorization Failed, no token");
                        return AuthenticateResult.Fail("No token");
                    }
                    SAEONLogs.Debug("Token: {Token}", token);
                    // Validate token
                    using (var client = new HttpClient())
                    {
                        client.SetBearerToken(token);
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
                        using (var formContent = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("token", token) }))
                        {
                            var response = await client.PostAsync(new Uri(Options.IntrospectionUrl), formContent).ConfigureAwait(false);
                            if (!response.IsSuccessStatusCode)
                            {
                                SAEONLogs.Error("HttpError: {StatusCode} {Reason}", response.StatusCode, response.ReasonPhrase);
                                SAEONLogs.Error("Response: {Response}", await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            }
                            response.EnsureSuccessStatusCode();
                            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            SAEONLogs.Information("Response: {Response}", json);
                            var jObj = JObject.Parse(json);
                            var isActive = jObj.Value<bool>("active");
                            if (!isActive)
                            {
                                SAEONLogs.Error("ODPAuthorization, invalid token {Token}", token);
                                return AuthenticateResult.Fail("Invalid token");
                            }
                            if (jObj["ext"] == null)
                            { // Access token
                                var clientId = jObj.Value<string>("client_id");
                                var claims = new List<Claim> {
                                    new Claim(ODPAuthenticationDefaults.ClientIdClaim, clientId),
                                    new Claim(ODPAuthenticationDefaults.AccessTokenClaim, token)
                                };
                                var identity = new ClaimsIdentity(claims, ODPAuthenticationDefaults.AuthenticationScheme);
                                var principal = new ClaimsPrincipal(identity);
                                var ticket = new AuthenticationTicket(principal, ODPAuthenticationDefaults.AuthenticationScheme);
                                SAEONLogs.Debug("ODPAuthentication access token succeeded Claims: {@Claims}", claims.ToClaimsList());
                                return AuthenticateResult.Success(ticket);
                            }
                            else
                            {
                                var clientId = jObj.Value<string>("client_id");
                                var userId = jObj["ext"].Value<string>("user_id");
                                var userEmail = jObj["ext"].Value<string>("email");
                                var userRoles = from r in jObj["ext"]["access_rights"] select (string)r["role_name"];
                                SAEONLogs.Debug("User Id: {Id} Email: {Email}, Roles: {Role}", userId, userEmail, userRoles);
                                var claims = new List<Claim> {
                                    new Claim(ODPAuthenticationDefaults.ClientIdClaim,clientId),
                                    new Claim(ODPAuthenticationDefaults.IdTokenClaim, token),
                                    new Claim(ClaimTypes.NameIdentifier,userId),
                                    new Claim(ClaimTypes.Email,userId)
                                };
                                foreach (var userRole in userRoles)
                                {
                                    claims.Add(new Claim(ClaimTypes.Role, userRole));
                                }
                                if (userRoles.Contains("admin") || userRoles.Contains("Admin"))
                                {
                                    claims.Add(new Claim(ODPAuthenticationDefaults.AdminTokenClaim, true.ToString()));
                                }
                                var identity = new ClaimsIdentity(claims, ODPAuthenticationDefaults.AuthenticationScheme);
                                var principal = new ClaimsPrincipal(identity);
                                var ticket = new AuthenticationTicket(principal, ODPAuthenticationDefaults.AuthenticationScheme);
                                SAEONLogs.Debug("ODPAuthentication id token succeeded Claims: {@Claims}", claims.ToClaimsList());
                                return AuthenticateResult.Success(ticket);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    SAEONLogs.Exception(ex);
                    throw;
                }
            }
        }
    }

    public static class ODPAuthenticationExtensions
    {
        public static AuthenticationBuilder AddODP(this AuthenticationBuilder builder)
        {
            return AddODP(builder, ODPAuthenticationDefaults.AuthenticationScheme, _ => { });
        }

        public static AuthenticationBuilder AddODP(this AuthenticationBuilder builder, string authenticationScheme)
        {
            return AddODP(builder, authenticationScheme, _ => { });
        }

        public static AuthenticationBuilder AddODP(this AuthenticationBuilder builder, Action<ODPAuthenticationOptions> configureOptions)
        {
            return AddODP(builder, ODPAuthenticationDefaults.AuthenticationScheme, configureOptions);
        }

        public static AuthenticationBuilder AddODP(this AuthenticationBuilder builder, string authenticationScheme, Action<ODPAuthenticationOptions> configureOptions)
        {
            builder.Services.AddSingleton<IPostConfigureOptions<ODPAuthenticationOptions>, ODPAuthenticationPostConfigureOptions>();
            return builder.AddScheme<ODPAuthenticationOptions, ODPAuthenticationHandler>(authenticationScheme, configureOptions);
        }

        public static void AddODPAccessTokenPolicy(this AuthorizationOptions options)
        {
            options.AddPolicy(ODPAuthenticationDefaults.AccessTokenPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(ODPAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == ODPAuthenticationDefaults.AccessTokenClaim) ||
                    context.User.HasClaim(c => c.Type == ODPAuthenticationDefaults.IdTokenClaim));
            });
        }

        public static void AddODPIdTokenPolicy(this AuthorizationOptions options)
        {
            options.AddPolicy(ODPAuthenticationDefaults.IdTokenPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(ODPAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(ODPAuthenticationDefaults.IdTokenClaim);
            });
        }

        public static void AddODPAdminPolicy(this AuthorizationOptions options)
        {
            options.AddPolicy(ODPAuthenticationDefaults.AdminTokenPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(ODPAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(ODPAuthenticationDefaults.IdTokenClaim);
                policy.RequireClaim(ODPAuthenticationDefaults.AdminTokenClaim);
            });
        }

        public static void AddODPPolicies(this AuthorizationOptions options)
        {
            options.AddODPAccessTokenPolicy();
            options.AddODPIdTokenPolicy();
            options.AddODPAdminPolicy();
        }
    }
}
#endif