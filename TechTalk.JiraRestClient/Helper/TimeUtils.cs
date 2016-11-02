using System;
using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Helper
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static class TimeUtils
    {
        public static readonly DateTime UnixStart = new DateTime(1970, 1, 1, 0, 0, 0);

        public static DateTime FromUnixTime(long unixTime)
        {
            return UnixStart.AddMilliseconds(unixTime);
        }

        public static long ToUnixTime(DateTime time)
        {
            return (long)(time - UnixStart).TotalMilliseconds;
        }
    }
}
