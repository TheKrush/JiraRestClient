using System;
using System.Diagnostics.CodeAnalysis;
using TechTalk.JiraRestClient.Helper;

namespace TechTalk.JiraRestClient.Dto
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class WorklogUpdatedValue
    {
        public int worklogId { get; set; }
        public long updatedTime { get; set; }
        public DateTime UpdatedTime => TimeUtils.FromUnixTime(updatedTime);
    }
}