using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TechTalk.JiraRestClient.Helper;

namespace TechTalk.JiraRestClient.Dto
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class WorklogUpdated
    {
        public List<WorklogUpdatedValue> values { get; set; }
        public long since { get; set; }
        public long until { get; set; }
        public bool lastPage { get; set; }
        public DateTime Since => TimeUtils.FromUnixTime(since);
        public DateTime Until => TimeUtils.FromUnixTime(until);
    }
}
