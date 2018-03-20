using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                var config = context.HttpContext.RequestServices.GetService<IConfiguration>();
                if (config == null) 
                {
                    Logging.Error("Configuration is null"); 
                } 
                else
                {
                    policy = config["ContentSecurityPolicy:Policy"]; 
                }
                if (!string.IsNullOrWhiteSpace(policy) && (context.Result is ViewResult result))
                {
                    Logging.Verbose("ContentSecurityPolicy: {csp}", policy);
                    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
                    if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Type-Options"))
                    { 
                        context.HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                    }

                    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options
                    if (!context.HttpContext.Response.Headers.ContainsKey("X-Frame-Options"))
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
                    if (!context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy"))
                    {
                        context.HttpContext.Response.Headers.Add("Content-Security-Policy", policy);
                    }
                    // and once again for IE
                    if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Security-Policy"))
                    {
                        context.HttpContext.Response.Headers.Add("X-Content-Security-Policy", policy);
                    }

                    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy
                    var referrer_policy = "no-referrer";
                    if (!context.HttpContext.Response.Headers.ContainsKey("Referrer-Policy"))
                    {
                        context.HttpContext.Response.Headers.Add("Referrer-Policy", referrer_policy);
                    }
                }

            }
        }
    }

}

