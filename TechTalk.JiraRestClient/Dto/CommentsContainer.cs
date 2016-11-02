using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Dto
{    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class CommentsContainer
    {
        public int startAt { get; set; }
        public int maxResults { get; set; }
        public int total { get; set; }
        public List<Comment> comments { get; set; }
    }
}
