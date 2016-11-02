using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Dto
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public abstract class IssueLink
    {
        protected IssueLink()
        {
            type = new LinkType();
            inwardIssue = new IssueRef();
            outwardIssue = new IssueRef();
        }

        public string id { get; set; }

        public LinkType type { get; set; }
        public IssueRef outwardIssue { get; set; }
        public IssueRef inwardIssue { get; set; }
    }
}
