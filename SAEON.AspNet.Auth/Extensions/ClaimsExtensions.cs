using System.Collections.Generic;
using System.Security.Claims;

namespace SAEON.AspNet.Auth
{
    public class ClaimInfo
    {
        public string Claim { get; set; }
        public string Value { get; set; }
        public override string ToString()
        {
            return $"{Claim}: {Value}";
        }
    }

    public static class ClaimsExtensions
    {
        public static List<ClaimInfo> ToClaimsList(this IEnumerable<Claim> claims)
        {
            var result = new List<ClaimInfo>();
            foreach (var claim in claims)
            {
                result.Add(new ClaimInfo { Claim = claim.Type, Value = claim.Value });
            }
            return result;
        }

        public static List<ClaimInfo> ToClaimsList(this List<Claim> claims)
        {
            var result = new List<ClaimInfo>();
            foreach (var claim in claims)
            {
                result.Add(new ClaimInfo { Claim = claim.Type, Value = claim.Value });
            }
            return result;
        }
    }
}
