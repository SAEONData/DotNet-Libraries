#if NETCOREAPP2_0
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#else
using System.Configuration;
using System.Linq; 
using System.Web.Mvc;
#endif
using SAEON.Logs;
 
namespace SAEON.AspNet.Core  
{
    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context) 
        {
            using (Logging.MethodCall(this.GetType()))
            {
                string policy = null;
#if NETCOREAPP2_0
                var config = context.HttpContext.RequestServices.GetService<IConfiguration>();
                if (config == null) 
                {
                    Logging.Error("Configuration is null"); 
                } 
                else
                {
                    policy = config["ContentSecurityPolicy:Policy"]; 
                }
#else
                policy = ConfigurationManager.AppSettings["ContentSecurityPolicy"];
#endif
                if (!string.IsNullOrWhiteSpace(policy) && (context.Result is ViewResult result))
                {
                    Logging.Verbose("ContentSecurityPolicy: {policy}", policy);
                    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
#if NETCOREAPP2_0
                    if (!context.HttpContext.Response.Headers. ContainsKey("X-Content-Type-Options"))
#else
                    if (!context.HttpContext.Response.Headers.AllKeys.Contains("X-Content-Type-Options"))
#endif
                    {
                        context.HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                    }

                    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options
#if NETCOREAPP2_0
                    if (!context.HttpContext.Response.Headers.ContainsKey("X-Frame-Options"))
#else
                    if (!context.HttpContext.Response.Headers.AllKeys.Contains("X-Frame-Options"))
#endif
                    {
                        context.HttpContext.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                    }
                    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
                    //var csp = "default-src 'self'; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';";
                    // also consider adding upgrade-insecure-requests once you have HTTPS in place for production
                    //csp += "upgrade-insecure-requests;";
                    // also an example if you need client images to be displayed from twitter
                    // csp += "img-src 'self' https://pbs.twimg.com;"; 

                    // once for standards compliant browsers
#if NETCOREAPP2_0
                    if (!context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy"))
#else
                    if (!context.HttpContext.Response.Headers.AllKeys.Contains("Content-Security-Policy"))
#endif
                    {
                        context.HttpContext.Response.Headers.Add("Content-Security-Policy", policy);
                    }
                    // and once again for IE
#if NETCOREAPP2_0
                    if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Security-Policy"))
#else
                    if (!context.HttpContext.Response.Headers.AllKeys.Contains("X-Content-Security-Policy"))
#endif
                    {
                        context.HttpContext.Response.Headers.Add("X-Content-Security-Policy", policy);
                    }

                    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy
                    var referrer_policy = "no-referrer";
#if NETCOREAPP2_0
                    if (!context.HttpContext.Response.Headers.ContainsKey("Referrer-Policy"))
#else
                    if (!context.HttpContext.Response.Headers.AllKeys.Contains("Referrer-Policy"))
#endif
                    {
                        context.HttpContext.Response.Headers.Add("Referrer-Policy", referrer_policy);
                    }
                }

            }
        }
    }

}

