using System;

namespace SAEON.Core
{
    public static class TimeSpanExtensions
    {
        public static string TimeStr(this TimeSpan timeSpan)
        {
            if (timeSpan.Days > 0)
                return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m {timeSpan.Seconds}.{timeSpan.Milliseconds:D3}s";
            else if (timeSpan.Hours > 0)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m {timeSpan.Seconds}.{timeSpan.Milliseconds:D3}s";
            else if (timeSpan.Minutes > 0)
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}.{timeSpan.Milliseconds:D3}s";
            else
                return $"{timeSpan.Seconds}.{timeSpan.Milliseconds:D3}s";
        }
    }
}
