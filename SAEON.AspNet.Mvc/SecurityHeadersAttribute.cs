using SAEON.Logs;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;

namespace SAEON.AspNet.Mvc
{
    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuted(ResultExecutedContext context) 
        {
            using (Logging.MethodCall(this.GetType()))
            {
                string policy = null;
                policy = ConfigurationManager.AppSettings["ContentSecurityPolicy"];
                if (!string.IsNullOrWhiteSpace(policy) && (context.Result is ViewResult result))
                { 
                    Logging.Verbose("ContentSecurityPolicy: {policy}", policy);
                    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
                    if (!context.HttpContext.Response.Headers.AllKeys.Contains("X-Content-Type-Options"))
                    {
                        context.HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                    }

                    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options
                    if (!context.HttpContext.Response.Headers.AllKeys.Contains("X-Frame-Options"))
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
                    if (!context.HttpContext.Response.Headers.AllKeys.Contains("Content-Security-Policy"))
                    {
                        context.HttpContext.Response.Headers.Add("Content-Security-Policy", policy);
                    }
                    // and once again for IE
                    if (!context.HttpContext.Response.Headers.AllKeys.Contains("X-Content-Security-Policy"))
                    {
                        context.HttpContext.Response.Headers.Add("X-Content-Security-Policy", policy);
                    }

                    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy
                    var referrer_policy = "no-referrer";
                    if (!context.HttpContext.Response.Headers.AllKeys.Contains("Referrer-Policy"))
                    {
                        context.HttpContext.Response.Headers.Add("Referrer-Policy", referrer_policy);
                    }
                }

            }
        }
    }

}
