using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Dto
{    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    internal class IssueContainer<TIssueFields> where TIssueFields : IssueFields, new()
    {
        public string expand { get; set; }

        public int maxResults { get; set; }
        public int total { get; set; }
        public int startAt { get; set; }

        public List<Issue<TIssueFields>> issues { get; set; }
    }
}
