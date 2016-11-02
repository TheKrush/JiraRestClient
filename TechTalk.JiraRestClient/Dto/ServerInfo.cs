using System;
using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Dto
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ServerInfo
    {
        public string baseUrl { get; set; }
        public string version { get; set; }
        public string buildNumber { get; set; }
        public DateTime? buildDate { get; set; }
        public DateTime? serverTime { get; set; }
        public string scmInfo { get; set; }
        public string buildPartnerName { get; set; }
        public String serverTitle { get; set; }
    }
}
