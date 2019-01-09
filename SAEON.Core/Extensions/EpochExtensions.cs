using System;

namespace SAEON.Core.Extensions
{
    public static class EpochExtensions
    {
        public static DateTime FromEpoch(this int unixTime) 
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public static int ToEpoch(this DateTime date) 
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt32((date.ToUniversalTime() - epoch).TotalSeconds);
        }

    }
}
