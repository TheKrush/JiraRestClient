using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Dto
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class IssueRef
    {
        public string id { get; set; }
        public string key { get; set; }

        internal string JiraIdentifier => string.IsNullOrWhiteSpace(id) ? key : id;
    }
}
