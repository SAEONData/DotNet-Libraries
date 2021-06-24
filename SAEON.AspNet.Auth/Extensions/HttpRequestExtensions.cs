#if NET472 
using System.Web;
#else
using Microsoft.AspNetCore.Http;
#endif
using System;
using System.Net;
using System.Net.Http.Headers;

namespace SAEON.AspNet.Auth
{
    public static class HttpRequestExtensions
    {
        public static string GetBearerToken(this HttpRequest request)
        {
            if (!request.Headers.ContainsKey("Authorization"))
            {
                return null;
            }
            if (!AuthenticationHeaderValue.TryParse(request.Headers["Authorization"], out AuthenticationHeaderValue headerValue))
            {
                return null;
            }
            if (!"Bearer".Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return headerValue.Parameter;
        }

#if NET5_0_OR_GREATER
        public static bool IsLocal(this HttpRequest req)
        {
            var connection = req.HttpContext.Connection;
            if (connection.RemoteIpAddress != null)
            {
                if (connection.LocalIpAddress != null)
                {
                    return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
                }
                else
                {
                    return IPAddress.IsLoopback(connection.RemoteIpAddress);
                }
            }

            // for in memory TestServer or when dealing with default connection info
            if (connection.RemoteIpAddress == null && connection.LocalIpAddress == null)
            {
                return true;
            }

            return false;
        }
#endif
    }
}
