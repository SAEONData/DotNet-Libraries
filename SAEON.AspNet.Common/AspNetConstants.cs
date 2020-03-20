#if !NET472
using System;
#endif

namespace SAEON.AspNet.Common
{
    public static class AspNetConstants
    {
        public static readonly string AcceptEncoding = "Accept-Encoding";
#if !NET472
        [Obsolete("ApplicationJson is obsolete. Use MediaTypeNames.Application.Json", true)]
#endif
        public static readonly string ApplicationJson = "application/json";
        public static readonly string ClaimClientId = "client_id";
        public static readonly string ClaimRole = "role";
        public static readonly string ClaimSubject = "sub";
        public static readonly string ClaimUserName = "name";
        public static readonly string ContentEncoding = "Content-Encoding";
        public static readonly string ContentSecurityPolicy = "ContentSecurityPolicy";
        public static readonly string RefreshTokens = "RefreshTokens"; // In seconds before expiry 
        public static readonly string TenantDefault = "DefaultTenant"; // Config
        public static readonly string TenantHeader = "Tenant"; // Header
        public static readonly string TenantSession = "Tenant"; // Session
        public static readonly string TenantTenants = "Tenants"; // Config
        public const string TenantPolicy = "TenantHeaderPolicy";
    }
}
