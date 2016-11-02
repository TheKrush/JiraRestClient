using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Dto
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    internal class WorklogsContainer
    {
        public int startAt { get; set; }
        public int maxResults { get; set; }
        public int total { get; set; }
        public List<Worklog> Worklogs { get; set; }
    }
}
