using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Dto
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class Comment
    {        
        public string id { get; set; }        
        public string body { get; set; }
    }
}
